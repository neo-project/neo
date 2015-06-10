namespace AntShares.Core
{
    public enum TransactionAttributeUsage : byte
    {
        ContractHash = 0x00,
        ECDH02 = 0x02,
        ECDH03 = 0x03,
        LockAfter = 0x10,
        LockBefore = 0x11,

        Remark = 0xf0 //0xf0-0xff
    }
}
