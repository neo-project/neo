using AntShares.Cryptography;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class ImportPrivateKeyDialog : Form
    {
        public ImportPrivateKeyDialog()
        {
            InitializeComponent();
        }

        public string WIF
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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            foreach (char c in textBox1.Text)
            {
                if (!Base58.Alphabet.Contains(c))
                {
                    button1.Enabled = false;
                    return;
                }
            }
            button1.Enabled = true;
        }
    }
}
