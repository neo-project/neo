using AntShares.Core;
using AntShares.Properties;
using System;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class SigningDialog : Form
    {
        public SigningDialog()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show(Strings.SigningFailedNoDataMessage);
                return;
            }
            SignatureContext context = SignatureContext.Parse(textBox1.Text);
            if (!Program.CurrentWallet.Sign(context))
            {
                MessageBox.Show(Strings.SigningFailedKeyNotFoundMessage);
                return;
            }
            textBox2.Text = context.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.SelectAll();
            textBox2.Copy();
        }
    }
}
