namespace Neo.Ledger
{
    public enum RelayResultReason : byte
    {
        Succeed = 0x00,
        AlreadyExists = 0x01,
        OutOfMemory = 0x02,
        UnableToVerify = 0x03,
        Invalid = 0x04,
        PolicyFail = 0x05,
        Unknown = 0xff
    }
}
