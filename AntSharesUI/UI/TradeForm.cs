using AntShares.Core;
using AntShares.IO.Json;
using AntShares.Properties;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class TradeForm : Form
    {
        public TradeForm()
        {
            InitializeComponent();
        }

        private ContractTransaction JsonToRequest(JObject json)
        {
            return new ContractTransaction
            {
                Inputs = ((JArray)json["vin"]).Select(p => new TransactionInput
                {
                    PrevHash = UInt256.Parse(p["txid"].AsString()),
                    PrevIndex = (ushort)p["vout"].AsNumber()
                }).ToArray(),
                Outputs = ((JArray)json["vout"]).Select(p => new TransactionOutput
                {
                    AssetId = UInt256.Parse(p["asset"].AsString()),
                    Value = new Fixed8((long)p["high"].AsNumber() << 32 | (long)p["low"].AsNumber()),
                    ScriptHash = Wallet.ToScriptHash(p["address"].AsString())
                }).ToArray()
            };
        }

        private JObject RequestToJson(ContractTransaction tx)
        {
            JObject json = new JObject();
            json["vin"] = tx.Inputs.Select(p => p.ToJson()).ToArray();
            json["vout"] = tx.Outputs.Select((p, i) => p.ToJson((ushort)i)).ToArray();
            json["change_address"] = Wallet.ToAddress(Program.CurrentWallet.GetChangeAddress());
            return json;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                txOutListBox1.ScriptHash = Wallet.ToScriptHash(textBox1.Text);
                txOutListBox1.Enabled = true;
            }
            catch (FormatException)
            {
                txOutListBox1.Enabled = false;
            }
            txOutListBox1.Clear();
        }

        private void txOutListBox1_ItemsChanged(object sender, EventArgs e)
        {
            button1.Enabled = txOutListBox1.ItemCount > 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ContractTransaction tx = Program.CurrentWallet.MakeTransaction(new ContractTransaction { Outputs = txOutListBox1.Items.Select(p => p.Output).ToArray() }, Fixed8.Zero);
            textBox3.Text = RequestToJson(tx).ToString();
            InformationBox.Show(textBox3.Text, Strings.TradeRequestCreatedMessage, Strings.TradeRequestCreatedCaption);
            tabControl1.SelectedTab = tabPage2;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            button2.Enabled = textBox2.TextLength > 0 && textBox3.TextLength > 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            IEnumerable<TransactionInput> inputs;
            IEnumerable<TransactionOutput> outputs;
            JObject json = JObject.Parse(textBox2.Text);
            if (json.ContainsProperty("hex"))
            {
                ContractTransaction tx_mine = JsonToRequest(JObject.Parse(textBox3.Text));
                ContractTransaction tx_others = (ContractTransaction)SignatureContext.FromJson(json).Signable;
                inputs = tx_others.Inputs.Except(tx_mine.Inputs);
                List<TransactionOutput> outputs_others = new List<TransactionOutput>(tx_others.Outputs);
                foreach (TransactionOutput output_mine in tx_mine.Outputs)
                {
                    TransactionOutput output_others = outputs_others.FirstOrDefault(p => p.AssetId == output_mine.AssetId && p.Value == output_mine.Value && p.ScriptHash == output_mine.ScriptHash);
                    if (output_others == null)
                    {
                        MessageBox.Show(Strings.TradeFailedFakeDataMessage, Strings.Failed, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    outputs_others.Remove(output_others);
                }
                outputs = outputs_others;
            }
            else
            {
                ContractTransaction tx_others = JsonToRequest(json);
                inputs = tx_others.Inputs;
                outputs = tx_others.Outputs;
            }
            try
            {
                if (inputs.Select(p => Blockchain.Default.GetTransaction(p.PrevHash).Outputs[p.PrevIndex].ScriptHash).Distinct().Any(p => Program.CurrentWallet.ContainsAddress(p)))
                {
                    MessageBox.Show(Strings.TradeFailedInvalidDataMessage, Strings.Failed, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch
            {
                MessageBox.Show(Strings.TradeFailedNoSyncMessage, Strings.Failed, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            outputs = outputs.Where(p => Program.CurrentWallet.ContainsAddress(p.ScriptHash));
            using (TradeVerificationDialog dialog = new TradeVerificationDialog(outputs))
            {
                button3.Enabled = dialog.ShowDialog() == DialogResult.OK;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SignatureContext context;
            JObject json1 = JObject.Parse(textBox2.Text);
            if (json1.ContainsProperty("hex"))
            {
                context = SignatureContext.FromJson(json1);
            }
            else
            {
                ContractTransaction tx1 = JsonToRequest(json1);
                ContractTransaction tx2 = JsonToRequest(JObject.Parse(textBox3.Text));
                context = new SignatureContext(new ContractTransaction
                {
                    Attributes = new TransactionAttribute[0],
                    Inputs = tx1.Inputs.Concat(tx2.Inputs).ToArray(),
                    Outputs = tx1.Outputs.Concat(tx2.Outputs).ToArray()
                });
            }
            Program.CurrentWallet.Sign(context);
            if (context.Completed)
            {
                context.Signable.Scripts = context.GetScripts();
                ContractTransaction tx = (ContractTransaction)context.Signable;
                Program.CurrentWallet.SaveTransaction(tx);
                Program.LocalNode.Relay(tx);
                InformationBox.Show(tx.Hash.ToString(), Strings.TradeSuccessMessage, Strings.TradeSuccessCaption);
            }
            else
            {
                InformationBox.Show(context.ToString(), Strings.TradeNeedSignatureMessage, Strings.TradeNeedSignatureCaption);
            }
        }
    }
}
