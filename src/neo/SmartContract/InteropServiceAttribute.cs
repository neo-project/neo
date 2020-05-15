using System;

namespace Neo.SmartContract
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class InteropServiceAttribute : Attribute
    {
        public string Method { get; }
        public long Price { get; }
        public TriggerType AllowedTriggers { get; }
        public CallFlags RequiredCallFlags { get; }

        public InteropServiceAttribute(string method, long price, TriggerType allowedTriggers, CallFlags requiredCallFlags)
        {
            this.Method = method;
            this.Price = price;
            this.AllowedTriggers = allowedTriggers;
            this.RequiredCallFlags = requiredCallFlags;
        }
    }
}
