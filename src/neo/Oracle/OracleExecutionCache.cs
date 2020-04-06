using Neo.IO;
using Neo.SmartContract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Oracle
{
    public class OracleExecutionCache : IEnumerable<KeyValuePair<UInt160, OracleResponse>>, ISerializable
    {
        private OracleResponse[] _entries;

        /// <summary>
        /// Results (OracleRequest.Hash/OracleResponse)
        /// </summary>
        private readonly Dictionary<UInt160, OracleResponse> _cache = new Dictionary<UInt160, OracleResponse>();

        /// <summary>
        /// Engine
        /// </summary>
        private readonly Func<OracleRequest, OracleResponse> _oracle;

        /// <summary>
        /// Count
        /// </summary>
        public int Count => _entries.Length;

        /// <summary>
        /// Filter Cost
        /// </summary>
        public long FilterCost { get; private set; }

        /// <summary>
        /// Responses
        /// </summary>
        public OracleResponse[] Responses => _entries.ToArray();

        public int Size => IO.Helper.GetVarSize(Count) + _entries.Sum(u => u.Size);

        private UInt160 _hash;

        /// <summary>
        /// Hash
        /// </summary>
        public UInt160 Hash
        {
            get
            {
                if (_hash != null) return _hash;

                using (var stream = new MemoryStream())
                {
                    foreach (var entry in _cache)
                    {
                        // Request Hash
                        stream.Write(entry.Key.ToArray());

                        // Response Hash
                        stream.Write(entry.Value.Hash.ToArray());
                    }

                    _hash = stream.ToArray().ToScriptHash();
                }

                return _hash;
            }
        }

        /// <summary>
        /// Constructor for oracles
        /// </summary>
        /// <param name="oracle">Oracle Engine</param>
        public OracleExecutionCache(Func<OracleRequest, OracleResponse> oracle = null) : this()
        {
            _oracle = oracle;
        }

        /// <summary>
        /// Constructor for ISerializable
        /// </summary>
        public OracleExecutionCache()
        {
            _hash = null;
            FilterCost = 0;
        }

        /// <summary>
        /// Constructor for cached results
        /// </summary>
        /// <param name="results">Results</param>
        public OracleExecutionCache(params OracleResponse[] results)
        {
            FilterCost = 0;

            _hash = null;
            _oracle = null;
            _entries = results;

            foreach (var result in results)
            {
                _cache[result.RequestHash] = result;
                FilterCost += result.FilterCost;
            }
        }

        /// <summary>
        /// Get Oracle result
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="result">Result</param>
        /// <returns></returns>
        public bool TryGet(OracleRequest request, out OracleResponse result)
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

        public IEnumerator<KeyValuePair<UInt160, OracleResponse>> GetEnumerator()
        {
            return _cache.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _cache.GetEnumerator();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_entries.ToArray());
        }

        public void Deserialize(BinaryReader reader)
        {
            FilterCost = 0;
            _hash = null;

            _entries = reader.ReadSerializableArray<OracleResponse>(byte.MaxValue);
            _cache.Clear();

            foreach (var result in _entries)
            {
                _cache[result.RequestHash] = result;
                FilterCost += result.FilterCost;
            }
        }
    }
}
