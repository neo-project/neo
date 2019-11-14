namespace Neo.Oracle
{
    public enum OracleResultError : byte
    {
        None = 0,
        Timeout = 1,
        ServerError = 2,
        PolicyError = 3,
        FilterError = 4,
    }
}
