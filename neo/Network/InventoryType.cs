namespace Neo.Network
{
    /// <summary>
    /// 定义清单中的对象类型
	/// Object types
    /// </summary>
    public enum InventoryType : byte
    {
        /// <summary>
        /// 交易
		/// Transaction
        /// </summary>
        TX = 0x01,
        /// <summary>
        /// 区块
		/// Block
        /// </summary>
        Block = 0x02,
        /// <summary>
        /// 共识数据
		/// Consensus Data
        /// </summary>
        Consensus = 0xe0
    }
}
