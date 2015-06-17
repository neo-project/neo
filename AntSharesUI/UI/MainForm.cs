using AntShares.Core;
using AntShares.Wallets;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void OnWalletChanged()
        {
            修改密码CToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            签名SToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            listView1.Items.Clear();
            if (Program.CurrentWallet != null)
            {
                listView1.Items.AddRange(Program.CurrentWallet.GetAddresses().Select(p => new ListViewItem(new string[] { p.ToAddress() })).ToArray());
            }
        }

        private void 创建钱包数据库NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (CreateWalletDialog dialog = new CreateWalletDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Program.CurrentWallet = UserWallet.CreateDatabase(dialog.WalletPath, dialog.Password);
            }
            OnWalletChanged();
        }

        private void 打开钱包数据库OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenWalletDialog dialog = new OpenWalletDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Program.CurrentWallet = UserWallet.OpenDatabase(dialog.WalletPath, dialog.Password);
            }
            OnWalletChanged();
        }

        private void 修改密码CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //弹出对话框，验证原密码，保存新密码
        }

        private void 签名SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SigningDialog dialog = new SigningDialog())
            {
                dialog.ShowDialog();
            }
        }

        private void 官网WToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://weangel.com/project.aspx?id=57");
        }

        private void 开发人员工具TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Helper.Show<DeveloperToolsForm>();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            显示详情DToolStripMenuItem.Enabled = listView1.SelectedIndices.Count == 1;
            复制到剪贴板CToolStripMenuItem.Enabled = listView1.SelectedIndices.Count == 1;
        }

        private void 显示详情DToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WalletEntry entry = Program.CurrentWallet.GetEntry(listView1.SelectedItems[0].Text.ToScriptHash());
            using (AccountDetailsDialog dialog = new AccountDetailsDialog(entry))
            {
                dialog.ShowDialog();
            }
        }

        private void 复制到剪贴板CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(listView1.SelectedItems[0].Text);
        }
    }
}
