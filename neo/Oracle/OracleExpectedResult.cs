using Neo.IO;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Neo.Oracle
{
    public class OracleExpectedResult : ISerializable, IEnumerable<KeyValuePair<UInt160, UInt160>>
    {
        private readonly IDictionary<UInt160, UInt160> _expectedResults;

        public int Size =>
            _expectedResults.Count.GetVarSize() +
            (_expectedResults.Count * UInt160.Length * 2);

        /// <summary>
        /// Count
        /// </summary>
        public int Count => _expectedResults.Count;

        /// <summary>
        /// Constructor
        /// </summary>
        public OracleExpectedResult()
        {
            _expectedResults = new Dictionary<UInt160, UInt160>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cache">Cache</param>
        /// <param name="attachExpectedHashes">Attach expected hashes</param>
        public OracleExpectedResult(OracleResultsCache cache, bool attachExpectedHashes = true)
        {
            foreach (var entry in cache)
            {
                _expectedResults.Add(entry.Key, attachExpectedHashes ? entry.Value.Hash : UInt160.Zero);
            }
        }

        /// <summary>
        /// Match
        /// </summary>
        /// <param name="cache">Oracle cache</param>
        /// <returns>Return TRUE if match</returns>
        public bool Match(OracleResultsCache cache)
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

        public void Deserialize(BinaryReader reader)
        {
            var count = (int)reader.ReadVarInt(byte.MaxValue);

            for (int x = 0; x < count; x++)
            {
                _expectedResults[reader.ReadSerializable<UInt160>()] = reader.ReadSerializable<UInt160>();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(_expectedResults.Count);

            foreach (var entry in _expectedResults)
            {
                writer.Write(entry.Key);
                writer.Write(entry.Value);
            }
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
