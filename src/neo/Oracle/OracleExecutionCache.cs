using Neo.IO;
using Neo.IO.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Oracle
{
    public class OracleExecutionCache : IEnumerable<KeyValuePair<UInt160, OracleResult>>, ISerializable
    {
        /// <summary>
        /// Results
        /// </summary>
        private readonly Dictionary<UInt160, OracleResult> _cache = new Dictionary<UInt160, OracleResult>();

        /// <summary>
        /// Engine
        /// </summary>
        private readonly Func<OracleRequest, OracleResult> _oracle;

        /// <summary>
        /// Count
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Size
        /// </summary>
        public int Size => IO.Helper.GetVarSize(_cache.Count) + _cache.Values.Sum(u => u.Size);

        /// <summary>
        /// Constructor for oracles
        /// </summary>
        /// <param name="oracle">Oracle Engine</param>
        public OracleExecutionCache(Func<OracleRequest, OracleResult> oracle = null)
        {
            _oracle = oracle;
        }

        /// <summary>
        /// Constructor required for ReadSerializable
        /// </summary>
        public OracleExecutionCache() { }

        /// <summary>
        /// Constructor for cached results
        /// </summary>
        /// <param name="results">Results</param>
        public OracleExecutionCache(params OracleResult[] results)
        {
            _oracle = null;

            foreach (var result in results)
            {
                _cache[result.RequestHash] = result;
            }
        }

        /// <summary>
        /// Get Oracle result
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="result">Result</param>
        /// <returns></returns>
        public bool TryGet(OracleRequest request, out OracleResult result)
        {
            if (_cache.TryGetValue(request.Hash, out result))
            {
                return true;
            }

            // Not found inside the cache, invoke it

            result = _oracle?.Invoke(request);

            if (result != null)
            {
                _cache[request.Hash] = result;
                return true;
            }

            // Without oracle logic

            return false;
        }

        public IEnumerator<KeyValuePair<UInt160, OracleResult>> GetEnumerator()
        {
            return _cache.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _cache.GetEnumerator();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(_cache.Count);

            foreach (var result in _cache.Values)
            {
                writer.Write(result);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            var count = (int)reader.ReadVarInt(ushort.MaxValue);

            _cache.Clear();
            for (int x = 0; x < count; x++)
            {
                var result = reader.ReadSerializable<OracleResult>();
                _cache.Add(result.Hash, result);
            }
        }

        public JObject ToJson()
        {
            JArray json = new JArray();

            foreach (var result in _cache.Values)
            {
                json.Add(result.ToJson());
            }

            return json;
        }

        public static OracleExecutionCache FromJson(JObject json)
        {
            List<OracleResult> entries = new List<OracleResult>();

            if (json is JArray arr)
            {
                foreach (var entry in arr)
                {
                    entries.Add(OracleResult.FromJson(entry));
                }
            }

            return new OracleExecutionCache(entries.ToArray());
        }
    }
}
