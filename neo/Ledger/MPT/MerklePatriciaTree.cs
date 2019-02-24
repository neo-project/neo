using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Neo.IO;

namespace Neo.Ledger.MPT
{
    /// <inheritdoc />
    /// </summary>
    public class MerklePatriciaTree : MerklePatricia,
        ICloneable<MerklePatriciaTree>
    {
        private readonly MerklePatricia _merklePatricia;
        private byte[] _rootHash;

        private readonly Dictionary<byte[], MerklePatriciaNode> db = new
            Dictionary<byte[], MerklePatriciaNode>(new ByteArrayComparer());

        /// <inheritdoc />
        protected override MerklePatriciaNode GetDb(byte[] hash) => db[hash];

        /// <inheritdoc />
        protected override bool RemoveDb(byte[] hash) => db.Remove(hash);

        /// <inheritdoc />
        protected override MerklePatriciaNode SetDb(byte[] hash, MerklePatriciaNode node) => db[hash] = node;

        /// <inheritdoc />
        protected override bool ContainsKeyDb(byte[] key) => db.ContainsKey(key);

        /// <inheritdoc />
        protected override byte[] GetRoot() => _rootHash;

        /// <inheritdoc />
        protected override void SetRoot(byte[] root) => _rootHash = root;

        private bool ContainsValueDb(byte[] value) =>
            db.Any(x => x.Value.Value != null && x.Value.Value.SequenceEqual(value));

        private void ClearDb() => db.Clear();
        private int CountAllDb() => db.Count();
        private int CountValuesDb() => db.Count(x => x.Value.IsLeaf || (x.Value.IsBranch && x.Value.Value != null));
        private IEnumerable<KeyValuePair<byte[], MerklePatriciaNode>> GetEnumeratorDb() => db;
        private MerklePatriciaTree NewTree() => new MerklePatriciaTree();

        /// <inheritdoc />
        public MerklePatriciaTree Clone()
        {
            var resp = NewTree();
            resp.SetRoot(GetRoot()?.ToArray());
            foreach (var entry in GetEnumeratorDb())
            {
                resp.SetDb(entry.Key.ToArray(), entry.Value.Clone());
            }

            return resp;
        }

        /// <inheritdoc />
        public void FromReplica(MerklePatriciaTree replica)
        {
            ClearDb();
            SetRoot(replica.GetRoot()?.ToArray());
            foreach (var entry in replica.GetEnumeratorDb())
            {
                SetDb(entry.Key.ToArray(), entry.Value.Clone());
            }
        }

        /// <inheritdoc />
        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ClearDb();
            SetRoot(reader.ReadVarBytes());
            if (GetRoot().Length == 0)
            {
                SetRoot(null);
            }

            var size = reader.ReadVarInt();
            for (var i = 0ul; i < size; i++)
            {
                var key = reader.ReadVarBytes();
                var value = MerklePatriciaNode.ExtensionNode();
                value.Deserialize(reader);
                SetDb(key, value);
            }
        }

        /// <inheritdoc />
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(GetRoot() ?? new byte[0]);
            writer.WriteVarInt(CountAllDb());
            foreach (var it in GetEnumeratorDb())
            {
                writer.WriteVarBytes(it.Key);
                writer.Write(it.Value);
            }
        }

        /// <summary>
        /// Count the number o values in the MPT not all the nodes, just the ones whith associated values.
        /// </summary>
        /// <returns>The number of values.</returns>
        public int Count() => CountValuesDb();

        /// <summary>
        /// Verify if the MPT contains the specific value.
        /// </summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns>true if the value is present.</returns>
        public bool ContainsValue(string value) => ContainsValue(Encoding.UTF8.GetBytes(value));

        /// <summary>
        /// Test if the tree contains a specific value.
        /// Takes O(n) operations.
        /// </summary>
        /// <param name="value">Value to look for.</param>
        /// <returns>true if the value is present.</returns>
        public bool ContainsValue(byte[] value) => ContainsValueDb(value);
    }
}