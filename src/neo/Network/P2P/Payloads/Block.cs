using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public sealed class Block : IEquatable<Block>, IInventory
    {
        public Header Header;
        public Transaction[] Transactions;

        public UInt256 Hash => Header.Hash;
        public uint Version => Header.Version;
        public UInt256 PrevHash => Header.PrevHash;
        public UInt256 MerkleRoot => Header.MerkleRoot;
        public ulong Timestamp => Header.Timestamp;
        public uint Index => Header.Index;
        public byte PrimaryIndex => Header.PrimaryIndex;
        public UInt160 NextConsensus => Header.NextConsensus;
        public Witness Witness => Header.Witness;

        InventoryType IInventory.InventoryType => InventoryType.Block;
        public int Size => Header.Size + Transactions.GetVarSize();
        Witness[] IVerifiable.Witnesses { get => ((IVerifiable)Header).Witnesses; set => throw new NotSupportedException(); }

        public void Deserialize(BinaryReader reader)
        {
            Header = reader.ReadSerializable<Header>();
            Transactions = reader.ReadSerializableArray<Transaction>((int)ProtocolSettings.Default.MaxTransactionsPerBlock);
            if (Transactions.Distinct().Count() != Transactions.Length)
                throw new FormatException();
            if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != Header.MerkleRoot)
                throw new FormatException();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader) => throw new NotSupportedException();

        public bool Equals(Block other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Block);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(DataCache snapshot) => ((IVerifiable)Header).GetScriptHashesForVerifying(snapshot);

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Header);
            writer.Write(Transactions);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer) => ((IVerifiable)Header).SerializeUnsigned(writer);

        public JObject ToJson()
        {
            JObject json = Header.ToJson();
            json["size"] = Size;
            json["tx"] = Transactions.Select(p => p.ToJson()).ToArray();
            return json;
        }

        internal bool Verify(DataCache snapshot)
        {
            return Header.Verify(snapshot);
        }

        internal bool Verify(DataCache snapshot, HeaderCache headerCache)
        {
            return Header.Verify(snapshot, headerCache);
        }
    }
}
