using Neo.IO;
using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network
{
    internal class WebSocketRemoteNode : RemoteNode
    {
        private WebSocket socket;
        private bool connected = false;
        private int disposed = 0;

        public WebSocketRemoteNode(LocalNode localNode, WebSocket socket, IPEndPoint remoteEndpoint)
            : base(localNode)
        {
            this.socket = socket;
            this.RemoteEndpoint = new IPEndPoint(remoteEndpoint.Address.MapToIPv6(), remoteEndpoint.Port);
            this.connected = true;
        }

        public override void Disconnect(bool error)
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                socket.Dispose();
                base.Disconnect(error);
            }
        }

        protected override async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            CancellationTokenSource source = new CancellationTokenSource(timeout);
            try
            {
                return await Message.DeserializeFromAsync(socket, source.Token);
            }
            catch (ArgumentException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex) when (ex is FormatException || ex is IOException || ex is WebSocketException || ex is OperationCanceledException)
            {
                Disconnect(false);
            }
            finally
            {
                source.Dispose();
            }
            return null;
        }

        protected override async Task<bool> SendMessageAsync(Message message)
        {
            if (!connected) throw new InvalidOperationException();
            if (disposed > 0) return false;
            ArraySegment<byte> segment = new ArraySegment<byte>(message.ToArray());
            CancellationTokenSource source = new CancellationTokenSource(10000);
            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Binary, true, source.Token);
                return true;
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) when (ex is WebSocketException || ex is OperationCanceledException)
            {
                Disconnect(false);
            }
            finally
            {
                source.Dispose();
            }
            return false;
        }
    }
}
