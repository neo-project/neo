namespace Neo.SmartContract
{
	/// <summary>
	/// 表示智能合约的参数类型
	/// Represents the smart contract parameter type
	/// </summary>
	public enum ContractParameterType : byte
    {
        /// <summary>
        /// 签名
		/// Signature
        /// </summary>
        Signature = 0x00,
        Boolean = 0x01,
        /// <summary>
        /// 整数
		/// Integer
        /// </summary>
        Integer = 0x02,
        /// <summary>
        /// 160位散列值
		/// 160 bit hash value
        /// </summary>
        Hash160 = 0x03,
        /// <summary>
        /// 256位散列值
		/// 256-bit hash value
        /// </summary>
        Hash256 = 0x04,
        /// <summary>
        /// 字节数组
		/// Byte array
        /// </summary>
        ByteArray = 0x05,
        PublicKey = 0x06,
        String = 0x07,

        Array = 0x10,

        InteropInterface = 0xf0,

        Void = 0xff
    }
}
