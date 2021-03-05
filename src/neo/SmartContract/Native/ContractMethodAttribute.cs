using System;

namespace Neo.SmartContract.Native
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    internal class ContractMethodAttribute : Attribute
    {
        public string Name { get; init; }
        public CallFlags RequiredCallFlags { get; init; }
        public long CpuFee { get; init; }
        public long StorageFee { get; init; }
    }
}
