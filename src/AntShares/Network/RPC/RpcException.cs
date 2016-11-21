using System;

namespace AntShares.Network.RPC
{
    public class RpcException : Exception
    {
        public RpcException(int code, string message) : base(message)
        {
            HResult = code;
        }
    }
}
