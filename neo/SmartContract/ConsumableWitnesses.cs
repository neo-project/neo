using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.SmartContract
{
    public class ConsumableWitnesses : IVerifiable
    {
        private readonly IVerifiable _verificable;
        private readonly List<UInt160> _witnesses;

        Witness[] IVerifiable.Witnesses
        {
            get => _verificable.Witnesses;
            set => _verificable.Witnesses = value;
        }

        public int Size => _verificable.Size;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="witness"></param>
        public ConsumableWitnesses(IVerifiable verificable)
        {
            ((IVerifiable)this).Witnesses = verificable.Witnesses;
            _witnesses = new List<UInt160>(verificable.Witnesses.Select(u => u.ScriptHash));
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            _verificable.DeserializeUnsigned(reader);
        }

        public UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            return _witnesses.ToArray();
        }

        public bool ConsumeScriptHash(UInt160 hash)
        {
            return _witnesses.Remove(hash);
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            _verificable.SerializeUnsigned(writer);
        }

        public void Serialize(BinaryWriter writer)
        {
            _verificable.Serialize(writer);
        }

        public void Deserialize(BinaryReader reader)
        {
            _verificable.Deserialize(reader);
        }
    }
}