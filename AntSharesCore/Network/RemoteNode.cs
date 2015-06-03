using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AntShares.Network
{
    public class RemoteNode : IDisposable
    {
        public event EventHandler<bool> Disconnected;
        internal event EventHandler<IPEndPoint[]> NewPeers;

        private LocalNode localNode;
        private TcpClient tcp = new TcpClient();
        private BinaryReader reader;
        private BinaryWriter writer;
        private int disposed = 0;

        public bool Connected { get; private set; }

        public IPEndPoint RemoteEndpoint { get; private set; }

        public byte Version { get; private set; }

        internal RemoteNode(LocalNode localNode, IPEndPoint remoteEndpoint)
        {
            this.localNode = localNode;
            this.RemoteEndpoint = remoteEndpoint;
        }

        internal async Task<bool> ConnectAsync()
        {
            try
            {
                await tcp.ConnectAsync(RemoteEndpoint.Address, RemoteEndpoint.Port);
            }
            catch (SocketException)
            {
                Disconnect(true);
                return false;
            }
            reader = new BinaryReader(tcp.GetStream(), Encoding.UTF8, true);
            writer = new BinaryWriter(tcp.GetStream(), Encoding.UTF8, true);
            Connected = true;
            StartProtocol();
            return true;
        }

        public void Disconnect(bool error)
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                Connected = false;
                if (reader != null)
                    reader.Close();
                if (writer != null)
                    writer.Close();
                tcp.Close();
                if (Disconnected != null)
                {
                    Disconnected(this, error);
                }
            }
        }

        public void Dispose()
        {
            Disconnect(false);
        }

        private void ReceiveLoop()
        {
            while (Connected)
            {
                Message message = new Message();
                try
                {
                    message.Deserialize(reader);
                }
                catch (FormatException)
                {
                    Disconnect(true);
                    break;
                }
                catch (IOException)
                {
                    Disconnect(true);
                    break;
                }
                //OnMessageReceived(message);
            }
        }

        internal async Task RequestPeersAsync()
        {
        }

        private async void StartProtocol()
        {
            Thread thread = new Thread(ReceiveLoop);
            thread.Name = string.Format("ReceiveLoop@{0}", RemoteEndpoint);
            thread.Start();
            //await SendMessageAsync("version", VersionPayload.Create(localEndpoint, RemoteEndpoint, height));
        }
    }
}
