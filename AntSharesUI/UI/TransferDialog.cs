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
            return Program.CurrentWallet.MakeTransaction(new ContractTransaction
            {
                Outputs = txOutListBox1.Items.GroupBy(p => new { p.Output.AssetId, p.Output.ScriptHash }).Select(g => new TransactionOutput
                {
                    AssetId = g.Key.AssetId,
                    Value = g.Sum(p => p.Output.Value),
                    ScriptHash = g.Key.ScriptHash
                }).ToArray()
            }, Fixed8.Zero);
        }

        private void txOutListBox1_ItemsChanged(object sender, EventArgs e)
        {
            button3.Enabled = txOutListBox1.ItemCount > 0;
        }
    }
}
