// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Network.P2P.Payloads;

namespace Neo.Ledger
{

    /// <summary>
    /// Used to cache verified transactions before being written into the block.
    /// </summary>
    public partial class MemoryPool
    {
        private const double baseFee = 0.0112063;
        private const double coefficient = 0.05;

        /// <summary>
        /// User gonna pay much more fees if the pool is almost full.
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private bool CapacityCheck(Transaction tx)
        {
            var txTotalFee = tx.NetworkFee + tx.SystemFee;
            var memoryUsage = Count / (double)Capacity;
            var feeMultiplier = ComputeFeeMultiplier(memoryUsage, coefficient);
            return txTotalFee >= feeMultiplier * baseFee;
        }

        private static double ComputeFeeMultiplier(double memoryUsage, double a)
        {
            // 10 times fee at most
            return 1 + 9 * (Math.Exp(a * memoryUsage / 100) - 1) / (Math.Exp(a) - 1);
        }

    }
}
