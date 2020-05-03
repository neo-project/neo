using Neo.IO;
using Neo.IO.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class TransactionAttributeCollection : ISerializable, IEnumerable<TransactionAttribute>
    {
        /// <summary>
        /// Maximum number of attributes that can be contained within a transaction
        /// </summary>
        public const int MaxTransactionAttributes = 16;

        private Cosigner[] _cosigners;
        private readonly List<TransactionAttribute> _entries;

        public int Size =>
            IO.Helper.GetVarSize(_entries.Count) +  // count
            _entries.Sum(u => u.Size);              // entries

        public int Count => _entries.Count;

        public Cosigner[] Cosigners
        {
            get
            {
                if (_cosigners != null)
                {
                    return _cosigners;
                }

                _cosigners = _entries.OfType<Cosigner>().ToArray();
                return _cosigners;
            }
        }

        public TransactionAttributeCollection()
        {
            _entries = new List<TransactionAttribute>();
        }

        public TransactionAttributeCollection(params TransactionAttribute[] attributes)
        {
            _entries = new List<TransactionAttribute>();

            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    Add(attr);
                }
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(_entries.Count);
            foreach (var entry in _entries)
            {
                writer.Write(entry);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            _entries.Clear();
            _cosigners = null;

            var count = (int)reader.ReadVarInt(MaxTransactionAttributes);
            for (int x = 0; x < count; x++)
            {
                Add(TransactionAttribute.DeserializeFrom(reader));
            }

            // Check duplicate cosigners

            if (Cosigners.Select(u => u.Account).Distinct().Count() != Cosigners.Length) throw new FormatException();
        }

        public void Add(TransactionAttribute attr)
        {
            _cosigners = null;
            _entries.Add(attr);
        }

        public IEnumerator<TransactionAttribute> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        public JObject ToJson()
        {
            var ret = new JArray();

            foreach (var entry in _entries)
            {
                ret.Add(entry.ToJson());
            }

            return ret;
        }
    }
}
