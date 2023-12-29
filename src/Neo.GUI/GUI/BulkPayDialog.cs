// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;
using static Neo.Program;

namespace Neo.GUI
{
    internal partial class BulkPayDialog : Form
    {
        public BulkPayDialog(AssetDescriptor asset = null)
        {
            InitializeComponent();
            if (asset == null)
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
        }

        public TxOutListBoxItem[] GetOutputs()
        {
            AssetDescriptor asset = (AssetDescriptor)comboBox1.SelectedItem;
            return textBox1.Lines.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p =>
            {
                string[] line = p.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                return new TxOutListBoxItem
                {
                    AssetName = asset.AssetName,
                    AssetId = asset.AssetId,
                    Value = BigDecimal.Parse(line[1], asset.Decimals),
                    ScriptHash = line[0].ToScriptHash(Service.NeoSystem.Settings.AddressVersion)
                };
            }).Where(p => p.Value.Value != 0).ToArray();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem is AssetDescriptor asset)
            {
                textBox3.Text = Service.CurrentWallet!.GetAvailable(Service.NeoSystem.StoreView, asset.AssetId).ToString();
            }
            else
            {
                textBox3.Text = "";
            }
            textBox1_TextChanged(this, EventArgs.Empty);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = comboBox1.SelectedIndex >= 0 && textBox1.TextLength > 0;
        }
    }
}
