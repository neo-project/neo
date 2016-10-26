namespace AntShares.Wallets
{
    /// <summary>
    /// 表示智能合约的参数类型
    /// </summary>
    public enum ContractParameterType : byte
    {
        /// <summary>
        /// 签名
        /// </summary>
        Signature,
        /// <summary>
        /// 整数
        /// </summary>
        Integer,
        /// <summary>
        /// 160位散列值
        /// </summary>
        Hash160,
        /// <summary>
        /// 256位散列值
        /// </summary>
        Hash256,
        /// <summary>
        /// 字节数组
        /// </summary>
        ByteArray
    }
}
