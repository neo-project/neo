using Neo.IO;
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
                    Add(attr.Usage, attr);
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
                switch ((TransactionAttributeUsage)reader.ReadByte())
                {
                    case TransactionAttributeUsage.Cosigners:
                        {
                            Add(TransactionAttributeUsage.Cosigners, reader.ReadSerializable<CosignerAttribute>());
                            break;
                        }
                    default: throw new FormatException();
                }
            }
        }

        public void Add(TransactionAttributeUsage usage, TransactionAttribute attr)
        {
            if (_entries.TryGetValue(usage, out var list))
            {
                list.Add(attr);
            }
            else
            {
                _entries[usage] = new List<TransactionAttribute>(new TransactionAttribute[] { attr });
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
            return _entries.Select(p =>
            {
                var ret = new JObject();
                ret["type"] = p.Key.ToString();
                ret["value"] = p.Value.Select(u => u.ToJson()).ToArray();
                return ret;
            })
            .ToArray();
        }

        public static TransactionAttributeCollection FromJson(JObject json)
        {
            var ret = new TransactionAttributeCollection();
            foreach (var entry in (JArray)json["attributes"])
            {
                var attr = TransactionAttribute.FromJson(entry);
                ret.Add(attr.Usage, attr);
            }
            return ret;
        }
    }
}
