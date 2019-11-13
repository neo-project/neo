using System;

namespace Neo.Network
{
    public class HttpException : Exception
    {
        public HttpException(int code, string message) : base(message)
        {
            HResult = code;
        }
    }
}
