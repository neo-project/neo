using AntShares.Core;
using AntShares.Implementations.Wallets.EntityFramework;
using AntShares.IO;
using AntShares.Properties;
using AntShares.Wallets;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
            交易TToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            创建新地址NToolStripMenuItem.Enabled = true;
            导入私钥IToolStripMenuItem.Enabled = true;
            listView1.Items.Clear();
            if (Program.CurrentWallet != null)
            {
                listView1.Items.AddRange(Program.CurrentWallet.GetAddresses().Select(p => new ListViewItem(new[] { Wallet.ToAddress(p), "" }) { Name = Wallet.ToAddress(p) }).ToArray());
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Program.LocalNode.Start(Settings.Default.NodePort);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lbl_height.Text = $"{Blockchain.Default.Height}/{Blockchain.Default.HeaderHeight}";
            lbl_count_node.Text = Program.LocalNode.RemoteNodeCount.ToString();
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

        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void 签名SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SigningDialog dialog = new SigningDialog())
            {
                dialog.ShowDialog();
            }
        }

        private void 资产分发IToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (IssueDialog dialog = new IssueDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                IssueTransaction tx = dialog.GetTransaction();
                if (tx == null) return;
                //TODO: 检查是否符合规则，如是否超过总量、分发方式是否符合约定等；
                SignatureContext context = new SignatureContext(tx);
                Program.CurrentWallet.Sign(context);
                if (context.Completed)
                {
                    context.Signable.Scripts = context.GetScripts();
                    InformationBox.Show(context.Signable.ToArray().ToHexString(), "分发交易构造完成，并已完整签名，可以广播。");
                }
                else
                {
                    InformationBox.Show(context.ToString(), "分发交易构造完成，但签名信息还不完整。");
                }
            }
        }

        private void 官网WToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.antshares.com/");
        }

        private void 开发人员工具TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Helper.Show<DeveloperToolsForm>();
        }

        private void 关于AntSharesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"小蚁(AntShares) 版本：{Assembly.GetExecutingAssembly().GetName().Version}", "关于");
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            删除DToolStripMenuItem.Enabled = listView1.SelectedIndices.Count > 0;
            查看私钥VToolStripMenuItem.Enabled = listView1.SelectedIndices.Count == 1;
            复制到剪贴板CToolStripMenuItem.Enabled = listView1.SelectedIndices.Count == 1;
        }

        private void 创建新地址NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.SelectedIndices.Clear();
            Account account = Program.CurrentWallet.CreateAccount();
            foreach (Contract contract in Program.CurrentWallet.GetContracts(account.PublicKeyHash))
            {
                listView1.Items.Add(new ListViewItem(new[] { contract.Address, "" }) { Name = contract.Address });
                listView1.Items[contract.Address].Selected = true;
            }
        }

        private void 导入私钥IToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ImportPrivateKeyDialog dialog = new ImportPrivateKeyDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                listView1.SelectedIndices.Clear();
                Account account = Program.CurrentWallet.Import(dialog.WIF);
                foreach (Contract contract in Program.CurrentWallet.GetContracts(account.PublicKeyHash))
                {
                    listView1.Items.Add(new ListViewItem(new[] { contract.Address, "" }) { Name = contract.Address });
                    listView1.Items[contract.Address].Selected = true;
                }
            }
        }

        private void 查看私钥VToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UInt160 scriptHash = Wallet.ToScriptHash(listView1.SelectedItems[0].Text);
            Account account = Program.CurrentWallet.GetAccountByScriptHash(scriptHash);
            using (ViewPrivateKeyDialog dialog = new ViewPrivateKeyDialog(account, scriptHash))
            {
                dialog.ShowDialog();
            }
        }

        private void 复制到剪贴板CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(listView1.SelectedItems[0].Text);
        }

        private void 删除DToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("删除地址后，这些地址中的资产将永久性地丢失，确认要继续吗？", "删除地址确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            string[] addresses = listView1.SelectedItems.OfType<ListViewItem>().Select(p => p.Name).ToArray();
            foreach (string address in addresses)
            {
                listView1.Items.RemoveByKey(address);
                UInt160 scriptHash = Wallet.ToScriptHash(address);
                Account account = Program.CurrentWallet.GetAccountByScriptHash(scriptHash);
                if (account == null) continue;
                Program.CurrentWallet.DeleteAccount(account.PublicKeyHash);
            }
        }
    }
}
