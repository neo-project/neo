using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.Oracle
{
    public class OracleExecutionCache : IEnumerable<KeyValuePair<UInt160, OracleResponse>>
    {
        /// <summary>
        /// Results
        /// </summary>
        private readonly Dictionary<UInt160, OracleResponse> _cache = new Dictionary<UInt160, OracleResponse>();

        /// <summary>
        /// Engine
        /// </summary>
        private readonly Func<OracleRequest, OracleResponse> _oracle;

        /// <summary>
        /// Count
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Constructor for oracles
        /// </summary>
        /// <param name="oracle">Oracle Engine</param>
        public OracleExecutionCache(Func<OracleRequest, OracleResponse> oracle = null)
        {
            _oracle = oracle;
        }

        /// <summary>
        /// Constructor for cached results
        /// </summary>
        /// <param name="results">Results</param>
        public OracleExecutionCache(params OracleResponse[] results)
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
    }
}
