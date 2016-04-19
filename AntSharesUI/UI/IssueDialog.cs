using AntShares.Core;
using AntShares.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class IssueDialog : Form
    {
        public IssueDialog(RegisterTransaction asset = null)
        {
            InitializeComponent();
            if (asset == null)
            {
                comboBox1.Items.AddRange(Blockchain.Default.GetAssets().Where(p => Program.CurrentWallet.ContainsAddress(p.Admin)).ToArray());
            }
            else
            {
                comboBox1.Items.Add(asset);
            }
        }

        public IssueTransaction GetTransaction()
        {
            if (txOutListBox1.Asset == null) return null;
            Random rand = new Random();
            return Program.CurrentWallet.MakeTransaction(new IssueTransaction
            {
                Nonce = (uint)rand.Next(),
                Outputs = txOutListBox1.Items.GroupBy(p => p.Output.ScriptHash).Select(g => new TransactionOutput
                {
                    AssetId = txOutListBox1.Asset.Hash,
                    Value = g.Sum(p => p.Output.Value),
                    ScriptHash = g.Key
                }).ToArray()
            }, Fixed8.Zero);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            txOutListBox1.Asset = comboBox1.SelectedItem as RegisterTransaction;
            if (txOutListBox1.Asset == null)
            {
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                textBox4.Text = "";
                groupBox3.Enabled = false;
            }
            else
            {
                textBox1.Text = txOutListBox1.Asset.Issuer.ToString();
                textBox2.Text = Wallet.ToAddress(txOutListBox1.Asset.Admin);
                textBox3.Text = txOutListBox1.Asset.Amount == -Fixed8.Satoshi ? "+\u221e" : txOutListBox1.Asset.Amount.ToString();
                textBox4.Text = Blockchain.Default.GetQuantityIssued(txOutListBox1.Asset.Hash).ToString();
                groupBox3.Enabled = true;
            }
            txOutListBox1.Clear();
        }

        private void txOutListBox1_ItemsChanged(object sender, EventArgs e)
        {
            button3.Enabled = txOutListBox1.ItemCount > 0;
        }
    }
}
