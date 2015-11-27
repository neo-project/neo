using System;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class AddContractDialog : Form
    {
        public byte[] RedeemScript
        {
            get
            {
                return textBox1.Text.HexToBytes();
            }
            set
            {
                textBox1.Text = value.ToHexString();
            }
        }

        public AddContractDialog()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = textBox1.TextLength > 0;
        }
    }
}
