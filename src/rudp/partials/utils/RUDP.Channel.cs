using System;
using System.Collections.Generic;
using Byter;

namespace Netly
{
    public static partial class RUDP
    {
        internal sealed class Channel
        {
            public const int ResentTimeout = 10;
            public int Id { get; }
            public Action<byte[], Host> SendRaw { get; set; }
            private readonly object _locker = new object();
            private readonly Primitive _primitive = new Primitive();
            private uint _sequencedId, _reliableId;
            private Dictionary<uint, DataContent> ReliablePackages { get; }

            private Channel(int id, Host host)
            {
                Id = id;
                ReliablePackages = new Dictionary<uint, DataContent>();
                _sequencedId = 0;
                _reliableId = 0;
                SendRaw = null;
            }

            public void ToAddData(byte[] data, MessageType messageType, Host host)
            {
                if (data != null && data.Length > 0)
                {
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
                                ReliablePackages.Add(_reliableId, new DataContent(_reliableId, data, host));
                                break;
                            }
                        }

                        _primitive.Add.Bytes(data);
                        SendRaw(_primitive.GetBytes(), host);
                        _primitive.Reset();
                    }
                }
            }

            public void ToUpdateReliableQueue()
            {
                lock (_locker)
                {
                    var now = DateTime.UtcNow;

                    if (ReliablePackages.Count > 0)
                    {
                        foreach (var package in ReliablePackages)
                        {
                            // resent package
                            if (package.Value.TimeoutAt <= now)
                            {
                                _primitive.Add.Byte((byte)MessageType.Reliable);
                                _primitive.Add.UInt(package.Value.Id);
                                _primitive.Add.Bytes(package.Value.Data);
                                SendRaw(_primitive.GetBytes(), package.Value.Host);
                                _primitive.Reset();
                                package.Value.UpdateTimeout();
                            }
                        }
                    }
                }
            }

            public void OnReceiveRaw(ref byte[] data, Host host)
            {
                // 7 bytes means.
                // --------------
                // 2 bytes for type: 1 byte overhead + 1 byte content
                // 5 bytes for id: 1 byte overhead + 4 byte for id (is uint)
                // ---------------
                // note: this result is based on <Byter.Primitive> package used to serialize and deserialize package
                const int contentSize = 7;

                if (data != null && data.Length == contentSize)
                {
                    var primitive = new Primitive(data);
                    var receivedType = primitive.Get.Byte();
                    var receivedId = primitive.Get.UInt();

                    if (primitive.IsValid)
                    {
                        if (receivedType == (byte)MessageType.Reliable)
                        {
                            // remove received package
                            lock (_locker)
                            {
                                if (ReliablePackages.ContainsKey(receivedId))
                                {
                                    ReliablePackages.Remove(receivedId);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}