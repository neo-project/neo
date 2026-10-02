// Copyright (C) 2015-2026 The Neo Project.
//
// InteropPriceAttribute.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.SmartContract
{
    /// <summary>
    /// Specifies the fixed price of the interoperable service starting from the given hardfork.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class InteropPriceAttribute : Attribute
    {
        /// <summary>
        /// The hardfork the price is applied from.
        /// </summary>
        public Hardfork Since { get; }

        /// <summary>
        /// The price coefficient. It's multiplied by the execution fee factor
        /// (in the unit of picoGAS) and gives the price in the unit of femtoGAS.
        /// </summary>
        public long Coefficient { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InteropPriceAttribute"/> class.
        /// </summary>
        /// <param name="since">The hardfork the price is applied from.</param>
        /// <param name="coefficient">The price coefficient.</param>
        public InteropPriceAttribute(Hardfork since, long coefficient)
        {
            Since = since;
            Coefficient = coefficient;
        }
    }
}
