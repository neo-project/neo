namespace Neo.Oracle.Protocols
{
    public interface IOracleProtocol
    {
        OracleResult Process(UInt256 txHash, OracleRequest request, OraclePolicy policy);
    }
}
