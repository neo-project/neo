using System;

namespace Neo.Network.P2P
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class PayloadAttribute : Attribute
    {
        public Type Type { get; }

        public PayloadAttribute(Type type)
        {
            Type = type;
        }
    }
}
