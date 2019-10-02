using System;

namespace Neo.Network.RPC.Server
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RpcControllerAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
