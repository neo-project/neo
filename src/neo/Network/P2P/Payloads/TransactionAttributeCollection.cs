using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class TransactionAttributeCollection : ISerializable, IEnumerable<KeyValuePair<TransactionAttributeUsage, List<TransactionAttribute>>>
    {
        /// <summary>
        /// Maximum number of attributes that can be contained within a transaction
        /// </summary>
        public const int MaxTransactionAttributes = 16;

        private readonly Dictionary<TransactionAttributeUsage, List<TransactionAttribute>> _entries;

        public int Size =>
            IO.Helper.GetVarSize(_entries.Count) +      // count
            _entries.Count +                            // usages
            _entries.Values.Sum(u => u.GetVarSize());   // entries

        public int Count => _entries.Count;

        public Cosigner[] Cosigners
        {
            get
            {
                if (_entries.TryGetValue(TransactionAttributeUsage.Cosigner, out var cosigners))
                {
                    return cosigners.Cast<Cosigner>().ToArray();
                }

                return Array.Empty<Cosigner>();
            }
        }

        public TransactionAttributeCollection()
        {
            _entries = new Dictionary<TransactionAttributeUsage, List<TransactionAttribute>>();
        }

        public TransactionAttributeCollection(params TransactionAttribute[] attributes)
        {
            _entries = new Dictionary<TransactionAttributeUsage, List<TransactionAttribute>>();

            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    Add(attr);
                }
            }
        }

        public bool TryGet<T>(TransactionAttributeUsage usage, out T[] attr)
        {
            if (_entries.TryGetValue(usage, out var val))
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
                    writer.Write((byte)entry.Usage);
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
                var usage = (TransactionAttributeUsage)reader.ReadByte();
                var obj = ReflectionCache<TransactionAttributeUsage>.CreateSerializable(usage, reader);
                if (!(obj is TransactionAttribute attr)) throw new FormatException();
                Add(attr);
            }

            // Check duplicate cosigners

            var cosigners = Cosigners;
            if (cosigners.Select(u => u.Account).Distinct().Count() != cosigners.Length) throw new FormatException();
        }

        public void Add(TransactionAttribute attr)
        {
            if (_entries.TryGetValue(attr.Usage, out var list))
            {
                list.Add(attr);
            }
            else
            {
                _entries[attr.Usage] = new List<TransactionAttribute>(new TransactionAttribute[] { attr });
            }
        }

        public IEnumerator<KeyValuePair<TransactionAttributeUsage, List<TransactionAttribute>>> GetEnumerator()
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

        public static TransactionAttributeCollection FromJson(JObject json)
        {
            var ret = new TransactionAttributeCollection();
            foreach (var entry in (JArray)json)
            {
                ret.Add(TransactionAttribute.FromJson(entry));
            }
            return ret;
        }
    }
}
