using Neo.IO;
using System.IO;

namespace Neo.Ledger.MPT
{
    // Modified Merkle Patricia
    public class MPTItem : StateBase, ICloneable<MPTItem>
    {
        public enum MPTNodeType : byte
        {
            Branch,
            Leaf
        }

        public MPTNodeType NodeType;
        public string Path;
        public string Value;
        public UInt256[] Hashes;
        public UInt256 KeyHash;

        public override int Size => base.Size + 1 + Path.Length + Value.Length + Hashes.Length +
                                    (KeyHash == null ? 0 : KeyHash.Size);

        MPTItem ICloneable<MPTItem>.Clone()
        {
            return new MPTItem
            {
                NodeType = NodeType,
                Path = Path,
                Value = Value,
                Hashes = Hashes,
                KeyHash = KeyHash
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            NodeType = (MPTNodeType) reader.ReadByte();
            if (NodeType == 0x00) // 00: branch
            {
                Hashes = new UInt256[16];
                for (var i = 0; i < 16; i++)
                {
                    byte[] bytes = reader.ReadVarBytes();
                    if (bytes.Length == 0)
                        Hashes[i] = null;
                    else
                        Hashes[i] = new UInt256(bytes);
                }

                KeyHash = null;
                Path = "";
            }
            else // 01: leaf
            {
                Hashes = new UInt256[0];
                Path = reader.ReadVarString();
                // hash of the original key (to avoid ambiguities
                KeyHash = reader.ReadSerializable<UInt256>();
            }

            Value = reader.ReadVarString();
        }

        void ICloneable<MPTItem>.FromReplica(MPTItem replica)
        {
            NodeType = replica.NodeType;
            Path = replica.Path;
            Value = replica.Value;
            Hashes = replica.Hashes;
            KeyHash = replica.KeyHash;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write((byte) NodeType);
            if (NodeType == 0x00) // 00: branch
            {
                for (var i = 0; i < 16; i++)
                {
                    if (Hashes[i] == null)
                        writer.Write(0x00);
                    else
                        writer.Write(Hashes[i]);
                }
            }
            else // 01: leaf
            {
                writer.WriteVarString(Path);
                // hash of the original key (to avoid ambiguities
                writer.Write(KeyHash);
            }

            writer.WriteVarString(Value);
        }
    }
}