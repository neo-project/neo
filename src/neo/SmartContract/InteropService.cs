using System;
using System.Collections.Generic;
using System.Reflection;

namespace Neo.SmartContract
{
    public static partial class InteropService
    {
        private static readonly Dictionary<uint, InteropDescriptor> methods = new Dictionary<uint, InteropDescriptor>();

        static InteropService()
        {
            foreach (Type t in typeof(InteropService).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                t.GetFields()[0].GetValue(null);
        }

        public static IEnumerable<InteropDescriptor> SupportedMethods()
        {
            return methods.Values;
        }

        internal static bool Invoke(ApplicationEngine engine, uint method)
        {
            if (!methods.TryGetValue(method, out InteropDescriptor descriptor))
                return false;
            if (!descriptor.AllowedTriggers.HasFlag(engine.Trigger))
                return false;
            ExecutionContextState state = engine.CurrentContext.GetState<ExecutionContextState>();
            if (!state.CallFlags.HasFlag(descriptor.RequiredCallFlags))
                return false;
            if (!engine.AddGas(descriptor.FixedPrice))
                return false;
            return descriptor.Handler(engine);
        }

        private static InteropDescriptor Register(string method, Func<ApplicationEngine, bool> handler, long price, TriggerType allowedTriggers, CallFlags requiredCallFlags)
        {
            InteropDescriptor descriptor = new InteropDescriptor(method, handler, price, allowedTriggers, requiredCallFlags);
            methods.Add(descriptor.Hash, descriptor);
            return descriptor;
        }
    }
}
