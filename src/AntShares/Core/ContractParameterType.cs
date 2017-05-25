namespace AntShares.Core
{
    /// <summary>
    /// 表示智能合约的参数类型
    /// </summary>
    public enum ContractParameterType : byte
    {
        /// <summary>
        /// 签名
        /// </summary>
        Signature = 0,
        Boolean = 1,
        /// <summary>
        /// 整数
        /// </summary>
        Integer = 2,
        /// <summary>
        /// 160位散列值
        /// </summary>
        Hash160 = 3,
        /// <summary>
        /// 256位散列值
        /// </summary>
        Hash256 = 4,
        /// <summary>
        /// 字节数组
        /// </summary>
        ByteArray = 5,
        PublicKey = 6,

        Void = 0xff
    }
}
