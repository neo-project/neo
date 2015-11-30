using AntShares.Core;
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
                MessageBox.Show("必须输入一段含有待签名数据的JSON对象。");
                return;
            }
            SignatureContext context = SignatureContext.Parse(textBox1.Text);
            if (!Program.CurrentWallet.Sign(context))
            {
                MessageBox.Show("没有足够的私钥对数据进行签名。");
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
