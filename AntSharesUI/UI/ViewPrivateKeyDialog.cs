using AntShares.Wallets;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class ViewPrivateKeyDialog : Form
    {
        public ViewPrivateKeyDialog(Account account, UInt160 scriptHash)
        {
            InitializeComponent();
            textBox3.Text = Wallet.ToAddress(scriptHash);
            textBox4.Text = account.PublicKey.EncodePoint(true).ToHexString();
            using (account.Decrypt())
            {
                textBox1.Text = account.PrivateKey.ToHexString();
            }
            textBox2.Text = account.Export();
        }
    }
}
