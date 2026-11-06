// Copyright (C) 2015-2026 The Neo Project.
//
// ApplicationEngine.Culture.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Native;
using Neo.VM;
using System.Globalization;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        /// <summary>
        /// Executes contracts with culture-independent formatting after <see cref="Hardfork.HF_Huyao"/>.
        /// </summary>
        public override VMState Execute()
        {
            var index = PersistingBlock?.Index ?? NativeContract.Ledger.CurrentIndex(SnapshotCache);
            if (!ProtocolSettings.IsHardforkEnabled(Hardfork.HF_Huyao, index))
                return base.Execute();

            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                return base.Execute();
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}
