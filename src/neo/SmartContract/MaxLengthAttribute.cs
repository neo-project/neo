using System;

namespace Neo.SmartContract
{
    [AttributeUsage(AttributeTargets.Parameter)]
    internal class MaxLengthAttribute : Attribute
    {
        public readonly int MaxLength;

        public MaxLengthAttribute(int maxLength)
        {
            MaxLength = maxLength;
        }
    }
}
