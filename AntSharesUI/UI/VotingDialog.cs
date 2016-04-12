using AntShares.Core;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class VotingDialog : Form
    {
        public VotingDialog()
        {
            InitializeComponent();
        }

        public VotingTransaction GetTransaction()
        {
            return Program.CurrentWallet.MakeTransaction(new VotingTransaction
            {
                Enrollments = textBox1.Lines.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => UInt256.Parse(p)).ToArray(),
                Outputs = new[]
                {
                    new TransactionOutput
                    {
                        AssetId = Blockchain.AntShare.Hash,
                        Value = Program.CurrentWallet.GetAvailable(Blockchain.AntShare.Hash),
                        ScriptHash = Program.CurrentWallet.GetChangeAddress()
                    }
                }
            }, Fixed8.Zero);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = textBox1.TextLength > 0;
        }
    }
}
