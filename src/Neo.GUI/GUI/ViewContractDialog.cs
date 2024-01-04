// Copyright (C) 2015-2024 The Neo Project.
//
// ViewContractDialog.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.SmartContract;
using Neo.Wallets;
using System.Linq;
using System.Windows.Forms;

namespace Neo.GUI
{
    public partial class ViewContractDialog : Form
    {
        public ViewContractDialog(Contract contract)
        {
            InitializeComponent();
            textBox1.Text = contract.ScriptHash.ToAddress(Program.Service.NeoSystem.Settings.AddressVersion);
            textBox2.Text = contract.ScriptHash.ToString();
            textBox3.Text = contract.ParameterList.Cast<byte>().ToArray().ToHexString();
            textBox4.Text = contract.Script.ToHexString();
        }
    }
}
