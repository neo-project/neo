using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Oracle
{
    public class OracleExpectedResult : TransactionAttribute, IEnumerable<KeyValuePair<UInt160, UInt160>>
    {
        private readonly IDictionary<UInt160, UInt160> _expectedResults;

        public override int Size => base.Size +
            IO.Helper.GetVarSize(_expectedResults.Count) +
            (_expectedResults.Count * UInt160.Length * 2);

        /// <summary>
        /// Count
        /// </summary>
        public int Count => _expectedResults.Count;

        /// <summary>
        /// Constructor
        /// </summary>
        public OracleExpectedResult() : base(TransactionAttributeUsage.OracleRequest)
        {
            _expectedResults = new Dictionary<UInt160, UInt160>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cache">Cache</param>
        /// <param name="attachExpectedHashes">Attach expected hashes</param>
        public OracleExpectedResult(OracleExecutionCache cache, bool attachExpectedHashes = true) : this()
        {
            foreach (var entry in cache)
            {
                _expectedResults.Add(entry.Key, attachExpectedHashes ? entry.Value.Hash : UInt160.Zero);
            }
        }

        /// <summary>
        /// Contains request
        /// </summary>
        /// <param name="oracleRequestHash">Oracle Request Hash</param>
        /// <returns>True or False</returns>
        public bool ContainsRequest(UInt160 oracleRequestHash)
        {
            return _expectedResults.ContainsKey(oracleRequestHash);
        }

        /// <summary>
        /// Match
        /// </summary>
        /// <param name="cache">Oracle cache</param>
        /// <returns>Return TRUE if match</returns>
        public bool Match(OracleExecutionCache cache)
        {
            if (cache.Count != Count) return false;

            foreach (var entry in cache)
            {
                // Not found

                if (!_expectedResults.TryGetValue(entry.Key, out var value)) return false;

                // Different

                if (value != UInt160.Zero && value != entry.Value.Hash) return false;
            }

            return true;
        }

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            var count = (int)reader.ReadVarInt(byte.MaxValue);

            for (int x = 0; x < count; x++)
            {
                _expectedResults[reader.ReadSerializable<UInt160>()] = reader.ReadSerializable<UInt160>();
            }
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.WriteVarInt(_expectedResults.Count);

            foreach (var entry in _expectedResults)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value);
            }
        }

        public override JObject ToJson()
        {
            var json = base.ToJson();

            json["expectedHashes"] = new JArray(_expectedResults.Select(t =>
            {
                var ret = new JObject();
                ret["requestHash"] = t.Key.ToString();
                ret["expectedHash"] = t.Value.ToString();
                return ret;
            }));

            return json;
        }

        public new static OracleExpectedResult FromJson(JObject json)
        {
            if (!Enum.TryParse<TransactionAttributeUsage>(json["usage"].AsString(), out var usage)
                || usage != TransactionAttributeUsage.OracleRequest)
            {
                throw new FormatException();
            }

            var attr = new OracleExpectedResult();

            foreach (var entry in (JArray)json["expectedHashes"])
            {
                var key = UInt160.Parse(entry["requestHash"].AsString());
                var value = UInt160.Parse(entry["expectedHash"].AsString());

                attr._expectedResults.Add(key, value);
            }

            return attr;
        }

        public IEnumerator<KeyValuePair<UInt160, UInt160>> GetEnumerator()
        {
            return _expectedResults.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _expectedResults.GetEnumerator();
        }
    }
}
