namespace AntShares.Core
{
    /// <summary>
    /// 资产类别
    /// </summary>
    public enum AssetType : byte
    {
        /// <summary>
        /// 小蚁股
        /// </summary>
        AntShare = 0x00,
        /// <summary>
        /// 小蚁币
        /// </summary>
        AntCoin = 0x01,
        /// <summary>
        /// 股权/股份
        /// </summary>
        Share = 0x10,
        /// <summary>
        /// 货币
        /// </summary>
        Currency = 0x20,
        /// <summary>
        /// 自定义资产
        /// </summary>
        Token = 0x40,
    }
}
