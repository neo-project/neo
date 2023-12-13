// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Neo.Cryptography;
using Neo.Properties;
using Neo.Wallets;
using static Neo.Program;

namespace Neo.GUI
{
    internal partial class SigningDialog : Form
    {
        private class WalletEntry
        {
            public WalletAccount Account;

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(Account.Label))
                {
                    return $"[{Account.Label}] " + Account.Address;
                }
                return Account.Address;
            }
        }


        public SigningDialog()
        {
            InitializeComponent();

            cmbFormat.SelectedIndex = 0;
            cmbAddress.Items.AddRange(Service.CurrentWallet.GetAccounts()
                .Where(u => u.HasKey)
                .Select(u => new WalletEntry() { Account = u })
                .ToArray());

            if (cmbAddress.Items.Count > 0)
            {
                cmbAddress.SelectedIndex = 0;
            }
            else
            {
                textBox2.Enabled = false;
                button1.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show(Strings.SigningFailedNoDataMessage);
                return;
            }

            byte[] raw, signedData;
            try
            {
                switch (cmbFormat.SelectedIndex)
                {
                    case 0: raw = Encoding.UTF8.GetBytes(textBox1.Text); break;
                    case 1: raw = textBox1.Text.HexToBytes(); break;
                    default: return;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var account = (WalletEntry)cmbAddress.SelectedItem;
            var keys = account.Account.GetKey();

            try
            {
                signedData = Crypto.Sign(raw, keys.PrivateKey, keys.PublicKey.EncodePoint(false).Skip(1).ToArray());
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            textBox2.Text = signedData?.ToHexString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.SelectAll();
            textBox2.Copy();
        }
    }
}
