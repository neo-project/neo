using AntShares.Core;
using AntShares.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class IssueDialog : Form
    {
        public IssueDialog()
        {
            InitializeComponent();
        }

        public IssueTransaction GetTransaction()
        {
            RegisterTransaction asset = comboBox1.SelectedItem as RegisterTransaction;
            if (asset == null) return null;
            IssueTransaction tx = Program.CurrentWallet.MakeTransaction<IssueTransaction>(listBox1.Items.OfType<TxOutListBoxItem>().GroupBy(p => p.Account).Select(g => new TransactionOutput
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

        private void IssueDialog_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(Blockchain.Default.GetAssets().Where(p => Program.CurrentWallet.ContainsAddress(p.Admin)).ToArray());
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RegisterTransaction tx = comboBox1.SelectedItem as RegisterTransaction;
            listBox1.Items.Clear();
            if (tx == null)
            {
                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                textBox4.Text = "";
                groupBox3.Enabled = false;
                button2.Enabled = false;
            }
            else
            {
                textBox1.Text = tx.Issuer.ToString();
                textBox2.Text = Wallet.ToAddress(tx.Admin);
                textBox3.Text = tx.Amount == -Fixed8.Satoshi ? "+\u221e" : tx.Amount.ToString();
                textBox4.Text = Blockchain.Default.GetQuantityIssued(tx.Hash).ToString();
                groupBox3.Enabled = true;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button2.Enabled = listBox1.SelectedIndices.Count > 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (PayToDialog dialog = new PayToDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                listBox1.Items.Add(new TxOutListBoxItem
                {
                    Account = dialog.Account,
                    Amount = dialog.Amount
                });
                button3.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            while (listBox1.SelectedIndices.Count > 0)
            {
                listBox1.Items.RemoveAt(listBox1.SelectedIndices[0]);
            }
            button3.Enabled = listBox1.Items.Count > 0;
        }
    }
}
