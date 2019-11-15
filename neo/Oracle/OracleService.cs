using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle.Protocols;
using Neo.Oracle.Protocols.HTTP1;
using Neo.SmartContract;
using System;
using System.Collections.Generic;

namespace Neo.Oracle
{
    public class OracleService
    {
        /// <summary>
        /// HTTP1 Protocol
        /// </summary>
        private static readonly IOracleProtocol HTTP1 = new OracleHTTP1Protocol();

        /// <summary>
        /// Timeout
        /// </summary>
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Process transaction
        /// </summary>
        /// <param name="tx">Transaction</param>
        public OracleResultsCache Process(Transaction tx)
        {
            var oracle = new OracleResultsCache(request => ProcessInternal(tx.Hash, request));

            using (var snapshot = Blockchain.Singleton.GetSnapshot())
            using (var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, tx.SystemFee, false, oracle))
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
            switch (request)
            {
                case OracleHTTP1Request http: return HTTP1.Process(txHash, http, TimeOut);

                default: return OracleResult.CreateError(txHash, request.Hash, OracleResultError.ServerError);
            }
        }
    }
}
