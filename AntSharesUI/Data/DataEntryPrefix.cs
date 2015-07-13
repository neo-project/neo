namespace AntShares.Data
{
    internal enum DataEntryPrefix : byte
    {
        Block = 0x00,
        Transaction = 0x01,
        Unspent = 0x02,

        IX_Register = 0x81,

        ST_QuantityIssued = 0xc1,

        Configuration = 0xf0
    }
}
