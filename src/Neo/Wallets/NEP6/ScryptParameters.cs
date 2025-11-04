// Copyright (C) 2015-2025 The Neo Project.
//
// ScryptParameters.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text.Json.Nodes;

namespace Neo.Wallets.NEP6
{
    /// <summary>
    /// Represents the parameters of the SCrypt algorithm.
    /// </summary>
    public class ScryptParameters
    {
        /// <summary>
        /// The default parameters used by <see cref="NEP6Wallet"/>.
        /// </summary>
        public static ScryptParameters Default { get; } = new ScryptParameters(16384, 8, 8);

        /// <summary>
        /// CPU/Memory cost parameter. Must be larger than 1, a power of 2 and less than 2^(128 * r / 8).
        /// </summary>
        public readonly int N;

        /// <summary>
        /// The block size, must be >= 1.
        /// </summary>
        public readonly int R;

        /// <summary>
        /// Parallelization parameter. Must be a positive integer less than or equal to Int32.MaxValue / (128 * r * 8).
        /// </summary>
        public readonly int P;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScryptParameters"/> class.
        /// </summary>
        /// <param name="n">CPU/Memory cost parameter.</param>
        /// <param name="r">The block size.</param>
        /// <param name="p">Parallelization parameter.</param>
        public ScryptParameters(int n, int r, int p)
        {
            N = n;
            R = r;
            P = p;
        }

        /// <summary>
        /// Converts the parameters from a JSON object.
        /// </summary>
        /// <param name="json">The parameters represented by a JSON object.</param>
        /// <returns>The converted parameters.</returns>
        public static ScryptParameters FromJson(JsonObject json)
        {
            return new ScryptParameters(json["n"].GetValue<int>(), json["r"].GetValue<int>(), json["p"].GetValue<int>());
        }

        /// <summary>
        /// Converts the parameters to a JSON object.
        /// </summary>
        /// <returns>The parameters represented by a JSON object.</returns>
        public JsonObject ToJson()
        {
            return new()
            {
                ["n"] = N,
                ["r"] = R,
                ["p"] = P
            };
        }
    }
}
