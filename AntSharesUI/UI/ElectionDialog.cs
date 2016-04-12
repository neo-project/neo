using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class ElectionDialog : Form
    {
        public ElectionDialog()
        {
            InitializeComponent();
        }

        public EnrollmentTransaction GetTransaction()
        {
            return Program.CurrentWallet.MakeTransaction(new EnrollmentTransaction
            {
                PublicKey = (ECPoint)comboBox1.SelectedItem,
                Outputs = new[]
                {
                    new TransactionOutput
                    {
                        AssetId = Blockchain.AntCoin.Hash,
                        Value = Fixed8.Parse(textBox1.Text),
                        ScriptHash = SignatureContract.CreateSignatureRedeemScript((ECPoint)comboBox1.SelectedItem).ToScriptHash()
                    }
                }
            }, Fixed8.Zero);
        }

        private void ElectionDialog_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(Program.CurrentWallet.GetAccounts().Select(p => p.PublicKey).ToArray());
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = comboBox1.SelectedIndex >= 0 && textBox1.TextLength > 0;
        }
    }
}
