using AntShares.Core;
using AntShares.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class ClaimForm : Form
    {
        public ClaimForm()
        {
            InitializeComponent();
        }

        private void CalculateClaimAmountUnavailable(uint height)
        {
            textBox2.Text = Wallet.CalculateClaimAmountUnavailable(Program.CurrentWallet.FindUnspentCoins().Where(p => p.AssetId.Equals(Blockchain.AntShare.Hash)).Select(p => p.Input), height).ToString();
        }

        private void ClaimForm_Load(object sender, EventArgs e)
        {
            Fixed8 amount_available = Wallet.CalculateClaimAmount(Program.CurrentWallet.GetUnclaimedCoins().Select(p => p.Input));
            textBox1.Text = amount_available.ToString();
            if (amount_available == Fixed8.Zero) button1.Enabled = false;
            CalculateClaimAmountUnavailable(Blockchain.Default.Height);
            Blockchain.PersistCompleted += Blockchain_PersistCompleted;
        }

        private void ClaimForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Blockchain.PersistCompleted -= Blockchain_PersistCompleted;
        }

        private void Blockchain_PersistCompleted(object sender, Block block)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, Block>(Blockchain_PersistCompleted), sender, block);
            }
            else
            {
                CalculateClaimAmountUnavailable(block.Height);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TransactionInput[] claims = Program.CurrentWallet.GetUnclaimedCoins().Select(p => p.Input).ToArray();
            if (claims.Length == 0) return;
            Helper.SignAndShowInformation(new ClaimTransaction
            {
                Claims = claims,
                Attributes = new TransactionAttribute[0],
                Inputs = new TransactionInput[0],
                Outputs = new[]
                {
                    new TransactionOutput
                    {
                        AssetId = Blockchain.AntCoin.Hash,
                        Value = Wallet.CalculateClaimAmount(claims),
                        ScriptHash = Program.CurrentWallet.GetChangeAddress()
                    }
                }
            });
            Close();
        }
    }
}
