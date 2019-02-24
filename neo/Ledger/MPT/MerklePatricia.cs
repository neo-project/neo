using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Neo.Cryptography;

namespace Neo.Ledger.MPT
{
    /// <inheritdoc />
    /// <summary>
    /// Modified Merkel Patricia Tree.
    /// Note: It is not a thread safe implementation.
    /// </summary>
    public abstract class MerklePatricia : StateBase, IEquatable<MerklePatricia>
    {
        private readonly MPTSet mptSet;
        private readonly MPTGet mptGet;
        private readonly MPTRemove mptRemove;
        private readonly MPTValidate mptValidate;

        protected abstract MerklePatriciaNode GetDb(byte[] key);
        protected abstract bool RemoveDb(byte[] key);
        protected abstract MerklePatriciaNode SetDb(byte[] kye, MerklePatriciaNode node);
        protected abstract bool ContainsKeyDb(byte[] key);
        protected abstract byte[] GetRoot();
        protected abstract void SetRoot(byte[] root);

        internal MerklePatricia()
        {
            mptSet = new MPTSet(GetDb, RemoveDb, SetDb, GetRoot, SetRoot);
            mptGet = new MPTGet(GetDb, GetRoot);
            mptRemove = new MPTRemove(GetDb, RemoveDb, SetDb, GetRoot, SetRoot);
            mptValidate = new MPTValidate(GetDb, ContainsKeyDb);
        }

        /// <summary>
        /// Get and set the key and value pairs of the tree.
        /// </summary>
        /// <param name="key">The string key that indicates the reference.</param>
        public string this[string key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                var resp = this[Encoding.UTF8.GetBytes(key)];
                return resp == null ? null : Encoding.UTF8.GetString(resp);
            }
            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this[Encoding.UTF8.GetBytes(key)] = Encoding.UTF8.GetBytes(value);
            }
        }


        /// <summary>
        /// Get and set the key and value pairs of the tree.
        /// </summary>
        /// <param name="key">The key that indicates the reference.</param>
        public byte[] this[byte[] key]
        {
            get => mptGet.Get(key);
            set => mptSet.Set(key, value);
        }

        /// <summary>
        /// Test if contains a specific key.
        /// </summary>
        /// <param name="key">Key to be tested.</param>
        /// <returns>true in the case the tree contains the key.</returns>
        public bool ContainsKey(byte[] key) => this[key] != null;

        public bool ContainsKey(string key) => ContainsKey(Encoding.UTF8.GetBytes(key));

        /// <summary>
        /// Removes a value for a specific key.
        /// </summary>
        /// <param name="key">Remove this key from the tree.</param>
        /// <returns>true is the key was present and sucessifully removed.</returns>
        public bool Remove(string key) => Remove(Encoding.UTF8.GetBytes(key));

        /// <summary>
        /// Removes a value for a specific key.
        /// </summary>
        /// <param name="key">Remove this key from the tree.</param>
        /// <returns>true is the key was present and sucessifully removed.</returns>
        public bool Remove(byte[] key) => mptRemove.Remove(key);

        /// <summary>
        /// Checks if the hashes correspond to their nodes.
        /// </summary>
        /// <returns>In the case the validation is Ok.</returns>
        public bool Validate() => mptValidate.Validate(GetRoot());

        /// <inheritdoc />
        public override string ToString() => GetRoot() == null ? "{}" : ToString(GetDb(GetRoot()));

        private string ToString(MerklePatriciaNode node)
        {
            if (node.IsExtension)
            {
                return $"{{\"{node.Path.ByteToHexString(false, false)}\": {ToString(GetDb(node.Next))}}}";
            }

            if (node.IsLeaf)
            {
                return node.ToString();
            }

            var resp = new StringBuilder("{");
            var virgula = false;
            for (var i = 0; i < node.Length; i++)
            {
                if (node[i] == null) continue;
                resp.Append(virgula ? "," : "")
                    .Append(i < node.Length - 2
                        ? $"\"{i:x}\":{ToString(GetDb(node[i]))}"
                        : $"\"{i:x}\":\"{node[i].ByteToHexString(false, false)}\"");

                virgula = true;
            }

            return resp.Append("}").ToString();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((MerklePatricia) obj);
        }

        /// <inheritdoc />
        public bool Equals(MerklePatricia other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (GetRoot() == null && other.GetRoot() == null) return true;
            if ((GetRoot() == null && other.GetRoot() != null) ||
                (GetRoot() != null && other.GetRoot() == null))
            {
                return false;
            }

            if (!GetRoot().SequenceEqual(other.GetRoot()))
            {
                return false;
            }

            var values = new Stack<byte[]>();
            values.Push(GetRoot());
            while (values.Count > 0)
            {
                var it = values.Pop();
                if (it == null) continue;
                var itNode = GetDb(it);
                var otherNode = other.GetDb(it);
                if (otherNode == null || !otherNode.Equals(itNode))
                {
                    return false;
                }

                foreach (var hash in itNode)
                {
                    if (hash != null)
                    {
                        values.Push(hash);
                    }
                }
            }
            
            return true;
        }
        
        /// <inheritdoc />
        public override int GetHashCode() => GetRoot() != null ?  (int) GetRoot().Murmur32(0) : 0;
        
    }
}