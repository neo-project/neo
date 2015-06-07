using AntShares.Core;
using AntShares.Wallets;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class MainForm : Form
    {
        private UserWallet wallet_current;

        public MainForm()
        {
            InitializeComponent();
        }

        private void OnWalletChanged()
        {
            修改密码CToolStripMenuItem.Enabled = wallet_current != null;
            listView1.Items.Clear();
            if (wallet_current != null)
            {
                listView1.Items.AddRange(wallet_current.GetAddresses().Select(p => new ListViewItem(new string[] { p.ToAddress() })).ToArray());
            }
        }

        private void 创建钱包数据库NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (CreateWalletDialog dialog = new CreateWalletDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                wallet_current = UserWallet.CreateDatabase(dialog.WalletPath, dialog.Password);
            }
            OnWalletChanged();
        }

        private void 打开钱包数据库OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenWalletDialog dialog = new OpenWalletDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                wallet_current = UserWallet.OpenDatabase(dialog.WalletPath, dialog.Password);
            }
            OnWalletChanged();
        }

        private void 修改密码CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //弹出对话框，验证原密码，保存新密码
        }

        private void 官网WToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://weangel.com/project.aspx?id=57");
        }

        private void 开发人员工具TToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
