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
        public CallFlags RequiredCallFlags { get; }

        internal InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, long price, TriggerType allowedTriggers, CallFlags requiredCallFlags)
            : this(method, handler, allowedTriggers, requiredCallFlags)
        {
            this.Price = price;
        }

        internal InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, Func<ApplicationEngine, long> priceCalculator, TriggerType allowedTriggers, CallFlags requiredCallFlags)
            : this(method, handler, allowedTriggers, requiredCallFlags)
        {
            this.StoragePriceCalculator = priceCalculator;
            this.IsStateful = true;
        }

        internal InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, Func<EvaluationStack, long> priceCalculator, TriggerType allowedTriggers, CallFlags requiredCallFlags)
            : this(method, handler, allowedTriggers, requiredCallFlags)
        {
            this.PriceCalculator = priceCalculator;
        }

        private InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, TriggerType allowedTriggers, CallFlags requiredCallFlags)
        {
            this.Method = method;
            this.Hash = method.ToInteropMethodHash();
            this.Handler = handler;
            this.AllowedTriggers = allowedTriggers;
            this.RequiredCallFlags = requiredCallFlags;
        }


        public long GetPrice()
        {
            return Price;
        }

        public long GetPrice(ApplicationEngine applicationEngine)
        {
            long price = Price;

            if (!IsStateful && PriceCalculator != null)
            {
                price = PriceCalculator(applicationEngine.CurrentContext.EvaluationStack);
            }
            else if (IsStateful && StoragePriceCalculator != null)
            {
                price = StoragePriceCalculator(applicationEngine);
            }

            return price;
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
