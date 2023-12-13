// Copyright (C) 2015-2022 The Neo Project.
// 
// The Neo.Json is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text;

namespace Neo.Json;

static class Utility
{
    public static Encoding StrictUTF8 { get; }

    static Utility()
    {
        StrictUTF8 = (Encoding)Encoding.UTF8.Clone();
        StrictUTF8.DecoderFallback = DecoderFallback.ExceptionFallback;
        StrictUTF8.EncoderFallback = EncoderFallback.ExceptionFallback;
    }
}
