using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract
{
    public static partial class InteropService
    {
        private static readonly Dictionary<uint, InteropDescriptor> methods = new Dictionary<uint, InteropDescriptor>();

        public static long GetPrice(uint hash, EvaluationStack stack)
        {
            return methods[hash].GetPrice(stack);
        }

        public static Dictionary<uint, string> SupportedMethods()
        {
            return methods.ToDictionary(p => p.Key, p => p.Value.Method);
        }

        internal static bool Invoke(ApplicationEngine engine, uint method)
        {
            if (!methods.TryGetValue(method, out InteropDescriptor descriptor))
                return false;
            if (!descriptor.AllowedTriggers.HasFlag(engine.Trigger))
                return false;
            return descriptor.Handler(engine);
        }

        private static uint Register(string method, Func<ApplicationEngine, bool> handler, long price, TriggerType allowedTriggers)
        {
            InteropDescriptor descriptor = new InteropDescriptor(method, handler, price, allowedTriggers);
            methods.Add(descriptor.Hash, descriptor);
            return descriptor.Hash;
        }

        private static uint Register(string method, Func<ApplicationEngine, bool> handler, Func<EvaluationStack, long> priceCalculator, TriggerType allowedTriggers)
        {
            InteropDescriptor descriptor = new InteropDescriptor(method, handler, priceCalculator, allowedTriggers);
            methods.Add(descriptor.Hash, descriptor);
            return descriptor.Hash;
        }
    }
}
