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
using System.IO;
using System.Windows.Forms;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;

namespace Neo.GUI
{
    internal partial class DeployContractDialog : Form
    {
        public DeployContractDialog()
        {
            InitializeComponent();
        }

        public byte[] GetScript()
        {
            byte[] script = textBox8.Text.HexToBytes();
            string manifest = "";
            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", script, manifest);
            return sb.ToArray();
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            button2.Enabled = textBox1.TextLength > 0
                && textBox2.TextLength > 0
                && textBox3.TextLength > 0
                && textBox4.TextLength > 0
                && textBox5.TextLength > 0
                && textBox8.TextLength > 0;
            try
            {
                textBox9.Text = textBox8.Text.HexToBytes().ToScriptHash().ToString();
            }
            catch (FormatException)
            {
                textBox9.Text = "";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            textBox8.Text = File.ReadAllBytes(openFileDialog1.FileName).ToHexString();
        }
    }
}
