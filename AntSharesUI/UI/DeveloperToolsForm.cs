using AntShares.Core;
using System;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class DeveloperToolsForm : Form
    {
        public DeveloperToolsForm()
        {
            InitializeComponent();
            tabControl1.TabPages.Remove(tabPage100);
        }

        private void tabControl1_DoubleClick(object sender, EventArgs e)
        {
            if (!tabControl1.TabPages.Contains(tabPage100))
            {
                Random rand = new Random();
                if (rand.Next() % 50 == 0)
                {
                    tabControl1.TabPages.Add(tabPage100);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RegisterTransaction antshare = new RegisterTransaction
            {
                RegisterType = RegisterType.AntShare,
                RegisterName = "[{'lang':'zh-CHS','name':'小蚁股'},{'lang':'en','name':'AntShare'}]",
                Amount = (Int64)numericUpDown1.Value,
                Issuer = textBox1.Text.ToScriptHash(),
                Admin = textBox2.Text.ToScriptHash(),
                Inputs = new TransactionInput[0],
                Outputs = new TransactionOutput[0]
            };
            SignatureContext context = new SignatureContext(antshare);
            InformationBox.Show(context.ToString(), "小蚁股签名上下文");
        }
    }
}
