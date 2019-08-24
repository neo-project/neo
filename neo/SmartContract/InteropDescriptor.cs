using Neo.VM;
using System;

namespace Neo.SmartContract
{
    internal class InteropDescriptor
    {
        public string Method { get; }
        public uint Hash { get; }
        public Func<ApplicationEngine, bool> Handler { get; }
        public long Price { get; }
        public Func<RandomAccessStack<StackItem>, long> PriceCalculator { get; }
        public TriggerType AllowedTriggers { get; }
        public bool RequireWriteAccess { get; }

        public InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, long price, TriggerType allowedTriggers, bool requireWriteAccess)
            : this(method, handler, allowedTriggers, requireWriteAccess)
        {
            this.Price = price;
        }

        public InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, Func<RandomAccessStack<StackItem>, long> priceCalculator, TriggerType allowedTriggers, bool requireWriteAccess)
            : this(method, handler, allowedTriggers, requireWriteAccess)
        {
            this.PriceCalculator = priceCalculator;
        }

        private InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, TriggerType allowedTriggers, bool requireWriteAccess)
        {
            this.Method = method;
            this.Hash = method.ToInteropMethodHash();
            this.Handler = handler;
            this.AllowedTriggers = allowedTriggers;
            this.RequireWriteAccess = requireWriteAccess;
        }

        public long GetPrice(RandomAccessStack<StackItem> stack)
        {
            return PriceCalculator is null ? Price : PriceCalculator(stack);
        }
    }
}
