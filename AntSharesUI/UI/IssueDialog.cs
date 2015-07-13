using AntShares.Core;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class IssueDialog : Form
    {
        public IssueDialog()
        {
            InitializeComponent();
        }

        private void IssueDialog_Load(object sender, EventArgs e)
        {
            //TODO: 对已登记的资产进行分发操作
            //1. 检索当前钱包所有账户，找出登记过的所有资产；(OK)
            //2. 用户选择一种资产，并设置分发数量等；
            //3. 检查是否符合规则，如是否超过总量、分发方式是否符合约定等；
            //4. 构造交易，签名；
            //5. 广播交易
            HashSet<UInt160> addresses = new HashSet<UInt160>(Program.CurrentWallet.GetAddresses());
            foreach (RegisterTransaction tx in Program.Blockchain.GetAssets())
            {
                if (addresses.Contains(tx.Issuer) || addresses.Contains(tx.Admin))
                {
                    comboBox1.Items.Add(tx);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RegisterTransaction tx = comboBox1.SelectedItem as RegisterTransaction;
            if (tx == null) return;
            textBox1.Text = tx.Amount.ToString();
        }
    }
}
