// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            ContractParameterType[] parameterList = textBox1.Text.HexToBytes().Select(p => (ContractParameterType)p).ToArray();
            byte[] redeemScript = textBox2.Text.HexToBytes();
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
                privateKey = textBox3.Text.HexToBytes();
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
