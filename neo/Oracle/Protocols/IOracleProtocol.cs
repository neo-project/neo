using System;

namespace Neo.Oracle.Protocols
{
    public interface IOracleProtocol
    {
        OracleResult Process(UInt256 txHash, OracleRequest request, TimeSpan timeout);
    }
}
