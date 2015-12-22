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
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void AddContractToListView(Contract contract, bool selected = false)
        {
            ContractListView.Items.Add(new ListViewItem(new[] { contract.Address, contract.GetType().ToString() })
            {
                Name = contract.Address
            }).Selected = selected;
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
            高级AToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            创建新地址NToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            导入私钥IToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            创建智能合约SToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            ContractListView.Items.Clear();
            if (Program.CurrentWallet != null)
            {
                foreach (Contract contract in Program.CurrentWallet.GetContracts())
                {
                    AddContractToListView(contract);
                }
            }
            OnBalanceChanged();
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

        private void OnBalanceChanged()
        {
            listView2.Items.Clear();
            if (Program.CurrentWallet != null)
            {
                listView2.Items.AddRange(Program.CurrentWallet.FindUnspentCoins().GroupBy(p => p.AssetId, (k, g) => new
                {
                    Asset = (RegisterTransaction)Blockchain.Default.GetTransaction(k),
                    Value = g.Sum(p => p.Value)
                }).Select(p => new ListViewItem(new[] { p.Asset.GetName(), p.Value.ToString(), p.Asset.Issuer.ToString() }) { Name = p.Asset.Hash.ToString() }).ToArray());
                //TODO: 未来要自动查询证书，显示真实发行者；如果发行者没有CA认证，或证书有问题，要有提示或警告。
            }
        }

        private async Task ShowInformationAsync(SignatureContext context)
        {
            if (context.Completed)
            {
                context.Signable.Scripts = context.GetScripts();
                Transaction tx = (Transaction)context.Signable;
                await Program.LocalNode.RelayAsync(tx);
                InformationBox.Show(tx.Hash.ToString(), "交易已发送，这是交易编号(TXID)：", "交易成功");
            }
            else
            {
                InformationBox.Show(context.ToString(), "交易构造完成，但没有足够的签名：", "签名不完整");
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
                UserWallet wallet;
                try
                {
                    wallet = UserWallet.Open(dialog.WalletPath, dialog.Password);
                }
                catch (CryptographicException)
                {
                    MessageBox.Show("密码错误！");
                    return;
                }
                ChangeWallet(wallet);
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

        private async void 转账TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (TransferDialog dialog = new TransferDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Transaction tx = dialog.GetTransaction();
                if (tx == null) return;
                SignatureContext context = new SignatureContext(tx);
                Program.CurrentWallet.Sign(context);
                await ShowInformationAsync(context);
            }
        }

        private void 签名SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SigningDialog dialog = new SigningDialog())
            {
                dialog.ShowDialog();
            }
        }

        private async void 注册资产RToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AssetRegisterDialog dialog = new AssetRegisterDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Transaction tx = null;
                try
                {
                    tx = dialog.GetTransaction();
                }
                catch
                {
                    MessageBox.Show("数据填写不完整，或格式错误。");
                    return;
                }
                if (tx == null)
                {
                    MessageBox.Show("余额不足以支付系统费用。");
                    return;
                }
                SignatureContext context = new SignatureContext(tx);
                Program.CurrentWallet.Sign(context);
                await ShowInformationAsync(context);
            }
        }

        private async void 资产分发IToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (IssueDialog dialog = new IssueDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Transaction tx = dialog.GetTransaction();
                if (tx == null) return;
                SignatureContext context = new SignatureContext(tx);
                Program.CurrentWallet.Sign(context);
                await ShowInformationAsync(context);
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
            查看私钥VToolStripMenuItem.Enabled = ContractListView.SelectedIndices.Count == 1;
            复制到剪贴板CToolStripMenuItem.Enabled = ContractListView.SelectedIndices.Count == 1;
            删除DToolStripMenuItem.Enabled = ContractListView.SelectedIndices.Count > 0;
        }

        private void 创建新地址NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ContractListView.SelectedIndices.Clear();
            Account account = Program.CurrentWallet.CreateAccount();
            foreach (Contract contract in Program.CurrentWallet.GetContracts(account.PublicKeyHash))
            {
                AddContractToListView(contract, true);
            }
        }

        private void 导入私钥IToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ImportPrivateKeyDialog dialog = new ImportPrivateKeyDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                ContractListView.SelectedIndices.Clear();
                Account account = Program.CurrentWallet.Import(dialog.WIF);
                foreach (Contract contract in Program.CurrentWallet.GetContracts(account.PublicKeyHash))
                {
                    AddContractToListView(contract, true);
                }
            }
        }

        private void 多方签名MToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (CreateMultiSigContractDialog dialog = new CreateMultiSigContractDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                MultiSigContract contract = dialog.GetContract();
                if (contract == null)
                {
                    MessageBox.Show("无法添加智能合约，因为当前钱包中不包含签署该合约的私钥。");
                    return;
                }
                Program.CurrentWallet.AddContract(contract);
                ContractListView.SelectedIndices.Clear();
                AddContractToListView(contract, true);
            }
        }

        private void 查看私钥VToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UInt160 scriptHash = Wallet.ToScriptHash(ContractListView.SelectedItems[0].Text);
            Account account = Program.CurrentWallet.GetAccountByScriptHash(scriptHash);
            using (ViewPrivateKeyDialog dialog = new ViewPrivateKeyDialog(account, scriptHash))
            {
                dialog.ShowDialog();
            }
        }

        private void 复制到剪贴板CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(ContractListView.SelectedItems[0].Text);
        }

        private void 删除DToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("删除地址后，这些地址中的资产将永久性地丢失，确认要继续吗？", "删除地址确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            string[] addresses = ContractListView.SelectedItems.OfType<ListViewItem>().Select(p => p.Name).ToArray();
            foreach (string address in addresses)
            {
                ContractListView.Items.RemoveByKey(address);
                UInt160 scriptHash = Wallet.ToScriptHash(address);
                Program.CurrentWallet.DeleteContract(scriptHash);
            }
            OnBalanceChanged();
        }
    }
}
