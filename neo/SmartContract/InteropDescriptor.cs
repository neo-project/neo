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

        public InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, long price, TriggerType allowedTriggers)
            : this(method, handler, allowedTriggers)
        {
            this.Price = price;
        }

        public InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, Func<RandomAccessStack<StackItem>, long> priceCalculator, TriggerType allowedTriggers)
            : this(method, handler, allowedTriggers)
        {
            this.PriceCalculator = priceCalculator;
        }

        private InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, TriggerType allowedTriggers)
        {
            this.Method = method;
            this.Hash = method.ToInteropMethodHash();
            this.Handler = handler;
            this.AllowedTriggers = allowedTriggers;
        }

        public long GetPrice(RandomAccessStack<StackItem> stack)
        {
            return PriceCalculator is null ? Price : PriceCalculator(stack);
        }
    }
}
