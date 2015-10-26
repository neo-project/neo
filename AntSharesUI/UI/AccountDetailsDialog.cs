using AntShares.Wallets;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class AccountDetailsDialog : Form
    {
        public AccountDetailsDialog(WalletEntry entry)
        {
            InitializeComponent();
            textBox1.Text = Wallet.ToAddress(entry.ScriptHash);
            textBox2.Text = entry.ScriptHash.ToString();
            textBox3.Text = entry.RedeemScript.ToHexString();
        }
    }
}
