using Neo.Cryptography;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Neo.SmartContract
{
    public record InteropDescriptor
    {
        public string Name { get; init; }

        private uint _hash;
        public uint Hash
        {
            get
            {
                if (_hash == 0)
                    _hash = BinaryPrimitives.ReadUInt32LittleEndian(Encoding.ASCII.GetBytes(Name).Sha256());
                return _hash;
            }
        }

        public MethodInfo Handler { get; init; }

        private IReadOnlyList<InteropParameterDescriptor> _parameters;
        public IReadOnlyList<InteropParameterDescriptor> Parameters => _parameters ??= Handler.GetParameters().Select(p => new InteropParameterDescriptor(p)).ToList().AsReadOnly();

        public long FixedPrice { get; init; }

        public CallFlags RequiredCallFlags { get; init; }

        public static implicit operator uint(InteropDescriptor descriptor)
        {
            return descriptor.Hash;
        }
    }
}
