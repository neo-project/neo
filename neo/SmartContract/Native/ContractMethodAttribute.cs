using System;

namespace Neo.SmartContract.Native
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class ContractMethodAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
