using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class ImportCustomContractDialog : Form
    {
        public Contract GetContract()
        {
            UInt160 publicKeyHash = ((ECPoint)comboBox1.SelectedItem).EncodePoint(true).ToScriptHash();
            ContractParameterType[] parameterList = textBox1.Text.HexToBytes().Cast<ContractParameterType>().ToArray();
            byte[] redeemScript = textBox2.Text.HexToBytes();
            return Contract.Create(publicKeyHash, parameterList, redeemScript);
        }

        public ImportCustomContractDialog()
        {
            InitializeComponent();
        }

        private void ImportCustomContractDialog_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(Program.CurrentWallet.GetAccounts().Select(p => p.PublicKey).ToArray());
        }

        private void Input_Changed(object sender, EventArgs e)
        {
            button1.Enabled = comboBox1.SelectedIndex >= 0 && textBox1.TextLength > 0 && textBox2.TextLength > 0;
        }
    }
}
