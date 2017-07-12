namespace Neo.Core
{
    /// <summary>
    /// 交易结果，表示交易中资产的变化量
    /// </summary>
    public class TransactionResult
    {
        /// <summary>
        /// 资产编号
        /// </summary>
        public UInt256 AssetId;
        /// <summary>
        /// 该资产的变化量
        /// </summary>
        public Fixed8 Amount;
    }
}
