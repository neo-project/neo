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

        private void ChangeWallet(UserWallet wallet)
        {
            if (Program.CurrentWallet != null)
            {
                Program.CurrentWallet.BalanceChanged -= CurrentWallet_BalanceChanged;
                Program.CurrentWallet.Dispose();
            }
            Program.CurrentWallet = wallet;
            if (Program.CurrentWallet != null)
            {
                Program.CurrentWallet.BalanceChanged += CurrentWallet_BalanceChanged;
            }
            修改密码CToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            交易TToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            创建新地址NToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            导入私钥IToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            listView1.Items.Clear();
            if (Program.CurrentWallet != null)
            {
                listView1.Items.AddRange(Program.CurrentWallet.GetAddresses().Select(p => new ListViewItem(new[] { Wallet.ToAddress(p), "" }) { Name = Wallet.ToAddress(p) }).ToArray());
            }
            OnBalanceChanged();
        }

        private void OnBalanceChanged()
        {
            listView2.Items.Clear();
            if (Program.CurrentWallet != null)
            {
                listView2.Items.AddRange(Program.CurrentWallet.FindUnspentCoins().GroupBy(p => p.AssetId, (k, g) => new
                {
                    AssetId = k,
                    AssetName = ((RegisterTransaction)Blockchain.Default.GetTransaction(k)).GetName(),
                    Value = g.Sum(p => p.Value)
                }).Select(p => new ListViewItem(new[] { p.AssetName, p.Value.ToString() }) { Name = p.AssetId.ToString() }).ToArray());
            }
        }

        private void CurrentWallet_BalanceChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnBalanceChanged));
            }
            else
            {
                OnBalanceChanged();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Program.LocalNode.Start(Settings.Default.NodePort);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ChangeWallet(null);
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
                ChangeWallet(UserWallet.Create(dialog.WalletPath, dialog.Password));
            }
        }

        private void 打开钱包数据库OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenWalletDialog dialog = new OpenWalletDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                ChangeWallet(UserWallet.Open(dialog.WalletPath, dialog.Password));
            }
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

        private void 转账TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (TransferDialog dialog = new TransferDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                SignatureContext context = dialog.GetTransaction();
                if (context == null) return;
                Program.CurrentWallet.Sign(context);
                InformationBox.Show(context.ToString(), "交易构造完成。");
            }
        }

        private void 资产分发IToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (IssueDialog dialog = new IssueDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                SignatureContext context = dialog.GetTransaction();
                if (context == null) return;
                Program.CurrentWallet.Sign(context);
                InformationBox.Show(context.ToString(), "交易构造完成。");
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
            查看私钥VToolStripMenuItem.Enabled = listView1.SelectedIndices.Count == 1;
            复制到剪贴板CToolStripMenuItem.Enabled = listView1.SelectedIndices.Count == 1;
            添加合约地址ToolStripMenuItem.Enabled = listView1.SelectedIndices.Count == 1;
            删除DToolStripMenuItem.Enabled = listView1.SelectedIndices.Count > 0;
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

        private void 添加合约地址ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UInt160 scriptHash = Wallet.ToScriptHash(listView1.SelectedItems[0].Text);
            Account account = Program.CurrentWallet.GetAccountByScriptHash(scriptHash);
            Contract contract;
            using (AddContractDialog dialog = new AddContractDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                contract = new Contract(dialog.RedeemScript, account.PublicKeyHash);
            }
            Program.CurrentWallet.AddContract(contract);
            listView1.SelectedIndices.Clear();
            listView1.Items.Add(new ListViewItem(new[] { contract.Address, "" }) { Name = contract.Address });
            listView1.Items[contract.Address].Selected = true;
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
                Program.CurrentWallet.DeleteContract(scriptHash);
            }
        }
    }
}
