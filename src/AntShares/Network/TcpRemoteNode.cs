using AntShares.IO;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Network
{
    internal class TcpRemoteNode : RemoteNode
    {
        private Socket socket;
        private NetworkStream stream;
        private bool connected = false;
        private int disposed = 0;

        public TcpRemoteNode(LocalNode localNode, IPEndPoint remoteEndpoint)
            : base(localNode)
        {
            this.socket = new Socket(remoteEndpoint.Address.IsIPv4MappedToIPv6 ? AddressFamily.InterNetwork : remoteEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.ListenerEndpoint = remoteEndpoint;
        }

        public TcpRemoteNode(LocalNode localNode, Socket socket)
            : base(localNode)
        {
            this.socket = socket;
            OnConnected();
        }

        public async Task<bool> ConnectAsync()
        {
            IPAddress address = ListenerEndpoint.Address;
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            try
            {
                await socket.ConnectAsync(address, ListenerEndpoint.Port);
                OnConnected();
            }
            catch (SocketException)
            {
                Disconnect(false);
                return false;
            }
            return true;
        }

        public override void Disconnect(bool error)
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                if (stream != null) stream.Dispose();
                socket.Dispose();
                base.Disconnect(error);
            }
        }

        private void OnConnected()
        {
            IPEndPoint remoteEndpoint = (IPEndPoint)socket.RemoteEndPoint;
            RemoteEndpoint = new IPEndPoint(remoteEndpoint.Address.MapToIPv6(), remoteEndpoint.Port);
            socket.SendTimeout = 10000;
            stream = new NetworkStream(socket);
            connected = true;
        }

        protected override Message ReceiveMessage(TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan) timeout = TimeSpan.Zero;
            BinaryReader reader = null;
            try
            {
                reader = new BinaryReader(stream, Encoding.UTF8, true);
                socket.ReceiveTimeout = (int)timeout.TotalMilliseconds;
                return reader.ReadSerializable<Message>();
            }
            catch (ArgumentException) { }
            catch (ObjectDisposedException) { }
            catch (FormatException)
            {
                Disconnect(true);
            }
            catch (IOException)
            {
                Disconnect(false);
            }
            finally
            {
                if (reader != null) reader.Dispose();
            }
            return null;
        }

        protected override bool SendMessage(Message message)
        {
            if (!connected) throw new InvalidOperationException();
            if (disposed > 0) return false;
            byte[] buffer = message.ToArray();
            try
            {
                stream.Write(buffer, 0, buffer.Length);
                return true;
            }
            catch (ObjectDisposedException) { }
            catch (IOException)
            {
                Disconnect(false);
            }
            return false;
        }
    }
}
