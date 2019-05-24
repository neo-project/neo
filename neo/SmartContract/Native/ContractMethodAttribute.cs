using System;

namespace Neo.SmartContract.Native
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class ContractMethodAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
