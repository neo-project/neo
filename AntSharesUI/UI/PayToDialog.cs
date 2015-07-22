using AntShares.Core;
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
                return textBox1.Text.ToScriptHash();
            }
            set
            {
                textBox1.Text = value.ToAddress();
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
                textBox1.Text.ToScriptHash();
            }
            catch (FormatException)
            {
                button1.Enabled = false;
                return;
            }
            long amount;
            if (!long.TryParse(textBox2.Text, out amount))
            {
                button1.Enabled = false;
                return;
            }
            button1.Enabled = true;
        }
    }
}
