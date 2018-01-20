#pragma warning disable CS0612

using Neo.IO.Caching;

namespace Neo.Core
{
    /// <summary>
    /// 交易类型
    /// </summary>
    public enum TransactionType : byte
    {
        /// <summary>
        /// 用于分配字节费的特殊交易
        /// </summary>
        [ReflectionCache(typeof(MinerTransaction))]
        MinerTransaction = 0x00,
        /// <summary>
        /// 用于分发资产的特殊交易
        /// </summary>
        [ReflectionCache(typeof(IssueTransaction))]
        IssueTransaction = 0x01,
        [ReflectionCache(typeof(ClaimTransaction))]
        ClaimTransaction = 0x02,
        /// <summary>
        /// 用于报名成为记账候选人的特殊交易
        /// </summary>
        [ReflectionCache(typeof(EnrollmentTransaction))]
        EnrollmentTransaction = 0x20,
        /// <summary>
        /// 用于资产登记的特殊交易
        /// </summary>
        [ReflectionCache(typeof(RegisterTransaction))]
        RegisterTransaction = 0x40,
        /// <summary>
        /// 合约交易，这是最常用的一种交易
        /// </summary>
        [ReflectionCache(typeof(ContractTransaction))]
        ContractTransaction = 0x80,
        [ReflectionCache(typeof(StateTransaction))]
        StateTransaction = 0x90,
        /// <summary>
        /// Publish scripts to the blockchain for being invoked later.
        /// </summary>
        [ReflectionCache(typeof(PublishTransaction))]
        PublishTransaction = 0xd0,
        [ReflectionCache(typeof(InvocationTransaction))]
        InvocationTransaction = 0xd1
    }
}
