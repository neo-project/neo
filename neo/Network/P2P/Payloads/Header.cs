using Neo.IO.Json;
using Neo.Ledger;
using Neo.Wallets;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class Header : BlockBase, IEquatable<Header>
    {
        public override int Size => base.Size + 1;

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            if (reader.ReadByte() != 0) throw new FormatException();
        }

        public bool Equals(Header other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Header);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write((byte)0);
        }

        public TrimmedBlock Trim()
        {
            return new TrimmedBlock
            {
                Version = Version,
                PrevHash = PrevHash,
                MerkleRoot = MerkleRoot,
                Timestamp = Timestamp,
                Index = Index,
                NextConsensus = NextConsensus,
                Witness = Witness,
                Hashes = new UInt256[0]
            };
        }

        public new static Header FromJson(JObject json)
        {
            Header header = new Header();
            BlockBase blockBase = header;
            blockBase.FromJson(json);
            return header;
        }

    }
}
