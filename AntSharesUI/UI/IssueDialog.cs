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
            RegisterTransaction asset = comboBox1.SelectedItem as RegisterTransaction;
            if (asset == null) return null;
            IssueTransaction tx = Program.CurrentWallet.MakeTransaction<IssueTransaction>(txOutListBox1.Items.GroupBy(p => p.Account).Select(g => new TransactionOutput
            {
                AssetId = asset.Hash,
                Value = g.Sum(p => p.Amount),
                ScriptHash = g.Key
            }).ToArray(), Fixed8.Zero);
            if (tx == null) return null;
            Random rand = new Random();
            tx.Nonce = (uint)rand.Next();
            return tx;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RegisterTransaction asset = comboBox1.SelectedItem as RegisterTransaction;
            if (asset == null)
            {
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                textBox4.Text = "";
                groupBox3.Enabled = false;
            }
            else
            {
                textBox1.Text = asset.Issuer.ToString();
                textBox2.Text = Wallet.ToAddress(asset.Admin);
                textBox3.Text = asset.Amount == -Fixed8.Satoshi ? "+\u221e" : asset.Amount.ToString();
                textBox4.Text = Blockchain.Default.GetQuantityIssued(asset.Hash).ToString();
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
