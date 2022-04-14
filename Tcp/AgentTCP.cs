using System;
using System.Net.Sockets;
using System.Threading;

namespace Zenet.Network.Tcp
{
    public class AgentTCP
    {
        internal static int Timeout = 5000;
        public readonly Socket Socket;
        public readonly NetworkStream Stream;
        public bool Opened => Connected();

        private EventHandler OnOpenEvent;
        private EventHandler OnCloseEvent;
        private EventHandler<byte[]> OnDataEvent;

        private bool tryClose, closed;

        public AgentTCP(ref Socket socket)
        {
            this.Socket = socket;
            this.Stream = new NetworkStream(this.Socket);
            this.Init();
        }

        public void Send(byte[] data, bool async = true)
        {
            if (!Opened || data == null || data.Length < 1) return;

            try
            {
                if (async) Stream.BeginWrite(data, 0, data.Length, null, null);
                else Stream.Write(data, 0, data.Length);
            }
            catch { }
        }

        public void OnOpen(Action callback)
        {
            OnOpenEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke());
            };
        }

        public void OnClose(Action callback)
        {
            OnCloseEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke());
            };
        }

        public void OnReceive(Action<byte[]> callback)
        {
            OnDataEvent += (_, e) =>
            {
                Callback.Execute(() => callback?.Invoke(e));
            };
        }

        private void Init()
        {
            Async.Thread(() =>
            {
                Thread.Sleep(100);
                OnOpenEvent?.Invoke(this, null);
                var buffer = new byte[1024 * 8];

                do
                {
                    try
                    {
                        var length = Stream.Read(buffer, 0, buffer.Length);
                        var data = new byte[length];
                        Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                        OnDataEvent?.Invoke(this, data);
                    }
                    catch { }
                }
                while (Opened);
            });
        }

        private bool Connected()
        {
            var connected = Available();

            if(!Opened && !closed) Close();

            return connected;
        }

        private bool Available()
        {
            if (Socket == null || !Socket.Connected) return false;
            try { return !(Socket.Poll(Timeout, SelectMode.SelectRead) && Socket.Available == 0); }
            catch { return false; }
        }

        public void Close()
        {
            if (tryClose || closed) return;
            tryClose = true;
            Socket.Shutdown(SocketShutdown.Both);
            
            Async.Thread(() =>
            {
                Stream?.Close();
                Socket?.Close();
                closed = true;
                tryClose = false;
                OnCloseEvent?.Invoke(this, null);
            });
        }
    }
}