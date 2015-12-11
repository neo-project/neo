using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class CreateMultiSigContractDialog : Form
    {
        public CreateMultiSigContractDialog()
        {
            InitializeComponent();
        }

        public MultiSigContract GetContract()
        {
            ECPoint[] publicKeys = listBox1.Items.OfType<string>().Select(p => ECPoint.DecodePoint(p.HexToBytes(), ECCurve.Secp256r1)).ToArray();
            foreach (ECPoint publicKey in publicKeys)
            {
                Account account = Program.CurrentWallet.GetAccount(publicKey.EncodePoint(true).ToScriptHash());
                if (account != null)
                {
                    return MultiSigContract.Create(account.PublicKeyHash, (int)numericUpDown2.Value, publicKeys);
                }
            }
            return null;
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            button6.Enabled = numericUpDown2.Value > 0;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button5.Enabled = listBox1.SelectedIndices.Count > 0;
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            button4.Enabled = textBox5.TextLength > 0;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Add(textBox5.Text);
            textBox5.Clear();
            numericUpDown2.Maximum = listBox1.Items.Count;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            numericUpDown2.Maximum = listBox1.Items.Count;
        }
    }
}
