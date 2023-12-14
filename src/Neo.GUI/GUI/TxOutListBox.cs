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
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Neo.Wallets;

namespace Neo.GUI
{
    [DefaultEvent(nameof(ItemsChanged))]
    internal partial class TxOutListBox : UserControl
    {
        public event EventHandler ItemsChanged;

        public AssetDescriptor Asset { get; set; }

        public int ItemCount => listBox1.Items.Count;

        public IEnumerable<TxOutListBoxItem> Items => listBox1.Items.OfType<TxOutListBoxItem>();

        public bool ReadOnly
        {
            get
            {
                return !panel1.Enabled;
            }
            set
            {
                panel1.Enabled = !value;
            }
        }

        private UInt160 _script_hash = null;
        public UInt160 ScriptHash
        {
            get
            {
                return _script_hash;
            }
            set
            {
                _script_hash = value;
                button3.Enabled = value == null;
            }
        }

        public TxOutListBox()
        {
            InitializeComponent();
        }

        public void Clear()
        {
            if (listBox1.Items.Count > 0)
            {
                listBox1.Items.Clear();
                button2.Enabled = false;
                ItemsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button2.Enabled = listBox1.SelectedIndices.Count > 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using PayToDialog dialog = new PayToDialog(asset: Asset, scriptHash: ScriptHash);
            if (dialog.ShowDialog() != DialogResult.OK) return;
            listBox1.Items.Add(dialog.GetOutput());
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            while (listBox1.SelectedIndices.Count > 0)
            {
                listBox1.Items.RemoveAt(listBox1.SelectedIndices[0]);
            }
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using BulkPayDialog dialog = new BulkPayDialog(Asset);
            if (dialog.ShowDialog() != DialogResult.OK) return;
            listBox1.Items.AddRange(dialog.GetOutputs());
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
