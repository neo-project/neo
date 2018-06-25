using Akka.Actor;
using Akka.IO;
using System;
using System.Net;
using System.Threading;

namespace Neo.Network.P2P
{
    public abstract class Connection : UntypedActor
    {
        public IPEndPoint Remote { get; }
        public IPEndPoint Local { get; }
        public abstract int ListenerPort { get; }

        private readonly Timer timer;
        private IActorRef tcp;

        protected Connection(IActorRef tcp, IPEndPoint remote, IPEndPoint local)
        {
            this.tcp = tcp;
            this.timer = new Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);
            this.Remote = remote;
            this.Local = local;
            timer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
        }

        public void Disconnect()
        {
            tcp.Tell(Tcp.Close.Instance);
        }

        protected abstract void OnData(ByteString data);

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Tcp.Received received:
                    timer.Change(TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan);
                    try
                    {
                        OnData(received.Data);
                    }
                    catch
                    {
                        tcp.Tell(Tcp.Abort.Instance);
                    }
                    break;
                case Tcp.ConnectionClosed _:
                    Context.Stop(Self);
                    break;
            }
        }

        private void OnTimer(object state)
        {
            tcp.Tell(Tcp.Abort.Instance);
        }

        protected override void PostStop()
        {
            timer.Dispose();
            base.PostStop();
        }

        protected void SendData(ByteString data)
        {
            tcp.Tell(Tcp.Write.Create(data));
        }
    }
}
