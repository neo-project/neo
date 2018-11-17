using Neo.IO;
using System.IO;

namespace Neo.Ledger
{
    // Modified Merkle Patricia
    public class MPTItem : StateBase, ICloneable<MPTItem>
    {
        public byte NodeType;  // 00: branch  01: leaf
        public string path;
        public string value;
        public UInt256[] hashes;
        public UInt256 KeyHash;

        public override int Size => base.Size + 1 + path.Length + value.Length + hashes.Length + (KeyHash==null?0:KeyHash.Size);

        MPTItem ICloneable<MPTItem>.Clone()
        {
            return new MPTItem
            {
                NodeType = NodeType,
                path = path,
                value = value,
                hashes = hashes,
                KeyHash = KeyHash
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            NodeType = reader.ReadByte();
            if(NodeType == 0x00) // 00: branch
            {
                hashes = new UInt256[16];
                for(var i=0; i<16; i++)
                {
                    byte[] bytes = reader.ReadVarBytes();
                    if(bytes.Length == 0)
                        hashes[i] = null;
                    else
                        hashes[i] = new UInt256(bytes);
                }
                KeyHash = null;
                path = "";
            }
            else // 01: leaf
            {
                hashes = new UInt256[0];
                path = reader.ReadVarString();
                // hash of the original key (to avoid ambiguities
                KeyHash = reader.ReadSerializable<UInt256>();
            }
            value = reader.ReadVarString();
        }

        void ICloneable<MPTItem>.FromReplica(MPTItem replica)
        {
            NodeType = replica.NodeType;
            path     = replica.path;
            value    = replica.value;
            hashes   = replica.hashes;
            KeyHash  = replica.KeyHash;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(NodeType);
            if(NodeType == 0x00) // 00: branch
            {
                for(var i=0; i<16; i++)
                {
                    if(hashes[i] == null)
                        writer.Write(0x00);
                    else
                        writer.Write(hashes[i]);
                }
            }
            else // 01: leaf
            {
                writer.WriteVarString(path);
                // hash of the original key (to avoid ambiguities
                writer.Write(KeyHash);
            }
            writer.WriteVarString(value);
        }
    }
}
