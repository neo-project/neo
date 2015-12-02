using AntShares.Wallets;
using System;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class PayToDialog : Form
    {
        public UInt160 Account
        {
            get
            {
                return Wallet.ToScriptHash(textBox1.Text);
            }
            set
            {
                textBox1.Text = Wallet.ToAddress(value);
            }
        }

        public Fixed8 Amount
        {
            get
            {
                return Fixed8.Parse(textBox2.Text);
            }
            set
            {
                textBox2.Text = value.ToString();
            }
        }

        public PayToDialog()
        {
            InitializeComponent();
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.TextLength == 0 || textBox2.TextLength == 0)
            {
                button1.Enabled = false;
                return;
            }
            try
            {
                Wallet.ToScriptHash(textBox1.Text);
            }
            catch (FormatException)
            {
                button1.Enabled = false;
                return;
            }
            Fixed8 amount;
            if (!Fixed8.TryParse(textBox2.Text, out amount))
            {
                button1.Enabled = false;
                return;
            }
            button1.Enabled = true;
        }
    }
}
