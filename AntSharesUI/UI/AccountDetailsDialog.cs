using AntShares.Core;
using AntShares.Cryptography;
using AntShares.Wallets;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class AccountDetailsDialog : Form
    {
        public AccountDetailsDialog(WalletEntry entry)
        {
            InitializeComponent();
            textBox1.Text = entry.ScriptHash.ToAddress();
            textBox2.Text = entry.ScriptHash.ToString();
            textBox3.Text = string.Format("{0}/{1}", entry.N, entry.M);
            textBox4.Text = entry.RedeemScript.ToHexString();
            textBox5.Text = string.Join("\r\n", entry.PublicKeys.Select(p => Secp256r1Point.FromBytes(p).ToString()));
        }
    }
}
