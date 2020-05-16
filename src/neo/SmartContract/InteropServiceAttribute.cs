using System;

namespace Neo.SmartContract
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class InteropServiceAttribute : Attribute
    {
        public string Name { get; }
        public long Price { get; }
        public TriggerType AllowedTriggers { get; }
        public CallFlags RequiredCallFlags { get; }

        public InteropServiceAttribute(string name, long price, TriggerType allowedTriggers, CallFlags requiredCallFlags)
        {
            this.Name = name;
            this.Price = price;
            this.AllowedTriggers = allowedTriggers;
            this.RequiredCallFlags = requiredCallFlags;
        }
    }
}
