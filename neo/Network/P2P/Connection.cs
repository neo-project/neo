using Akka.Actor;
using Akka.IO;
using System;
using System.Net;

namespace Neo.Network.P2P
{
    public abstract class Connection : UntypedActor
    {
        internal class Ack : Tcp.Event { public static Ack Instance = new Ack(); }

        public IPEndPoint Remote { get; }
        public IPEndPoint Local { get; }
        public abstract int ListenerPort { get; }

        private ICancelable timer;
        protected readonly IActorRef tcp;
        protected bool ack = true;

        protected Connection(IActorRef tcp, IPEndPoint remote, IPEndPoint local)
        {
            this.timer = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromSeconds(10), tcp, Tcp.Abort.Instance, ActorRefs.NoSender);
            this.tcp = tcp;
            this.Remote = remote;
            this.Local = local;
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
                case Ack _:
                    ack = true;
                    break;
                case Tcp.Received received:
                    OnReceived(received.Data);
                    break;
                case Tcp.ConnectionClosed _:
                    Context.Stop(Self);
                    break;
            }
        }

        private void OnReceived(ByteString data)
        {
            timer.CancelIfNotNull();
            timer = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromMinutes(1), tcp, Tcp.Abort.Instance, ActorRefs.NoSender);
            try
            {
                OnData(data);
            }
            catch
            {
                tcp.Tell(Tcp.Abort.Instance);
            }
        }

        protected override void PostStop()
        {
            timer.CancelIfNotNull();
            base.PostStop();
        }

        protected void SendData(ByteString data)
        {
            ack = false;
            tcp.Tell(Tcp.Write.Create(data, Ack.Instance));
        }
    }
}
