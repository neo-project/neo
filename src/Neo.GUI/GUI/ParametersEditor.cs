// Copyright (C) 2015-2024 The Neo Project.
//
// ParametersEditor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace Neo.GUI
{
    internal partial class ParametersEditor : Form
    {
        private readonly IList<ContractParameter> parameters;

        public ParametersEditor(IList<ContractParameter> parameters)
        {
            InitializeComponent();
            this.parameters = parameters;
            listView1.Items.AddRange(parameters.Select((p, i) => new ListViewItem(new[]
            {
                new ListViewItem.ListViewSubItem
                {
                    Name = "index",
                    Text = $"[{i}]"
                },
                new ListViewItem.ListViewSubItem
                {
                    Name = "type",
                    Text = p.Type.ToString()
                },
                new ListViewItem.ListViewSubItem
                {
                    Name = "value",
                    Text = p.ToString()
                }
            }, -1)
            {
                Tag = p
            }).ToArray());
            panel1.Enabled = !parameters.IsReadOnly;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count > 0)
            {
                textBox1.Text = listView1.SelectedItems[0].SubItems["value"].Text;
                textBox2.Enabled = ((ContractParameter)listView1.SelectedItems[0].Tag).Type != ContractParameterType.Array;
                button2.Enabled = !textBox2.Enabled;
                button4.Enabled = true;
            }
            else
            {
                textBox1.Clear();
                textBox2.Enabled = true;
                button2.Enabled = false;
                button4.Enabled = false;
            }
            textBox2.Clear();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = listView1.SelectedIndices.Count > 0 && textBox2.TextLength > 0;
            button3.Enabled = textBox2.TextLength > 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0) return;
            var parameter = (ContractParameter)listView1.SelectedItems[0].Tag;
            try
            {
                parameter.SetValue(textBox2.Text);
                listView1.SelectedItems[0].SubItems["value"].Text = parameter.ToString();
                textBox1.Text = listView1.SelectedItems[0].SubItems["value"].Text;
                textBox2.Clear();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0) return;
            var parameter = (ContractParameter)listView1.SelectedItems[0].Tag;
            using var dialog = new ParametersEditor((IList<ContractParameter>)parameter.Value);
            dialog.ShowDialog();
            listView1.SelectedItems[0].SubItems["value"].Text = parameter.ToString();
            textBox1.Text = listView1.SelectedItems[0].SubItems["value"].Text;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var s = textBox2.Text;
            var parameter = new ContractParameter();
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
            {
                parameter.Type = ContractParameterType.Boolean;
                parameter.Value = true;
            }
            else if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
            {
                parameter.Type = ContractParameterType.Boolean;
                parameter.Value = false;
            }
            else if (long.TryParse(s, out var num))
            {
                parameter.Type = ContractParameterType.Integer;
                parameter.Value = num;
            }
            else if (s.StartsWith("0x"))
            {
                if (UInt160.TryParse(s, out var i160))
                {
                    parameter.Type = ContractParameterType.Hash160;
                    parameter.Value = i160;
                }
                else if (UInt256.TryParse(s, out var i256))
                {
                    parameter.Type = ContractParameterType.Hash256;
                    parameter.Value = i256;
                }
                else if (BigInteger.TryParse(s.Substring(2), NumberStyles.AllowHexSpecifier, null, out var bi))
                {
                    parameter.Type = ContractParameterType.Integer;
                    parameter.Value = bi;
                }
                else
                {
                    parameter.Type = ContractParameterType.String;
                    parameter.Value = s;
                }
            }
            else if (ECPoint.TryParse(s, ECCurve.Secp256r1, out var point))
            {
                parameter.Type = ContractParameterType.PublicKey;
                parameter.Value = point;
            }
            else
            {
                try
                {
                    parameter.Value = s.HexToBytes();
                    parameter.Type = ContractParameterType.ByteArray;
                }
                catch (FormatException)
                {
                    parameter.Type = ContractParameterType.String;
                    parameter.Value = s;
                }
            }
            parameters.Add(parameter);
            listView1.Items.Add(new ListViewItem(new[]
            {
                new ListViewItem.ListViewSubItem
                {
                    Name = "index",
                    Text = $"[{listView1.Items.Count}]"
                },
                new ListViewItem.ListViewSubItem
                {
                    Name = "type",
                    Text = parameter.Type.ToString()
                },
                new ListViewItem.ListViewSubItem
                {
                    Name = "value",
                    Text = parameter.ToString()
                }
            }, -1)
            {
                Tag = parameter
            });
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var index = listView1.SelectedIndices[0];
            parameters.RemoveAt(index);
            listView1.Items.RemoveAt(index);
        }
    }
}
