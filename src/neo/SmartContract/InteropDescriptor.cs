using Neo.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Neo.SmartContract
{
    public class InteropDescriptor
    {
        public string Name { get; }
        public uint Hash { get; }
        public MethodInfo Handler { get; }
        public IReadOnlyList<InteropParameterDescriptor> Parameters { get; }
        public long FixedCPUPrice { get; }
        public long FixedStoragePrice { get; }
        public CallFlags RequiredCallFlags { get; }
        public bool AllowCallback { get; }

        internal InteropDescriptor(string name, MethodInfo handler, long fixedCPUPrice, long fixedStoragePrice, CallFlags requiredCallFlags, bool allowCallback)
        {
            this.Name = name;
            this.Hash = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(name).Sha256(), 0);
            this.Handler = handler;
            this.Parameters = handler.GetParameters().Select(p => new InteropParameterDescriptor(p)).ToList().AsReadOnly();
            this.FixedCPUPrice = fixedCPUPrice;
            this.FixedStoragePrice = fixedStoragePrice;
            this.RequiredCallFlags = requiredCallFlags;
            this.AllowCallback = allowCallback;
        }

        public static implicit operator uint(InteropDescriptor descriptor)
        {
            return descriptor.Hash;
        }
    }
}
