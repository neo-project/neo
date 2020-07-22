using Neo.Cryptography;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Neo.SmartContract
{
    public class InteropDescriptor
    {
        public string Name { get; }
        public uint Hash { get; }
        internal MethodInfo Handler { get; }
        internal InteropParameterDescriptor[] Parameters { get; }
        public long FixedPrice { get; }
        public CallFlags RequiredCallFlags { get; }
        public bool AllowCallback { get; }

        internal InteropDescriptor(string name, MethodInfo handler, long fixedPrice, CallFlags requiredCallFlags, bool allowCallback)
        {
            this.Name = name;
            this.Hash = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(name).Sha256(), 0);
            this.Handler = handler;
            this.Parameters = handler.GetParameters().Select(p => new InteropParameterDescriptor(p)).ToArray();
            this.FixedPrice = fixedPrice;
            this.RequiredCallFlags = requiredCallFlags;
            this.AllowCallback = allowCallback;
        }

        public static implicit operator uint(InteropDescriptor descriptor)
        {
            return descriptor.Hash;
        }
    }
}
