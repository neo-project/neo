using System;

namespace Neo.SmartContract.Native
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    internal class ContractMethodAttribute : Attribute
    {
        public string Name { get; set; }
        public long Price { get; }
        public CallFlags RequiredCallFlags { get; }

        public ContractMethodAttribute(long price, CallFlags requiredCallFlags)
        {
            this.Price = price;
            this.RequiredCallFlags = requiredCallFlags;
        }
    }
}
