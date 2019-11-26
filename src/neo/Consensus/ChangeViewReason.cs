namespace Neo.Consensus
{
    public enum ChangeViewReason : byte
    {
        Timeout = 0x0,
        ChangeAgreement = 0x1,
        TxNotFound = 0x2,
        TxRejectedByPolicy = 0x3,
        TxInvalid = 0x4,
        BlockRejectedByPolicy = 0x5
    }
}
