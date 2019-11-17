using Neo.Network.P2P.Payloads;
using Neo.Oracle.Protocols;
using Neo.Oracle.Protocols.HTTP;
using Neo.Persistence;
using Neo.SmartContract;
using System;

namespace Neo.Oracle
{
    public class OracleService
    {
        #region Protocols

        /// <summary>
        /// HTTP Protocol
        /// </summary>
        private static readonly IOracleProtocol HTTP = new OracleHTTPProtocol();

        #endregion

        /// <summary>
        /// Timeout
        /// </summary>
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Process transaction
        /// </summary>
        /// <param name="snapshot">Snapshot</param>
        /// <param name="persistingBlock">Persisting block</param>
        /// <param name="tx">Transaction</param>
        /// <param name="testMode">Test mode</param>
        /// <returns>OracleResultsCache</returns>
        public OracleExecutionCache Process(Snapshot snapshot, Block persistingBlock, Transaction tx, bool testMode = false)
        {
            var oracle = CreateExecutionCache(tx.Hash);

            using (var engine = ApplicationEngine.Run(tx.Script, snapshot, tx, persistingBlock, testMode, tx.SystemFee, oracle))
            {
                if (engine.State != VM.VMState.HALT)
                {
                    return new OracleExecutionCache();
                }
            }

            return oracle;
        }

        /// <summary>
        /// Create Execution Cache
        /// </summary>
        /// <param name="txHash">Tx hash</param>
        /// <returns>OracleExecutionCache</returns>
        public OracleExecutionCache CreateExecutionCache(UInt256 txHash)
        {
            return new OracleExecutionCache((req) => ExecuteRequest(txHash, req));
        }

        /// <summary>
        /// Execute oracle request
        /// </summary>
        /// <param name="txHash">Transaction hash</param>
        /// <param name="request">Request</param>
        /// <returns>OracleResult</returns>
        private OracleResult ExecuteRequest(UInt256 txHash, OracleRequest request)
        {
            return request switch
            {
                OracleHTTPRequest http => HTTP.Process(txHash, http, TimeOut),
                _ => OracleResult.CreateError(txHash, request.Hash, OracleResultError.ServerError),
            };
        }
    }
}
