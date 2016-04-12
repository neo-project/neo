using AntShares.Core;
using AntShares.Cryptography.ECC;
using AntShares.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class AssetRegisterDialog : Form
    {
        public AssetRegisterDialog()
        {
            InitializeComponent();
        }

        public RegisterTransaction GetTransaction()
        {
            return Program.CurrentWallet.MakeTransaction(new RegisterTransaction
            {
                AssetType = (AssetType)comboBox1.SelectedItem,
                Name = (AssetType)comboBox1.SelectedItem == AssetType.Share ? string.Empty : $"[{{'lang':'zh-CN','name':'{textBox1.Text}'}}]",
                Amount = checkBox1.Checked ? Fixed8.Parse(textBox2.Text) : -Fixed8.Satoshi,
                Issuer = (ECPoint)comboBox2.SelectedItem,
                Admin = Wallet.ToScriptHash(comboBox3.Text),
                Outputs = new TransactionOutput[0]
            }, Fixed8.Zero);
        }

        private void AssetRegisterDialog_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(Enum.GetValues(typeof(AssetType)).OfType<AssetType>().Where(p => p >= AssetType.Share).OfType<object>().ToArray());
            comboBox2.Items.AddRange(Program.CurrentWallet.GetAccounts().Select(p => p.PublicKey).ToArray());
            comboBox3.Items.AddRange(Program.CurrentWallet.GetContracts().ToArray());
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = (AssetType)comboBox1.SelectedItem != AssetType.Share;
            checkBox1.Enabled = (AssetType)comboBox1.SelectedItem == AssetType.Token;
            if ((AssetType)comboBox1.SelectedItem == AssetType.Share) checkBox1.Checked = true;
            else if ((AssetType)comboBox1.SelectedItem == AssetType.Currency) checkBox1.Checked = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox2.Enabled = checkBox1.Checked;
        }
    }
}
