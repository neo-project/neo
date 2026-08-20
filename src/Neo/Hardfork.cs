// Copyright (C) 2015-2026 The Neo Project.
//
// Hardfork.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo
{
    public enum Hardfork : byte
    {
        HF_Aspidochelone,
        HF_Basilisk,
        HF_Cockatrice,
        HF_Domovoi,
        HF_Echidna,
        HF_Faun,
        HF_Gorgon,
        HF_Huyao,
        /// <summary>
        /// First hardfork that can be activated via Policy.activateHardfork (neo#4580).
        /// No protocol behavior is attached yet; reserved for committee/Policy activation.
        /// </summary>
        HF_Iara
    }

    /// <summary>
    /// Helpers for the raw hardfork names used by Policy (e.g. <c>Iara</c>, not <c>HF_Iara</c>).
    /// </summary>
    public static class Hardforks
    {
        /// <summary>
        /// Returns the on-chain name of a hardfork (the enum identifier without the <c>HF_</c> prefix).
        /// </summary>
        public static string GetName(Hardfork hardfork)
        {
            var name = hardfork.ToString();
            return name.StartsWith("HF_", StringComparison.Ordinal) ? name[3..] : name;
        }

        /// <summary>
        /// Parses a Policy hardfork name such as <c>Iara</c> or <c>HF_Iara</c>.
        /// </summary>
        public static bool TryParse(string? name, out Hardfork hardfork)
        {
            hardfork = default;
            if (string.IsNullOrEmpty(name))
                return false;

            if (Enum.TryParse(name, ignoreCase: true, out hardfork) && Enum.IsDefined(hardfork))
                return true;

            if (!name.StartsWith("HF_", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse("HF_" + name, ignoreCase: true, out hardfork)
                && Enum.IsDefined(hardfork))
            {
                return true;
            }

            return false;
        }
    }
}
