using AntShares.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            //TODO: 签名
        }
    }
}
