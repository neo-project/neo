using System;

namespace Neo.SmartContract.Native
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class ContractMethodAttribute : Attribute
    {
        public string Name { get; set; }
        public long Price { get; }
        public ContractParameterType ReturnType { get; }
        public ContractParameterType[] ParameterTypes { get; set; } = Array.Empty<ContractParameterType>();
        public string[] ParameterNames { get; set; } = Array.Empty<string>();
        public CallFlags RequiredCallFlags { get; }

        public ContractMethodAttribute(long price, ContractParameterType returnType, CallFlags requiredCallFlags)
        {
            this.Price = price;
            this.ReturnType = returnType;
            this.RequiredCallFlags = requiredCallFlags;
        }
    }
}
