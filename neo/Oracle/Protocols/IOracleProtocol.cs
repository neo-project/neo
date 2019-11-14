using System;

namespace Neo.Oracle.Protocols
{
    public interface IOracleProtocol
    {
        OracleResult Process(OracleRequest request, TimeSpan timeout);
    }
}
