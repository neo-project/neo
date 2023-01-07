// Copyright (C) 2015-2023 The Neo Project.
//
// The Neo.Json is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Json;

enum JPathTokenType : byte
{
    Root,
    Dot,
    LeftBracket,
    RightBracket,
    Asterisk,
    Comma,
    Colon,
    Identifier,
    String,
    Number
}
