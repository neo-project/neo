namespace Neo.Implementations.Blockchains.LevelDB
{
    internal enum DataEntryPrefix : byte
    {
        DATA_Block = 0x01,
        DATA_Transaction = 0x02,

        ST_Account = 0x40,
        ST_Coin = 0x44,
        ST_SpentCoin = 0x45,
        ST_Validator = 0x48,
        ST_Asset = 0x4c,
        ST_Contract = 0x50,
        ST_Storage = 0x70,

        IX_HeaderHashList = 0x80,

        SYS_CurrentBlock = 0xc0,
        SYS_CurrentHeader = 0xc1,
        SYS_Version = 0xf0,
    }
}
