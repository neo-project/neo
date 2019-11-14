using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle.Protocols;
using Neo.Oracle.Protocols.HTTP;
using Neo.SmartContract;
using System;
using System.Collections.Generic;

namespace Neo.Oracle
{
    public class OracleService
    {
        /// <summary>
        /// HTTP Protocol
        /// </summary>
        private static readonly IOracleProtocol HTTP = new OracleHTTPProtocol();

        /// <summary>
        /// Timeout
        /// </summary>
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Process transaction
        /// </summary>
        /// <param name="tx">Transaction</param>
        public Dictionary<UInt160, OracleResult> Process(Transaction tx)
        {
            var oracle = new OracleTransactionCache(ProcessInternal);

            using (var snapshot = Blockchain.Singleton.GetSnapshot())
            using (var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, tx.SystemFee, false, oracle))
            {
                if (engine.Execute() == VM.VMState.HALT)
                {
                    return oracle.Cache;
                }
            }

            return new Dictionary<UInt160, OracleResult>();
        }

        /// <summary>
        /// Process internal
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>OracleResult</returns>
        private OracleResult ProcessInternal(OracleRequest request)
        {
            switch (request)
            {
                case OracleHTTPRequest http: return HTTP.Process(http, TimeOut);

                default: return OracleResult.CreateError(request.TxHash, request.Hash, OracleResultError.ServerError);
            }
        }
    }
}
