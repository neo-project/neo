using Neo.Network.P2P.Payloads;
using Neo.Oracle.Protocols;
using Neo.Oracle.Protocols.HTTP1;
using Neo.Oracle.Protocols.HTTP2;
using Neo.Persistence;
using Neo.SmartContract;
using System;

namespace Neo.Oracle
{
    public class OracleService
    {
        #region Protocols

        /// <summary>
        /// HTTP1 Protocol
        /// </summary>
        private static readonly IOracleProtocol HTTP1 = new OracleHTTP1Protocol();

        /// <summary>
        /// HTTP2 Protocol
        /// </summary>
        private static readonly IOracleProtocol HTTP2 = new OracleHTTP2Protocol();

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
        public OracleResultsCache Process(Snapshot snapshot, Transaction tx, bool testMode = false)
        {
            var oracle = new OracleResultsCache(request => ProcessInternal(tx.Hash, request));

            using (var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, tx.SystemFee, testMode, oracle))
            {
                if (engine.Execute() == VM.VMState.HALT)
                {
                    return oracle;
                }
            }

            return new OracleResultsCache();
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
                OracleHTTP1Request http1 => HTTP1.Process(txHash, http1, TimeOut),
                OracleHTTP2Request http2 => HTTP2.Process(txHash, http2, TimeOut),

                _ => OracleResult.CreateError(txHash, request.Hash, OracleResultError.ServerError),
            };
        }
    }
}
