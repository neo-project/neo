using Neo.VM;
using System;

namespace Neo.SmartContract
{
    public class InteropDescriptor
    {
        public string Method { get; }
        public uint Hash { get; }
        internal Func<ApplicationEngine, bool> Handler { get; }
        public uint Price { get; }
        public Func<EvaluationStack, uint> PriceCalculator { get; }
        public TriggerType AllowedTriggers { get; }
        public CallFlags RequiredCallFlags { get; }

        internal InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, uint price, TriggerType allowedTriggers, CallFlags requiredCallFlags)
            : this(method, handler, allowedTriggers, requiredCallFlags)
        {
            this.Price = price;
        }

        internal InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, Func<EvaluationStack, uint> priceCalculator, TriggerType allowedTriggers, CallFlags requiredCallFlags)
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

        public uint GetPrice(EvaluationStack stack)
        {
            return PriceCalculator is null ? Price : PriceCalculator(stack);
        }

        public static implicit operator uint(InteropDescriptor descriptor)
        {
            return descriptor.Hash;
        }
    }
}
