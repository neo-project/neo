using Neo.IO;
using Neo.IO.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class TransactionAttributeCollection : ISerializable, IEnumerable<KeyValuePair<TransactionAttributeType, List<TransactionAttribute>>>
    {
        /// <summary>
        /// Maximum number of attributes that can be contained within a transaction
        /// </summary>
        public const int MaxTransactionAttributes = 16;

        private readonly Dictionary<TransactionAttributeType, List<TransactionAttribute>> _entries;

        public int Size =>
            IO.Helper.GetVarSize(_entries.Count) +      // count
            _entries.Values.Sum(u => u.GetVarSize());   // entries

        public int Count => _entries.Count;

        public Cosigner[] Cosigners
        {
            get
            {
                if (_entries.TryGetValue(TransactionAttributeType.Cosigner, out var cosigners))
                {
                    return cosigners.Cast<Cosigner>().ToArray();
                }

                return Array.Empty<Cosigner>();
            }
        }

        public TransactionAttributeCollection()
        {
            _entries = new Dictionary<TransactionAttributeType, List<TransactionAttribute>>();
        }

        public TransactionAttributeCollection(params TransactionAttribute[] attributes)
        {
            _entries = new Dictionary<TransactionAttributeType, List<TransactionAttribute>>();

            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    Add(attr);
                }
            }
        }

        public bool TryGet<T>(TransactionAttributeType type, out T[] attr)
        {
            if (_entries.TryGetValue(type, out var val))
            {
                attr = val.Cast<T>().ToArray();
                return true;
            }

            attr = null;
            return false;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(_entries.Sum(u => u.Value.Count));
            foreach (var attr in _entries)
            {
                foreach (var entry in attr.Value)
                {
                    writer.Write(entry);
                }
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            _entries.Clear();

            var count = (int)reader.ReadVarInt(MaxTransactionAttributes);
            for (int x = 0; x < count; x++)
            {
                Add(TransactionAttribute.DeserializeFrom(reader));
            }

            // Check duplicate cosigners

            var cosigners = Cosigners;
            if (cosigners.Select(u => u.Account).Distinct().Count() != cosigners.Length) throw new FormatException();
        }

        public void Add(TransactionAttribute attr)
        {
            if (_entries.TryGetValue(attr.Type, out var list))
            {
                list.Add(attr);
            }
            else
            {
                _entries[attr.Type] = new List<TransactionAttribute>(new TransactionAttribute[] { attr });
            }
        }

        public IEnumerator<KeyValuePair<TransactionAttributeType, List<TransactionAttribute>>> GetEnumerator()
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

            foreach (var entries in _entries.Values)
            {
                foreach (var attr in entries)
                {
                    ret.Add(attr.ToJson());
                }
            }

            return ret;
        }
    }
}
