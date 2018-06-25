using Akka.Actor;
using Akka.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Neo.Network.P2P
{
    public abstract class Peer : UntypedActor
    {
        public class Start { public int Port; }
        public class Peers { public IEnumerable<IPEndPoint> EndPoints; }
        public class Connect { public IPEndPoint EndPoint; }
        private class Timer { }

        private const int MaxConnectionsPerAddress = 3;

        private static readonly IActorRef tcp = Context.System.Tcp();
        private ICancelable timer;
        protected ActorSelection Connections => Context.ActorSelection("connection_*");

        private static readonly HashSet<IPAddress> localAddresses = new HashSet<IPAddress>();
        private readonly Dictionary<IPAddress, int> ConnectedAddresses = new Dictionary<IPAddress, int>();
        protected readonly Dictionary<IActorRef, IPEndPoint> ConnectedPeers = new Dictionary<IActorRef, IPEndPoint>();
        protected readonly HashSet<IPEndPoint> UnconnectedPeers = new HashSet<IPEndPoint>();
        //TODO: badPeers

        public int ListenerPort { get; private set; }
        protected abstract int ConnectedMax { get; }
        protected abstract int UnconnectedMax { get; }

        static Peer()
        {
            localAddresses.UnionWith(NetworkInterface.GetAllNetworkInterfaces().SelectMany(p => p.GetIPProperties().UnicastAddresses).Select(p => p.Address.Unmap()));
        }

        protected void AddPeers(IEnumerable<IPEndPoint> peers)
        {
            if (UnconnectedPeers.Count < UnconnectedMax)
            {
                peers = peers.Where(p => p.Port != ListenerPort || !localAddresses.Contains(p.Address));
                UnconnectedPeers.UnionWith(peers);
            }
        }

        protected void ConnectToPeer(IPEndPoint endPoint)
        {
            endPoint = endPoint.Unmap();
            if (endPoint.Port == ListenerPort && localAddresses.Contains(endPoint.Address)) return;
            if (ConnectedAddresses.TryGetValue(endPoint.Address, out int count) && count >= MaxConnectionsPerAddress)
                return;
            if (ConnectedPeers.Values.Contains(endPoint)) return;
            tcp.Tell(new Tcp.Connect(endPoint));
        }

        private static bool IsIntranetAddress(IPAddress address)
        {
            byte[] data = address.MapToIPv4().GetAddressBytes();
            Array.Reverse(data);
            uint value = data.ToUInt32(0);
            return (value & 0xff000000) == 0x0a000000 || (value & 0xff000000) == 0x7f000000 || (value & 0xfff00000) == 0xac100000 || (value & 0xffff0000) == 0xc0a80000 || (value & 0xffff0000) == 0xa9fe0000;
        }

        protected abstract void NeedMorePeers(int count);

        private void OnConnected(IPEndPoint remote, IPEndPoint local)
        {
            IActorRef connection = Context.ActorOf(
                props: ProtocolProps(Sender, remote, local),
                name: $"connection_{Guid.NewGuid()}");
            Context.Watch(connection);
            Sender.Tell(new Tcp.Register(connection));
            ConnectedAddresses.TryGetValue(remote.Address, out int count);
            ConnectedAddresses[remote.Address] = ++count;
            ConnectedPeers.Add(connection, remote);
            if (count > MaxConnectionsPerAddress)
                Sender.Tell(Tcp.Abort.Instance);
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Start start:
                    OnStart(start.Port);
                    break;
                case Timer _:
                    OnTimer();
                    break;
                case Peers peers:
                    AddPeers(peers.EndPoints);
                    break;
                case Connect connect:
                    ConnectToPeer(connect.EndPoint);
                    break;
                case Tcp.Connected connected:
                    OnConnected(((IPEndPoint)connected.RemoteAddress).Unmap(), ((IPEndPoint)connected.LocalAddress).Unmap());
                    break;
                case Tcp.Bound _:
                case Tcp.CommandFailed _:
                    break;
                case Terminated terminated:
                    OnTerminated(terminated.ActorRef);
                    break;
            }
        }

        private void OnStart(int port)
        {
            ListenerPort = port;
            timer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(0, 5000, Context.Self, new Timer(), ActorRefs.NoSender);
            if (ListenerPort > 0)
            {
                if (localAddresses.All(p => !p.IsIPv4MappedToIPv6 || IsIntranetAddress(p)) && UPnP.Discover())
                {
                    try
                    {
                        localAddresses.Add(UPnP.GetExternalIP());
                        UPnP.ForwardPort(ListenerPort, ProtocolType.Tcp, "NEO");
                    }
                    catch { }
                }
                tcp.Tell(new Tcp.Bind(Self, new IPEndPoint(IPAddress.Any, ListenerPort), options: new[] { new Inet.SO.ReuseAddress(true) }));
                //TODO: Websocket
            }
        }

        private void OnTerminated(IActorRef actorRef)
        {
            if (ConnectedPeers.TryGetValue(actorRef, out IPEndPoint endPoint))
            {
                ConnectedAddresses.TryGetValue(endPoint.Address, out int count);
                if (count > 0) count--;
                if (count == 0)
                    ConnectedAddresses.Remove(endPoint.Address);
                else
                    ConnectedAddresses[endPoint.Address] = count;
                ConnectedPeers.Remove(actorRef);
            }
        }

        private void OnTimer()
        {
            if (ConnectedPeers.Count >= ConnectedMax) return;
            if (UnconnectedPeers.Count == 0)
                NeedMorePeers(ConnectedMax - ConnectedPeers.Count);
            IPEndPoint[] endpoints = UnconnectedPeers.Take(ConnectedMax - ConnectedPeers.Count).ToArray();
            foreach (IPEndPoint endpoint in endpoints)
            {
                ConnectToPeer(endpoint);
                UnconnectedPeers.Remove(endpoint);
            }
        }

        protected override void PostStop()
        {
            timer.CancelIfNotNull();
            base.PostStop();
        }

        protected abstract Props ProtocolProps(IActorRef tcp, IPEndPoint remote, IPEndPoint local);
    }
}
