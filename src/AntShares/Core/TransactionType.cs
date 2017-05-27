namespace AntShares.Core
{
    /// <summary>
    /// 交易类型
    /// </summary>
    public enum TransactionType : byte
    {
        /// <summary>
        /// 用于分配字节费的特殊交易
        /// </summary>
        MinerTransaction = 0x00,
        /// <summary>
        /// 用于分发资产的特殊交易
        /// </summary>
        IssueTransaction = 0x01,
        /// <summary>
        /// 用于分配小蚁币的特殊交易
        /// </summary>
        ClaimTransaction = 0x02,
        /// <summary>
        /// 用于报名成为记账候选人的特殊交易
        /// </summary>
        EnrollmentTransaction = 0x20,
        /// <summary>
        /// 用于资产登记的特殊交易
        /// </summary>
        RegisterTransaction = 0x40,
        /// <summary>
        /// 合约交易，这是最常用的一种交易
        /// </summary>
        ContractTransaction = 0x80,
        /// <summary>
        /// Publish scripts to the blockchain for being invoked later.
        /// </summary>
        PublishTransaction = 0xd0,
        InvocationTransaction = 0xd1
    }
}
