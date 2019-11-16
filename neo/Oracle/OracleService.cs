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
        /// <param name="tx">Transaction</param>
        /// <param name="testMode">Test mode</param>
        /// <returns>OracleResultsCache</returns>
        public OracleExecutionCache Process(Snapshot snapshot, Transaction tx, bool testMode = false)
        {
            var oracle = new OracleExecutionCache(request => ProcessInternal(tx.Hash, request));

            using (var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, tx.SystemFee, testMode, oracle))
            {
                engine.LoadScript(tx.Script);

                if (engine.Execute() == VM.VMState.HALT)
                {
                    return oracle;
                }
            }

            return new OracleExecutionCache();
        }

        /// <summary>
        /// Process internal
        /// </summary>
        /// <param name="txHash">Transaction hash</param>
        /// <param name="request">Request</param>
        /// <returns>OracleResult</returns>
        private OracleResult ProcessInternal(UInt256 txHash, OracleRequest request)
        {
            return request switch
            {
                OracleHTTPRequest http => HTTP.Process(txHash, http, TimeOut),
                _ => OracleResult.CreateError(txHash, request.Hash, OracleResultError.ServerError),
            };
        }
    }
}
