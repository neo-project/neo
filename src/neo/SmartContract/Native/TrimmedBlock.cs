using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using System.Linq;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents a block which the transactions are trimmed.
    /// </summary>
    public class TrimmedBlock : IInteroperable, ISerializable
    {
        /// <summary>
        /// The header of the block.
        /// </summary>
        public Header Header;

        /// <summary>
        /// The hashes of the transactions of the block.
        /// </summary>
        public UInt256[] Hashes;

        /// <summary>
        /// The hash of the block.
        /// </summary>
        public UInt256 Hash => Header.Hash;

        /// <summary>
        /// The index of the block.
        /// </summary>
        public uint Index => Header.Index;

        public int Size => Header.Size + Hashes.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            Header = reader.ReadSerializable<Header>();
            Hashes = reader.ReadSerializableArray<UInt256>(ushort.MaxValue);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Header);
            writer.Write(Hashes);
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            throw new NotSupportedException();
        }

        StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
        {
            return new VM.Types.Array(referenceCounter, new StackItem[]
            {
                // Computed properties
                Header.Hash.ToArray(),

                // BlockBase properties
                Header.Version,
                Header.PrevHash.ToArray(),
                Header.MerkleRoot.ToArray(),
                Header.Timestamp,
                Header.Nonce,
                Header.Index,
                Header.PrimaryIndex,
                Header.NextConsensus.ToArray(),

                // Block properties
                Hashes.Length
            });
        }
    }
}
