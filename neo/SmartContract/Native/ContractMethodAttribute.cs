using System;

namespace Neo.SmartContract.Native
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class ContractMethodAttribute : Attribute
    {
        public string Name { get; set; }
        public ContractParameterType ReturnType { get; }
        public ContractParameterType[] ParameterTypes { get; set; } = new ContractParameterType[0];
        public string[] ParameterNames { get; set; } = new string[0];

        public ContractMethodAttribute(ContractParameterType returnType)
        {
            this.ReturnType = returnType;
        }
    }
}
