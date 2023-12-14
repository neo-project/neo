// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;
using static Neo.Program;

namespace Neo.GUI
{
    public partial class TransferDialog : Form
    {
        public TransferDialog()
        {
            InitializeComponent();
            comboBoxFrom.Items.AddRange(Service.CurrentWallet.GetAccounts().Where(p => !p.WatchOnly).Select(p => p.Address).ToArray());
        }

        public Transaction GetTransaction()
        {
            TransferOutput[] outputs = txOutListBox1.Items.ToArray();
            UInt160 from = comboBoxFrom.SelectedItem is null ? null : ((string)comboBoxFrom.SelectedItem).ToScriptHash(Service.NeoSystem.Settings.AddressVersion);
            return Service.CurrentWallet.MakeTransaction(Service.NeoSystem.StoreView, outputs, from);
        }

        private void txOutListBox1_ItemsChanged(object sender, EventArgs e)
        {
            button3.Enabled = txOutListBox1.ItemCount > 0;
        }
    }
}
