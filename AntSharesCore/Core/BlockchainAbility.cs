using System;

namespace AntShares.Core
{
    /// <summary>
    /// 表示特定区块链实现所提供的功能
    /// </summary>
    [Flags]
    public enum BlockchainAbility : byte
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 必须实现的虚函数：GetBlockAndHeight, GetBlockHeight, GetNextBlock, GetNextBlockHash, GetSysFeeAmount
        /// </summary>
        BlockIndexes = 0x01,

        /// <summary>
        /// 必须实现的虚函数：GetContract, GetEnrollments
        /// </summary>
        TransactionIndexes = 0x02,

        /// <summary>
        /// 必须实现的虚函数：ContainsUnspent, GetUnclaimed, GetUnspent, GetVotes, IsDoubleSpend
        /// </summary>
        UnspentIndexes = 0x04,

        /// <summary>
        /// 必须实现的虚函数：GetQuantityIssued
        /// </summary>
        Statistics = 0x08,

        /// <summary>
        /// 所有的功能
        /// </summary>
        All = 0xff
    }
}
