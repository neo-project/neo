// Copyright (C) 2015-2025 The Neo Project.
//
// DBFTConfigurationNames.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.ToolSet.Configuration
{
    internal class DBFTConfigurationNames
    {
        public static readonly string IgnoreRecoveryLogsKey = "DBFT:IGNORERECOVERYLOGS";
        public static readonly string MaxBlockSizeKey = "DBFT:MAXBLOCKSIZE";
        public static readonly string MaxBlockSystemFeeKey = "DBFT:MAXBLOCKSYSTEMFEE";
        public static readonly string ExceptionPolicyKey = "DBFT:EXCEPTIONPOLICY";

        public static readonly string RecoveryLogsStoreKey = "DBFT:STOREROOT";
    }
}
