// Copyright (C) 2015-2026 The Neo Project.
//
// RecordType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents the type of a name record (aligned with non-native NNS / DNS RR types).
    /// </summary>
    public enum RecordType : byte
    {
        /// <summary>IPv4 address record (RFC 1035).</summary>
        A = 1,

        /// <summary>Canonical name record (RFC 1035).</summary>
        CNAME = 5,

        /// <summary>Text record (RFC 1035).</summary>
        TXT = 16,

        /// <summary>IPv6 address record (RFC 3596).</summary>
        AAAA = 28
    }
}
