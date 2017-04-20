namespace AntShares.Core
{
    /// <summary>
    /// 资产类别
    /// </summary>
    public enum AssetType : byte
    {
        CreditFlag = 0x40,
        DutyFlag = 0x80,

        SystemShare = 0x00,
        SystemCoin = 0x01,
        Currency = 0x08,
        Share = DutyFlag | 0x10,
        Invoice = DutyFlag | 0x18,
        Token = CreditFlag | 0x20,
    }
}
