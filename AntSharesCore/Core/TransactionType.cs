namespace AntShares.Core
{
    public enum TransactionType : byte
    {
        GenerationTransaction = 0x00,
        RegisterTransaction = 0x40,
        IssueTransaction = 0x50,
        ContractTransaction = 0x80,
        AgencyTransaction = 0xb0
    }
}
