// Copyright (C) 2015-2025 The Neo Project.
//
// SCryptModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Interfaces;
using Neo.Wallets.NEP6;

namespace Neo.Build.Core.Models.Wallets
{
    public class SCryptModel : JsonModel, IConvertToObject<ScryptParameters>
    {
        public static readonly SCryptModel Default = new()
        {
            N = 16384,
            R = 8,
            P = 8,
        };

        /// <summary>
        /// CPU/Memory cost parameter. Must be larger than 1, a power of 2 and less than 2^(128 * r / 8).
        /// </summary>
        public int N { get; set; }

        /// <summary>
        /// The block size, must be >= 1.
        /// </summary>
        public int R { get; set; }

        /// <summary>
        /// Parallelization parameter. Must be a positive integer less than or equal to Int32.MaxValue / (128 * r * 8).
        /// </summary>
        public int P { get; set; }

        public ScryptParameters ToObject() =>
            new(N, R, P);
    }
}
