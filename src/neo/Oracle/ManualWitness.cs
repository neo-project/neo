using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.IO;

namespace Neo.Oracle
{
    internal class ManualWitness : IVerifiable
    {
        public Witness[] Witnesses
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public int Size => throw new NotImplementedException();
        public void Deserialize(BinaryReader reader) => throw new NotImplementedException();
        public void DeserializeUnsigned(BinaryReader reader) => throw new NotImplementedException();
        public void Serialize(BinaryWriter writer) => throw new NotImplementedException();
        public void SerializeUnsigned(BinaryWriter writer) => throw new NotImplementedException();

        private readonly UInt160[] _hashes;

        public ManualWitness(params UInt160[] hashes)
        {
            _hashes = hashes ?? Array.Empty<UInt160>();
        }

        public UInt160[] GetScriptHashesForVerifying(StoreView snapshot)
        {
            return _hashes;
        }
    }
}
