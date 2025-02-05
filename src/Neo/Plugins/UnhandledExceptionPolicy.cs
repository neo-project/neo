// Copyright (C) 2015-2025 The Neo Project.
//
// UnhandledExceptionPolicy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins
{
    public enum UnhandledExceptionPolicy : byte
    {
        Ignore = 0,
        StopPlugin = 1,
        StopNode = 2,
    }
}
