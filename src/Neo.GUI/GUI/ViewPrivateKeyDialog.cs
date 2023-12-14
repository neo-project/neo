// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Windows.Forms;
using Neo.Wallets;

namespace Neo.GUI
{
    internal partial class ViewPrivateKeyDialog : Form
    {
        public ViewPrivateKeyDialog(WalletAccount account)
        {
            InitializeComponent();
            KeyPair key = account.GetKey();
            textBox3.Text = account.Address;
            textBox4.Text = key.PublicKey.EncodePoint(true).ToHexString();
            textBox1.Text = key.PrivateKey.ToHexString();
            textBox2.Text = key.Export();
        }
    }
}
