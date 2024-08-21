using System;
using System.Collections.Generic;
using Byter;

namespace Netly
{
    public static partial class RUDP
    {
        internal sealed class Channel
        {
            public const int ResentTimeout = 500;

            private readonly object
                _locker = new object(),
                _reliableLocker = new object(),
                _sequencedLocker = new object();

            private readonly Primitive _primitive = new Primitive();
            private uint _receivedSequencedId, _receivedReliableId;
            private uint _sequencedId, _reliableId;

            public Channel(Host host)
            {
                ReliablePackages = new Dictionary<uint, DataContent>();
                _sequencedId = 0;
                _reliableId = 0;
                SendRaw = null;
            }

            public Action<byte[], MessageType> OnRawData { get; set; }
            public Action<byte[]> SendRaw { get; set; }
            private Dictionary<uint, DataContent> ReliablePackages { get; }

            public void ToAddData(byte[] data, MessageType messageType)
            {
                if (data != null && data.Length > 0)
                    lock (_locker)
                    {
                        // adding prefix (package type)
                        _primitive.Add.Byte((byte)messageType);

                        // adding header
                        switch (messageType)
                        {
                            case MessageType.Unreliable:
                            {
                                // don't need more information
                                break;
                            }
                            case MessageType.Sequenced:
                            {
                                _sequencedId++;
                                _primitive.Add.UInt(_sequencedId);
                                break;
                            }
                            case MessageType.Reliable:
                            {
                                _reliableId++;
                                _primitive.Add.UInt(_reliableId);
                                ReliablePackages.Add(_reliableId, new DataContent(_reliableId, data));
                                break;
                            }
                        }

                        _primitive.Add.Bytes(data);
                        SendRaw(_primitive.GetBytes());
                        _primitive.Reset();
                    }
            }

            public void ToUpdateReliableQueue()
            {
                lock (_locker)
                {
                    var now = DateTime.UtcNow;

                    if (ReliablePackages.Count > 0)
                        foreach (var package in ReliablePackages)
                            // resent package
                            if (package.Value.TimeoutAt <= now)
                            {
                                _primitive.Add.Byte((byte)MessageType.Reliable);
                                _primitive.Add.UInt(package.Value.Id);
                                _primitive.Add.Bytes(package.Value.Data);
                                SendRaw(_primitive.GetBytes());
                                _primitive.Reset();
                                package.Value.UpdateTimeout();
                            }
                }
            }

            public void OnReceiveRaw(ref byte[] data, Host host)
            {
                Console.WriteLine($"{host} OnReceiveRaw: {data.Length}");

                // 7 bytes means.
                // --------------
                // 2 bytes for type: 1 byte overhead + 1 byte content
                // 5 bytes for id: 1 byte overhead + 4 byte for id (is uint)
                // ---------------
                // note: this result is based on <Byter.Primitive> package used to serialize and deserialize package
                const int contentSize = 7;
                var primitive = new Primitive(data);
                var receivedType = primitive.Get.Byte();

                // Remove data when RECEIVE ACK Package
                if (data != null && data.Length == contentSize)
                {
                    var receivedId = primitive.Get.UInt();

                    if (primitive.IsValid)
                    {
                        if (receivedType == (byte)MessageType.Reliable)
                            // remove received package
                            lock (_locker)
                            {
                                Console.WriteLine($"Reliable package already received remove from resent Queue, id: {receivedId}");
                                if (ReliablePackages.ContainsKey(receivedId)) ReliablePackages.Remove(receivedId);
                            }

                        return;
                    }
                }

                // Isn't ACK Package handle the data
                {
                    if (primitive.IsValid)
                        switch (receivedType)
                        {
                            case (byte)MessageType.Unreliable:
                            {
                                var buffer = primitive.Get.Bytes();
                                if (primitive.IsValid) OnRawData?.Invoke(buffer, MessageType.Unreliable);
                                Console.WriteLine(
                                    $"Unreliable data received: size: {buffer.Length} valid: {primitive.IsValid}");
                                return;
                            }
                            case (byte)MessageType.Reliable:
                            {
                                lock (_reliableLocker)
                                {
                                    var receivedId = primitive.Get.UInt();
                                    var nextDataId = 1 + _receivedReliableId;

                                    Console.WriteLine($"Reliable ID: {_receivedReliableId}, Package ID: {receivedId}");
                                    if (receivedId <= _receivedReliableId)
                                    {
                                        // answer ack: data received successful
                                        SendPackageAck(receivedId);
                                        Console.WriteLine($"#1 Reliable data ACK package, id: {receivedId}");
                                        return;
                                    }

                                    var buffer = primitive.Get.Bytes();
                                    Console.WriteLine($"NextID: {nextDataId} == Received: {receivedId} IsEqual: {nextDataId == receivedId}");
                                    if (nextDataId == receivedId)
                                    {
                                        _receivedReliableId = receivedId;

                                        if (primitive.IsValid)
                                        {
                                            SendPackageAck(receivedId);
                                            Console.WriteLine($"Answer ACK package, id: {receivedId}");
                                            OnRawData?.Invoke(buffer, MessageType.Reliable);
                                        }
                                    }

                                    Console.WriteLine(
                                        $"Reliable data received, id: {receivedId} data: {buffer.Length} valid: {primitive.IsValid}");
                                    // TODO: adding on queue and dispatch on order
                                }

                                break;
                            }
                            case (byte)MessageType.Sequenced:
                            {
                                lock (_sequencedLocker)
                                {
                                    var receivedId = primitive.Get.UInt();
                                    Console.WriteLine(
                                        $"Sequenced data received, id: {receivedId} discarded: {!(receivedId > _receivedSequencedId)}");
                                    if (receivedId > _receivedSequencedId)
                                    {
                                        _receivedSequencedId = receivedId;
                                        var buffer = primitive.Get.Bytes();
                                        if (primitive.IsValid) OnRawData?.Invoke(buffer, MessageType.Sequenced);
                                    }
                                }

                                break;
                            }
                        }
                }
            }

            private void SendPackageAck(uint receivedId)
            {
                var primitive = new Primitive();
                primitive.Add.Byte((byte)MessageType.Reliable);
                primitive.Add.UInt(receivedId);
                SendRaw(primitive.GetBytes());
                primitive.Reset();
            }
        }
    }
}