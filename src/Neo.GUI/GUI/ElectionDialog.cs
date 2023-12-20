// Copyright (C) 2015-2024 The Neo Project.
//
// ElectionDialog.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Linq;
using System.Windows.Forms;
using static Neo.Program;
using static Neo.SmartContract.Helper;

namespace Neo.GUI
{
    public partial class ElectionDialog : Form
    {
        public ElectionDialog()
        {
            InitializeComponent();
        }

        public byte[] GetScript()
        {
            ECPoint pubkey = (ECPoint)comboBox1.SelectedItem;
            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitDynamicCall(NativeContract.NEO.Hash, "registerValidator", pubkey);
            return sb.ToArray();
        }

        private void ElectionDialog_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(Service.CurrentWallet.GetAccounts().Where(p => !p.WatchOnly && IsSignatureContract(p.Contract.Script)).Select(p => p.GetKey().PublicKey).ToArray());
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex >= 0)
            {
                button1.Enabled = true;
            }
        }
    }
}
