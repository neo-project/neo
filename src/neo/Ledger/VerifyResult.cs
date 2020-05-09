namespace Neo.Ledger
{
    public enum VerifyResult : byte
    {
        Succeed,
        AlreadyExists,
        OutOfMemory,
        UnableToVerify,
        Invalid,
        InvalidWitness,
        Expired,
        InsufficientFunds,
        InsufficientFee,
        PolicyFail,
        Unknown
    }
}
