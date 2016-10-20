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

        private void ClaimForm_Load(object sender, EventArgs e)
        {
            Fixed8 amount_available = Wallet.CalculateClaimAmount(Program.CurrentWallet.GetUnclaimedCoins().Select(p => p.Input));
            Fixed8 amount_unavailable = Wallet.CalculateClaimAmountUnavailable(Program.CurrentWallet.FindUnspentCoins().Where(p => p.AssetId.Equals(Blockchain.AntShare.Hash)).Select(p => p.Input), Blockchain.Default.Height);
            textBox1.Text = amount_available.ToString();
            textBox2.Text = amount_unavailable.ToString();
            if (amount_available == Fixed8.Zero) button1.Enabled = false;
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
