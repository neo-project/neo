// Copyright (C) 2015-2024 The Neo Project.
//
// ImportCustomContractDialog.cs file belongs to the neo project and is free
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
using System;
using System.Linq;
using System.Windows.Forms;

namespace Neo.GUI
{
    internal partial class ImportCustomContractDialog : Form
    {
        public Contract GetContract()
        {
            ContractParameterType[] parameterList = textBox1.Text.FromHexString().Select(p => (ContractParameterType)p).ToArray();
            byte[] redeemScript = textBox2.Text.FromHexString();
            return Contract.Create(parameterList, redeemScript);
        }

        public KeyPair GetKey()
        {
            if (textBox3.TextLength == 0) return null;
            byte[] privateKey;
            try
            {
                privateKey = Wallet.GetPrivateKeyFromWIF(textBox3.Text);
            }
            catch (FormatException)
            {
                privateKey = textBox3.Text.FromHexString();
            }
            return new KeyPair(privateKey);
        }

        public ImportCustomContractDialog()
        {
            InitializeComponent();
        }

        private void Input_Changed(object sender, EventArgs e)
        {
            button1.Enabled = textBox1.TextLength > 0 && textBox2.TextLength > 0;
        }
    }
}
