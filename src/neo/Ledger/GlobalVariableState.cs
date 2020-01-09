using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Ledger
{
    public class GlobalVariableState : ICloneable<GlobalVariableState>, ISerializable
    {
        public uint GlobalCounter = 0x00000000;

        int ISerializable.Size => sizeof(uint);

        GlobalVariableState ICloneable<GlobalVariableState>.Clone()
        {
            return new GlobalVariableState
            {
                GlobalCounter = GlobalCounter
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            GlobalCounter = reader.ReadUInt32();
        }

        void ICloneable<GlobalVariableState>.FromReplica(GlobalVariableState replica)
        {
            GlobalCounter = replica.GlobalCounter;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(GlobalCounter);
        }

        internal void Set(uint value)
        {
            GlobalCounter = value;
        }
    }
}
