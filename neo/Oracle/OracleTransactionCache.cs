using System;
using System.Collections.Generic;

namespace Neo.Oracle
{
    public class OracleTransactionCache
    {
        /// <summary>
        /// Results
        /// </summary>
        public readonly Dictionary<UInt160, OracleResult> Cache = new Dictionary<UInt160, OracleResult>();

        /// <summary>
        /// Engine
        /// </summary>
        private readonly Func<OracleRequest, OracleResult> _oracleEngine;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="engine">Engine</param>
        public OracleTransactionCache(Func<OracleRequest, OracleResult> engine = null)
        {
            _oracleEngine = engine;
        }

        /// <summary>
        /// Get Oracle result
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="result">Result</param>
        /// <returns></returns>
        public bool TryGet(OracleRequest request, out OracleResult result)
        {
            if (Cache.TryGetValue(request.Hash, out result))
            {
                return true;
            }

            // Not found inside the cache, invoke it

            result = _oracleEngine?.Invoke(request);

            if (result != null)
            {
                Cache[request.Hash] = result;
                return true;
            }

            // No oracle logic attached

            return false;
        }
    }
}
