using AntShares.IO;
using AntShares.Network.Payloads;
using System;
using System.IO;
using System.Linq;
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
        private TcpClient tcp;
        private BinaryReader reader;
        private BinaryWriter writer;
        private bool connected = false;
        private int disposed = 0;

        public IPEndPoint RemoteEndpoint { get; private set; }

        public byte Version { get; private set; }

        internal RemoteNode(LocalNode localNode, IPEndPoint remoteEndpoint)
        {
            this.localNode = localNode;
            this.tcp = new TcpClient();
            this.RemoteEndpoint = remoteEndpoint;
        }

        internal RemoteNode(LocalNode localNode, TcpClient tcp)
        {
            this.localNode = localNode;
            this.tcp = tcp;
            OnConnected();
        }

        internal async Task ConnectAsync()
        {
            try
            {
                await tcp.ConnectAsync(RemoteEndpoint.Address, RemoteEndpoint.Port);
            }
            catch (SocketException)
            {
                Disconnect(true);
                return;
            }
            OnConnected();
            await StartProtocolAsync();
        }

        public void Disconnect(bool error)
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                if (reader != null)
                    reader.Close();
                if (writer != null)
                    lock (writer)
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

        private void OnConnected()
        {
            reader = new BinaryReader(tcp.GetStream(), Encoding.UTF8, true);
            writer = new BinaryWriter(tcp.GetStream(), Encoding.UTF8, true);
            connected = true;
        }

        private void ReceiveLoop()
        {
            while (disposed == 0)
            {
                Message message = ReceiveMessage();
                if (message == null)
                    break;
                //OnMessageReceived(message);
            }
        }

        private Message ReceiveMessage()
        {
            try
            {
                return reader.ReadSerializable<Message>();
            }
            catch (FormatException)
            {
                Disconnect(true);
            }
            catch (IOException)
            {
                Disconnect(true);
            }
            return null;
        }

        private async Task<Message> ReceiveMessageAsync()
        {
            return await Task.Run(() =>
            {
                return ReceiveMessage();
            });
        }

        internal async Task RequestPeersAsync()
        {

        }

        private bool SendMessage(Message message)
        {
            if (!connected)
                throw new InvalidOperationException();
            if (disposed > 0)
                return false;
            try
            {
                lock (writer)
                {
                    writer.Write(message);
                }
                return true;
            }
            catch (ObjectDisposedException) { }
            catch (IOException)
            {
                Disconnect(true);
            }
            return false;
        }

        private bool SendMessage(string command, Payload payload = null)
        {
            return SendMessage(Message.Create(command, payload));
        }

        private async Task<bool> SendMessageAsync(Message message)
        {
            return await Task.Run(() =>
            {
                return SendMessage(message);
            });
        }

        private async Task<bool> SendMessageAsync(string command, Payload payload = null)
        {
            return await SendMessageAsync(Message.Create(command, payload));
        }

        internal async Task StartProtocolAsync()
        {
            if (!await SendMessageAsync("version", VersionPayload.Create(localNode.LocalEndpoint.Port, localNode.UserAgent, 0)))
                return;
            Message message = await ReceiveMessageAsync();
            if (message == null)
                return;
            if (message.Command != "version")
            {
                Disconnect(true);
                return;
            }
            VersionPayload payload;
            try
            {
                payload = Payload.FromBytes<VersionPayload>(message.Payload);
            }
            catch (FormatException)
            {
                Disconnect(true);
                return;
            }
            if (RemoteEndpoint != null && RemoteEndpoint.Port != payload.Port)
            {
                Disconnect(true);
                return;
            }
            if (RemoteEndpoint == null)
            {
                IPEndPoint remoteEndpoint = new IPEndPoint(((IPEndPoint)tcp.Client.RemoteEndPoint).Address, payload.Port);
                lock (localNode.pendingPeers)
                {
                    lock (localNode.connectedPeers)
                    {
                        if (localNode.pendingPeers.All(p => p.RemoteEndpoint != remoteEndpoint) && !localNode.connectedPeers.ContainsKey(remoteEndpoint))
                        {
                            RemoteEndpoint = remoteEndpoint;
                        }
                    }
                }
                if (RemoteEndpoint == null)
                {
                    Disconnect(false);
                    return;
                }
            }
            if (!await SendMessageAsync("verack"))
                return;
            message = await ReceiveMessageAsync();
            if (message == null)
                return;
            if (message.Command != "verack")
            {
                Disconnect(true);
                return;
            }
            lock (localNode.pendingPeers)
            {
                lock (localNode.connectedPeers)
                {
                    localNode.connectedPeers.Add(RemoteEndpoint, this);
                }
                localNode.pendingPeers.Remove(this);
            }
            Thread thread = new Thread(ReceiveLoop);
            thread.Name = string.Format("ReceiveLoop@{0}", RemoteEndpoint);
            thread.Start();
        }
    }
}
