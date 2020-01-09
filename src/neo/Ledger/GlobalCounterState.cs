using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Ledger
{
    public class GlobalCounterState : ICloneable<GlobalCounterState>, ISerializable
    {
        public uint Value = 0x20000000;

        int ISerializable.Size => sizeof(uint);

        GlobalCounterState ICloneable<GlobalCounterState>.Clone()
        {
            return new GlobalCounterState
            {
                Value = Value
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Value = reader.ReadUInt32();
        }

        void ICloneable<GlobalCounterState>.FromReplica(GlobalCounterState replica)
        {
            Value = replica.Value;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        internal void Set(uint value)
        {
            Value = value;
        }
    }
}
