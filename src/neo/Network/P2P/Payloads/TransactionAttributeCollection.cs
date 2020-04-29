using Neo.IO;
using Neo.IO.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class TransactionAttributeCollection : ISerializable, IEnumerable<KeyValuePair<TransactionAttributeUsage, TransactionAttribute>>
    {
        /// <summary>
        /// Maximum number of attributes that can be contained within a transaction
        /// </summary>
        public const int MaxTransactionAttributes = 16;

        private readonly Dictionary<TransactionAttributeUsage, TransactionAttribute> _entries;

        public int Size =>
            IO.Helper.GetVarSize(_entries.Count) +  // count
            _entries.Count +                        // usages
            _entries.Values.Sum(u => u.Size);       // entries

        public int Count => _entries.Count;

        public TransactionAttribute this[TransactionAttributeUsage usage]
        {
            get
            {
                if (_entries.TryGetValue(usage, out var attr))
                {
                    return attr;
                }

                return null;
            }
        }

        public TransactionAttributeCollection()
        {
            _entries = new Dictionary<TransactionAttributeUsage, TransactionAttribute>();
        }

        public TransactionAttributeCollection(params TransactionAttribute[] attributes)
        {
            _entries = new Dictionary<TransactionAttributeUsage, TransactionAttribute>();

            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    Add(attr.Usage, attr);
                }
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(_entries.Count);
            foreach (var attr in _entries)
            {
                writer.Write((byte)attr.Key);
                writer.Write(attr.Value);
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
                    case TransactionAttributeUsage.Cosigner:
                        {
                            _entries.Add(TransactionAttributeUsage.Cosigner, reader.ReadSerializable<CosignerAttribute>());
                            break;
                        }
                    default: throw new FormatException();
                }
            }
        }

        public void Add(TransactionAttributeUsage usage, TransactionAttribute attr)
        {
            _entries.Add(usage, attr);
        }

        public IEnumerator<KeyValuePair<TransactionAttributeUsage, TransactionAttribute>> GetEnumerator()
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
                ret["value"] = p.Value.ToJson();
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
