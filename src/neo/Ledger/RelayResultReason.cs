namespace Neo.Ledger
{
    public enum RelayResultReason : byte
    {
        Succeed,
        AlreadyExists,
        OutOfMemory,
        UnableToVerify,
        Invalid,
        Expired,
        InsufficientFunds,
        PolicyFail,
        Unknown
    }
}
