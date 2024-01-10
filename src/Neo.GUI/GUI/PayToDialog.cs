// Copyright (C) 2015-2024 The Neo Project.
//
// PayToDialog.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Wallets;
using System;
using System.Windows.Forms;
using static Neo.Program;

namespace Neo.GUI
{
    internal partial class PayToDialog : Form
    {
        public PayToDialog(AssetDescriptor asset = null, UInt160 scriptHash = null)
        {
            InitializeComponent();
            if (asset is null)
            {
                foreach (UInt160 assetId in NEP5Watched)
                {
                    try
                    {
                        comboBox1.Items.Add(new AssetDescriptor(Service.NeoSystem.StoreView, Service.NeoSystem.Settings, assetId));
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }
                }
            }
            else
            {
                comboBox1.Items.Add(asset);
                comboBox1.SelectedIndex = 0;
                comboBox1.Enabled = false;
            }
            if (scriptHash != null)
            {
                textBox1.Text = scriptHash.ToAddress(Service.NeoSystem.Settings.AddressVersion);
                textBox1.ReadOnly = true;
            }
        }

        public TxOutListBoxItem GetOutput()
        {
            AssetDescriptor asset = (AssetDescriptor)comboBox1.SelectedItem;
            return new TxOutListBoxItem
            {
                AssetName = asset.AssetName,
                AssetId = asset.AssetId,
                Value = BigDecimal.Parse(textBox2.Text, asset.Decimals),
                ScriptHash = textBox1.Text.ToScriptHash(Service.NeoSystem.Settings.AddressVersion)
            };
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem is AssetDescriptor asset)
            {
                textBox3.Text = Service.CurrentWallet.GetAvailable(Service.NeoSystem.StoreView, asset.AssetId).ToString();
            }
            else
            {
                textBox3.Text = "";
            }
            textBox_TextChanged(this, EventArgs.Empty);
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex < 0 || textBox1.TextLength == 0 || textBox2.TextLength == 0)
            {
                button1.Enabled = false;
                return;
            }
            try
            {
                textBox1.Text.ToScriptHash(Service.NeoSystem.Settings.AddressVersion);
            }
            catch (FormatException)
            {
                button1.Enabled = false;
                return;
            }
            AssetDescriptor asset = (AssetDescriptor)comboBox1.SelectedItem;
            if (!BigDecimal.TryParse(textBox2.Text, asset.Decimals, out BigDecimal amount))
            {
                button1.Enabled = false;
                return;
            }
            if (amount.Sign <= 0)
            {
                button1.Enabled = false;
                return;
            }
            button1.Enabled = true;
        }
    }
}
