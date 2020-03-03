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
        public Func<ApplicationEngine, long> PriceCalculator { get; }
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
            this.PriceCalculator = priceCalculator;
        }

        internal InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, Func<EvaluationStack, long> priceCalculator, TriggerType allowedTriggers, CallFlags requiredCallFlags)
            : this(method, handler, allowedTriggers, requiredCallFlags)
        {
            this.PriceCalculator = (engine) => priceCalculator(engine.CurrentContext.EvaluationStack);
        }

        private InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, TriggerType allowedTriggers, CallFlags requiredCallFlags)
        {
            this.Method = method;
            this.Hash = method.ToInteropMethodHash();
            this.Handler = handler;
            this.AllowedTriggers = allowedTriggers;
            this.RequiredCallFlags = requiredCallFlags;
        }

        public long GetPrice(ApplicationEngine applicationEngine)
        {
            return PriceCalculator is null ? Price : PriceCalculator(applicationEngine);
        }

        public static implicit operator uint(InteropDescriptor descriptor)
        {
            return descriptor.Hash;
        }
    }
}
