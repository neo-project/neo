namespace AntShares.Core
{
    public enum TransactionAttributeUsage : byte
    {
        ContractHash = 0x00,
        Remark = 0x01,

        ECDH02 = 0x02,
        ECDH03 = 0x03,

        Script = 0x20
    }
}
