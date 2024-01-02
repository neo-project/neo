// Copyright (C) 2015-2024 The Neo Project.
//
// TxOutListBoxItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Wallets;

namespace Neo.GUI
{
    internal class TxOutListBoxItem : TransferOutput
    {
        public string AssetName;

        public override string ToString()
        {
            return $"{ScriptHash.ToAddress(Program.Service.NeoSystem.Settings.AddressVersion)}\t{Value}\t{AssetName}";
        }
    }
}
