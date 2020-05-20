using Neo.Cryptography;
using System;
using System.Text;

namespace Neo.SmartContract
{
    public class InteropDescriptor
    {
        public string Method { get; }
        public uint Hash { get; }
        internal Func<ApplicationEngine, bool> Handler { get; }
        public long FixedPrice { get; }
        public TriggerType AllowedTriggers { get; }
        public CallFlags RequiredCallFlags { get; }

        internal InteropDescriptor(string method, Func<ApplicationEngine, bool> handler, long fixedPrice, TriggerType allowedTriggers, CallFlags requiredCallFlags)
        {
            this.Method = method;
            this.Hash = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(method).Sha256(), 0);
            this.Handler = handler;
            this.FixedPrice = fixedPrice;
            this.AllowedTriggers = allowedTriggers;
            this.RequiredCallFlags = requiredCallFlags;
        }

        public static implicit operator uint(InteropDescriptor descriptor)
        {
            return descriptor.Hash;
        }
    }
}
