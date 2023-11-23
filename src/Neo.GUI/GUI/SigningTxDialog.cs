// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Properties;
using Neo.SmartContract;
using System;
using System.Windows.Forms;
using static Neo.Program;

namespace Neo.GUI
{
    internal partial class SigningTxDialog : Form
    {
        public SigningTxDialog()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show(Strings.SigningFailedNoDataMessage);
                return;
            }
            ContractParametersContext context = ContractParametersContext.Parse(textBox1.Text, Service.NeoSystem.StoreView);
            if (!Service.CurrentWallet.Sign(context))
            {
                MessageBox.Show(Strings.SigningFailedKeyNotFoundMessage);
                return;
            }
            textBox2.Text = context.ToString();
            if (context.Completed) button4.Visible = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.SelectAll();
            textBox2.Copy();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ContractParametersContext context = ContractParametersContext.Parse(textBox2.Text, Service.NeoSystem.StoreView);
            if (!(context.Verifiable is Transaction tx))
            {
                MessageBox.Show("Only support to broadcast transaction.");
                return;
            }
            tx.Witnesses = context.GetWitnesses();
            Service.NeoSystem.Blockchain.Tell(tx);
            InformationBox.Show(tx.Hash.ToString(), Strings.RelaySuccessText, Strings.RelaySuccessTitle);
            button4.Visible = false;
        }
    }
}
