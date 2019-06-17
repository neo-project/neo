using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SDK
{
    public class NeoSdkException : Exception
    {
        public NeoSdkException(int code, string message, object data = null) : base(message)
        {
            HResult = code;
            Data.Add("data", data);
        }

        public NeoSdkException(string message, object data = null) : base(message)
        {
            Data.Add("data", data);
        }

    }
}
