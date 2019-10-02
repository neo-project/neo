using System;

namespace Neo.Network.RPC.Server
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcMethodAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
