using AntShares.Wallets;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class ContractDetailsDialog : Form
    {
        public ContractDetailsDialog(Contract contract)
        {
            InitializeComponent();
            textBox1.Text = Wallet.ToAddress(contract.ScriptHash);
            textBox2.Text = contract.ScriptHash.ToString();
            textBox3.Text = contract.RedeemScript.ToHexString();
        }
    }
}
