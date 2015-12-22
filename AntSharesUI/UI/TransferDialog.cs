using AntShares.Core;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class TransferDialog : Form
    {
        public TransferDialog()
        {
            InitializeComponent();
        }

        public ContractTransaction GetTransaction()
        {
            RegisterTransaction tx = comboBox1.SelectedItem as RegisterTransaction;
            if (tx == null) return null;
            return Program.CurrentWallet.MakeTransaction<ContractTransaction>(listBox1.Items.OfType<TxOutListBoxItem>().GroupBy(p => p.Account).Select(g => new TransactionOutput
            {
                AssetId = tx.Hash,
                Value = g.Sum(p => p.Amount),
                ScriptHash = g.Key
            }).ToArray(), Fixed8.Zero);
        }

        private void TransferDialog_Load(object sender, EventArgs e)
        {
            foreach (UInt256 asset_id in Program.CurrentWallet.FindUnspentCoins().Select(p => p.AssetId).Distinct())
            {
                comboBox1.Items.Add(Blockchain.Default.GetTransaction(asset_id));
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RegisterTransaction tx = comboBox1.SelectedItem as RegisterTransaction;
            listBox1.Items.Clear();
            if (tx == null)
            {
                textBox1.Text = "";
                groupBox3.Enabled = false;
                button2.Enabled = false;
            }
            else
            {
                textBox1.Text = Program.CurrentWallet.GetAvailable(tx.Hash).ToString();
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
