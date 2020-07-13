using System;

namespace Neo.SmartContract.Native
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    internal class ContractMethodAttribute : Attribute
    {
        public string Name { get; set; }
        public long Price { get; }
        public CallFlags RequiredCallFlags { get; }
        public uint CacheKey { get; }
        public uint[] CleanCacheKeys { get; }

        public ContractMethodAttribute(long price, CallFlags requiredCallFlags, uint cacheKey = uint.MinValue, params uint[] cleanCacheKeys)
        {
            this.Price = price;
            this.RequiredCallFlags = requiredCallFlags;
            this.CacheKey = cacheKey;
            this.CleanCacheKeys = cleanCacheKeys;
        }
    }
}
