namespace Neo.Ledger
{
    public enum VerifyResult : byte
    {
        Succeed,
        AlreadyExists,
        OutOfMemory,
        UnableToVerify,
        Invalid,
        Expired,
        InsufficientFunds,
        InsufficientFee,
        PolicyFail,
        Unknown
    }
}
