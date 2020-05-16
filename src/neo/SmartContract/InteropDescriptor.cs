using Neo.SmartContract.Native;
using System;
using System.Reflection;

namespace Neo.SmartContract
{
    public class InteropDescriptor
    {
        public string Name { get; }
        public uint Hash { get; }
        internal Func<ApplicationEngine, bool> Handler { get; }
        public long Price { get; }
        public TriggerType AllowedTriggers { get; }
        public CallFlags RequiredCallFlags { get; }

        internal InteropDescriptor(InteropServiceAttribute attribute, MethodInfo handler)
            : this(attribute.Name, (Func<ApplicationEngine, bool>)handler.CreateDelegate(typeof(Func<ApplicationEngine, bool>)), attribute.Price, attribute.AllowedTriggers, attribute.RequiredCallFlags)
        {
        }

        internal InteropDescriptor(NativeContract contract)
            : this(contract.ServiceName, contract.Invoke, 0, TriggerType.System | TriggerType.Application, CallFlags.None)
        {
        }

        private InteropDescriptor(string name, Func<ApplicationEngine, bool> handler, long price, TriggerType allowedTriggers, CallFlags requiredCallFlags)
        {
            this.Name = name;
            this.Hash = name.ToInteropMethodHash();
            this.Handler = handler;
            this.Price = price;
            this.AllowedTriggers = allowedTriggers;
            this.RequiredCallFlags = requiredCallFlags;
        }
    }
}
