// Copyright (C) 2015-2024 The Neo Project.
//
// NeoEnvironmentVariableDefaults.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Hosting.App.Host
{
    internal static class NeoEnvironmentVariableDefaults
    {
        public const string PREFIX = "NEO_";
        public static readonly string PipeName = $"{PREFIX}_NAMEDPIPE__NAME";
        public static readonly string Environment = $"{PREFIX}ENVIRONMENT";
    }
}
