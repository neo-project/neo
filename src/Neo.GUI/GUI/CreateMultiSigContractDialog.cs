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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Neo.Wallets;
using static Neo.Program;

namespace Neo.GUI
{
    internal partial class CreateMultiSigContractDialog : Form
    {
        private ECPoint[] publicKeys;

        public CreateMultiSigContractDialog()
        {
            InitializeComponent();
        }

        public Contract GetContract()
        {
            publicKeys = listBox1.Items.OfType<string>().Select(p => ECPoint.DecodePoint(p.HexToBytes(), ECCurve.Secp256r1)).ToArray();
            return Contract.CreateMultiSigContract((int)numericUpDown2.Value, publicKeys);
        }

        public KeyPair GetKey()
        {
            HashSet<ECPoint> hashSet = new HashSet<ECPoint>(publicKeys);
            return Service.CurrentWallet.GetAccounts().FirstOrDefault(p => p.HasKey && hashSet.Contains(p.GetKey().PublicKey))?.GetKey();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            button6.Enabled = numericUpDown2.Value > 0;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button5.Enabled = listBox1.SelectedIndices.Count > 0;
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            button4.Enabled = textBox5.TextLength > 0;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add(textBox5.Text);
            textBox5.Clear();
            numericUpDown2.Maximum = listBox1.Items.Count;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            numericUpDown2.Maximum = listBox1.Items.Count;
        }
    }
}
