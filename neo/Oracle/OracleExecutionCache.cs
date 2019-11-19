using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.Oracle
{
    public class OracleExecutionCache : IEnumerable<KeyValuePair<UInt160, OracleResult>>
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
        /// Constructor for oracles
        /// </summary>
        /// <param name="oracle">Oracle Engine</param>
        public OracleExecutionCache(Func<OracleRequest, OracleResult> oracle = null)
        {
            _oracle = oracle;
        }

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
    }
}
