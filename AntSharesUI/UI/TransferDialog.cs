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
            RegisterTransaction asset = comboBox1.SelectedItem as RegisterTransaction;
            if (asset == null) return null;
            return Program.CurrentWallet.MakeTransaction(new ContractTransaction
            {
                Outputs = txOutListBox1.Items.GroupBy(p => p.Account).Select(g => new TransactionOutput
                {
                    AssetId = asset.Hash,
                    Value = g.Sum(p => p.Amount),
                    ScriptHash = g.Key
                }).ToArray()
            }, Fixed8.Zero);
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
            if (tx == null)
            {
                textBox1.Text = "";
                groupBox3.Enabled = false;
            }
            else
            {
                textBox1.Text = Program.CurrentWallet.GetAvailable(tx.Hash).ToString();
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
