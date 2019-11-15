using System;

namespace Neo.Oracle.Protocols.HTTP2
{
    public class OracleHTTP2Protocol : IOracleProtocol
    {
        /// <summary>
        /// Process HTTP2 oracle request
        /// </summary>
        /// <param name="txHash">Transaction Hash</param>
        /// <param name="request">Request</param>
        /// <param name="timeout">Timeout</param>
        /// <returns>Oracle result</returns>
        public OracleResult Process(UInt256 txHash, OracleRequest request, TimeSpan timeout)
        {
            if (!(request is OracleHTTP2Request httpRequest))
            {
                return OracleResult.CreateError(txHash, request.Hash, OracleResultError.ServerError);
            }

            // TODO: Require .net core 3.0

            return OracleResult.CreateError(txHash, request.Hash, OracleResultError.ServerError);
        }
    }
}
