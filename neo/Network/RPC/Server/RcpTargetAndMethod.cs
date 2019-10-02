using System.Reflection;

namespace Neo.Network.RPC.Server
{
    internal class RcpTargetAndMethod
    {
        public object Target { get; set; }

        public MethodInfo Method { get; set; }
    }
}
