using AntShares.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class IssueDialog : Form
    {
        private class IssueListBoxItem
        {
            public UInt160 Account;
            public Fixed8 Amount;

            public override string ToString()
            {
                return string.Format("{0}\t{1}", Account.ToAddress(), Amount);
            }
        }

        public IssueDialog()
        {
            InitializeComponent();
        }

        public IssueTransaction GetTransaction()
        {
            RegisterTransaction tx = comboBox1.SelectedItem as RegisterTransaction;
            if (tx == null) return null;
            return new IssueTransaction
            {
                Inputs = new TransactionInput[0], //TODO: 从区块链或钱包中找出负资产，并合并到交易中
                Outputs = listBox1.Items.OfType<IssueListBoxItem>().GroupBy(p => p.Account).Select(g => new TransactionOutput
                {
                    AssetId = tx.Hash,
                    ScriptHash = g.Key,
                    Value = g.Sum(p => p.Amount)
                }).ToArray()
            };
        }

        private void IssueDialog_Load(object sender, EventArgs e)
        {
            HashSet<UInt160> addresses = new HashSet<UInt160>(Program.CurrentWallet.GetAddresses());
            foreach (RegisterTransaction tx in Blockchain.Default.GetAssets())
            {
                if (addresses.Contains(tx.Admin))
                {
                    comboBox1.Items.Add(tx);
                }
            }
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
                textBox1.Text = tx.Issuer.ToAddress();
                textBox2.Text = tx.Admin.ToAddress();
                textBox3.Text = tx.Amount.ToString();
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
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;
                listBox1.Items.Add(new IssueListBoxItem
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
