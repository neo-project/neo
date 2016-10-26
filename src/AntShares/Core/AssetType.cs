namespace AntShares.Core
{
    /// <summary>
    /// 资产类别
    /// </summary>
    public enum AssetType : byte
    {
        CreditFlag = 0x40,
        DutyFlag = 0x80,

        /// <summary>
        /// 小蚁股
        /// </summary>
        AntShare = 0x00,

        /// <summary>
        /// 小蚁币
        /// </summary>
        AntCoin = 0x01,

        /// <summary>
        /// 法币
        /// </summary>
        Currency = 0x08,

        /// <summary>
        /// 股权
        /// </summary>
        Share = DutyFlag | 0x10,

        Invoice = DutyFlag | 0x18,

        Token = CreditFlag | 0x20,
    }
}
