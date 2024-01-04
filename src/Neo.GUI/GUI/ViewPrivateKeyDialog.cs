// Copyright (C) 2015-2024 The Neo Project.
//
// ViewPrivateKeyDialog.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Wallets;
using System.Windows.Forms;

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
