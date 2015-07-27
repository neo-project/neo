using System;

namespace AntShares.Core
{
    [Flags]
    public enum BlockchainAbility : byte
    {
        None = 0,

        /// <summary>
        /// 必须实现的虚函数：GetAssets
        /// </summary>
        GetAssets = 0x01,

        /// <summary>
        /// 必须实现的虚函数：GetQuantityIssued
        /// </summary>
        GetQuantityIssued = 0x02,

        /// <summary>
        /// 必须实现的虚函数：GetUnspent, GetUnspentAntShares, IsDoubleSpend
        /// </summary>
        GetUnspent = 0x04,

        All = 0xff
    }
}
