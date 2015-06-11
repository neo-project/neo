using AntShares.Core;
using AntShares.IO;
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
            if (context.Completed)
            {
                context.Signable.Scripts = context.GetScripts();
                textBox2.Text = context.Signable.ToArray().ToHexString();
                MessageBox.Show("签名完成，该对象的签名信息已经完整，可以广播。");
            }
            else
            {
                textBox2.Text = context.ToString();
                MessageBox.Show("签名完成，但该对象的签名信息还不完整。");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.SelectAll();
            textBox2.Copy();
        }
    }
}
