using Neo.VM;
using System;

namespace Neo.SmartContract
{
    public class InteropDescriptor
    {
        public string Method { get; }
        public uint Hash { get; }
        internal Func<ApplicationEngine, bool> Handler { get; }
        public long Price { get; }
        public Func<EvaluationStack, long> PriceCalculator { get; }
        public Func<ApplicationEngine, long> StoragePriceCalculator { get; }
        public bool IsStateful { get; }
        public TriggerType AllowedTriggers { get; }

        internal InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, long price, TriggerType allowedTriggers)
            : this(method, handler, allowedTriggers)
        {
            this.Price = price;
        }

        internal InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, Func<EvaluationStack, long> priceCalculator, TriggerType allowedTriggers)
            : this(method, handler, allowedTriggers)
        {
            this.PriceCalculator = priceCalculator;
        }

        public InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, Func<ApplicationEngine, long> priceCalculator, TriggerType allowedTriggers)
            : this(method, handler, allowedTriggers)
        {
            this.StoragePriceCalculator = priceCalculator;
            this.IsStateful = true;
        }

        private InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, TriggerType allowedTriggers)
        {
            this.Method = method;
            this.Hash = method.ToInteropMethodHash();
            this.Handler = handler;
            this.AllowedTriggers = allowedTriggers;
        }

        public long GetPrice()
        {
            return Price;
        }

        public long GetPrice(ApplicationEngine applicationEngine)
        {
            return StoragePriceCalculator is null ? Price : StoragePriceCalculator(applicationEngine);
        }

        public long GetPrice(EvaluationStack stack)
        {
#if DEBUG
            if (IsStateful)
            {
                throw new InvalidOperationException();
            }
#endif
            return PriceCalculator is null ? Price : PriceCalculator(stack);
        }

        public static implicit operator uint(InteropDescriptor descriptor)
        {
            return descriptor.Hash;
        }
    }
}
