// Copyright (C) 2015-2025 The Neo Project.
//
// ProgramDefaults.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;

namespace Neo.Build.ToolSet
{
    public static class ProgramDefaults
    {
        private static readonly string s_userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static readonly string HomeRootPath = Path.Combine(s_userProfilePath, ".neo");
        public static readonly string CheckpointRootPath = Path.Combine(Environment.CurrentDirectory, "checkpoints");
    }
}
