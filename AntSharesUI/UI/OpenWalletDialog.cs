using System;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class OpenWalletDialog : Form
    {
        public OpenWalletDialog()
        {
            InitializeComponent();
        }

        public string Password
        {
            get
            {
                return textBox2.Text;
            }
            set
            {
                textBox2.Text = value;
            }
        }

        public string WalletPath
        {
            get
            {
                return textBox1.Text;
            }
            set
            {
                textBox1.Text = value;
            }
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.TextLength == 0 || textBox2.TextLength == 0)
            {
                button2.Enabled = false;
                return;
            }
            button2.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }
    }
}
