namespace AntShares.Data
{
    internal enum DataEntryPrefix : byte
    {
        Block = 0x01,
        Transaction = 0x02,

        IX_Asset = 0x81,
        IX_Enrollment = 0x84,
        IX_Unspent = 0x90,
        IX_AntShare = 0x91,
        IX_Vote = 0x94,

        ST_Height = 0xc0,
        ST_QuantityIssued = 0xc1,
        Configuration = 0xf0
    }
}
