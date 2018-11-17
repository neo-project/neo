using Neo.IO;
using System.IO;

namespace Neo.Ledger
{
    // Modified Merkle Patricia
    public class MPTItem : StateBase, ICloneable<MPTItem>
    {
        public string[] MPTNode;

        public override int Size => base.Size + MPTNode.GetVarSize();

        MPTItem ICloneable<MPTItem>.Clone()
        {
            return new MPTItem
            {
                MPTNode = MPTNode
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            MPTNode = reader.ReadVarBytes();
        }

        void ICloneable<MPTItem>.FromReplica(MPTItem replica)
        {
            MPTNode = replica.MPTNode;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(MPTNode);
        }
    }
}
