// Copyright (C) 2015-2024 The Neo Project.
//
// InvokeContractDialog.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Properties;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static Neo.Program;

namespace Neo.GUI
{
    internal partial class InvokeContractDialog : Form
    {
        private readonly Transaction tx;
        private JObject abi;
        private UInt160 script_hash;
        private ContractParameter[] parameters;

        public InvokeContractDialog()
        {
            InitializeComponent();
        }

        public InvokeContractDialog(Transaction tx) : this()
        {
            this.tx = tx;
            tabControl1.SelectedTab = tabPage2;
            textBox6.Text = tx.Script.Span.ToHexString();
            textBox6.ReadOnly = true;
        }

        public InvokeContractDialog(byte[] script) : this()
        {
            tabControl1.SelectedTab = tabPage2;
            textBox6.Text = script.ToHexString();
        }

        public Transaction GetTransaction()
        {
            byte[] script = textBox6.Text.Trim().FromHexString();
            return tx ?? Service.CurrentWallet.MakeTransaction(Service.NeoSystem.StoreView, script);
        }

        private void UpdateScript()
        {
            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitDynamicCall(script_hash, (string)comboBox1.SelectedItem, parameters);
            textBox6.Text = sb.ToArray().ToHexString();
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button5.Enabled = textBox6.TextLength > 0;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            byte[] script;
            try
            {
                script = textBox6.Text.Trim().FromHexString();
            }
            catch (FormatException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            Transaction tx_test = tx ?? new Transaction
            {
                Signers = new Signer[0],
                Attributes = new TransactionAttribute[0],
                Script = script,
                Witnesses = new Witness[0]
            };
            using ApplicationEngine engine = ApplicationEngine.Run(tx_test.Script, Service.NeoSystem.StoreView, container: tx_test);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"VM State: {engine.State}");
            sb.AppendLine($"Gas Consumed: {engine.GasConsumed}");
            sb.AppendLine($"Evaluation Stack: {new JArray(engine.ResultStack.Select(p => p.ToParameter().ToJson()))}");
            textBox7.Text = sb.ToString();
            if (engine.State != VMState.FAULT)
            {
                label7.Text = engine.GasConsumed + " gas";
                button3.Enabled = true;
            }
            else
            {
                MessageBox.Show(Strings.ExecutionFailed);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            textBox6.Text = File.ReadAllBytes(openFileDialog1.FileName).ToHexString();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() != DialogResult.OK) return;
            abi = (JObject)JToken.Parse(File.ReadAllText(openFileDialog2.FileName));
            script_hash = UInt160.Parse(abi["hash"].AsString());
            textBox8.Text = script_hash.ToString();
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(((JArray)abi["functions"]).Select(p => p["name"].AsString()).Where(p => p != abi["entrypoint"].AsString()).ToArray());
            textBox9.Clear();
            button8.Enabled = false;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            using (ParametersEditor dialog = new ParametersEditor(parameters))
            {
                dialog.ShowDialog();
            }
            UpdateScript();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(comboBox1.SelectedItem is string method)) return;
            JArray functions = (JArray)abi["functions"];
            var function = functions.First(p => p["name"].AsString() == method);
            JArray _params = (JArray)function["parameters"];
            parameters = _params.Select(p => new ContractParameter(p["type"].AsEnum<ContractParameterType>())).ToArray();
            textBox9.Text = string.Join(", ", _params.Select(p => p["name"].AsString()));
            button8.Enabled = parameters.Length > 0;
            UpdateScript();
        }
    }
}
