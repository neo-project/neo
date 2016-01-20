using AntShares.Core;
using AntShares.Cryptography;
using AntShares.Implementations.Wallets.EntityFramework;
using AntShares.IO;
using AntShares.Properties;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class MainForm : Form
    {
        private bool balance_changed = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void AddContractToListView(Contract contract, bool selected = false)
        {
            ContractListView.Items.Add(new ListViewItem(new[] { contract.Address, contract.GetType().ToString() })
            {
                Name = contract.Address,
                Tag = contract
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
            balance_changed = true;
        }

        private void CurrentWallet_BalanceChanged(object sender, EventArgs e)
        {
            balance_changed = true;
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
            if (balance_changed)
            {
                IEnumerable<UnspentCoin> coins = Program.CurrentWallet == null ? Enumerable.Empty<UnspentCoin>() : Program.CurrentWallet.FindUnspentCoins();
                var assets = coins.GroupBy(p => p.AssetId, (k, g) => new
                {
                    Asset = (RegisterTransaction)Blockchain.Default.GetTransaction(k),
                    Value = g.Sum(p => p.Value)
                }).ToDictionary(p => p.Asset.Hash);
                foreach (RegisterTransaction tx in listView2.Items.OfType<ListViewItem>().Select(p => (RegisterTransaction)p.Tag).ToArray())
                {
                    if (!assets.ContainsKey(tx.Hash))
                    {
                        listView2.Items.RemoveByKey(tx.Hash.ToString());
                    }
                }
                foreach (var asset in assets.Values)
                {
                    if (listView2.Items.ContainsKey(asset.Asset.Hash.ToString()))
                    {
                        listView2.Items[asset.Asset.Hash.ToString()].SubItems["value"].Text = asset.Value.ToString();
                    }
                    else
                    {
                        listView2.Items.Add(new ListViewItem(new[]
                        {
                            new ListViewItem.ListViewSubItem
                            {
                                Name = "name",
                                Text = asset.Asset.GetName()
                            },
                            new ListViewItem.ListViewSubItem
                            {
                                Name = "type",
                                Text = asset.Asset.AssetType.ToString()
                            },
                            new ListViewItem.ListViewSubItem
                            {
                                Name = "value",
                                Text = asset.Value.ToString()
                            },
                            new ListViewItem.ListViewSubItem
                            {
                                ForeColor = Color.Gray,
                                Name = "issuer",
                                Text = $"未知发行者[{asset.Asset.Issuer}]"
                            }
                        }, -1, listView2.Groups["unchecked"])
                        {
                            Name = asset.Asset.Hash.ToString(),
                            Tag = asset.Asset,
                            UseItemStyleForSubItems = false
                        });
                    }
                }
                balance_changed = false;
            }
            foreach (ListViewItem item in listView2.Groups["unchecked"].Items.OfType<ListViewItem>().ToArray())
            {
                ListViewItem.ListViewSubItem subitem = item.SubItems["issuer"];
                RegisterTransaction asset = (RegisterTransaction)item.Tag;
                using (CertificateQueryResult result = CertificateQueryService.Query(asset.Issuer))
                {
                    switch (result.Type)
                    {
                        case CertificateQueryResultType.Querying:
                        case CertificateQueryResultType.QueryFailed:
                            break;
                        case CertificateQueryResultType.System:
                            subitem.ForeColor = Color.Green;
                            subitem.Text = "小蚁系统";
                            break;
                        case CertificateQueryResultType.Invalid:
                            subitem.ForeColor = Color.Red;
                            subitem.Text = $"[证书错误][{asset.Issuer}]";
                            break;
                        case CertificateQueryResultType.Expired:
                            subitem.ForeColor = Color.Yellow;
                            subitem.Text = $"[证书已过期]{result.Certificate.Subject}[{asset.Issuer}]";
                            break;
                        case CertificateQueryResultType.Good:
                            subitem.ForeColor = Color.Green;
                            subitem.Text = $"{result.Certificate.Subject}[{asset.Issuer}]";
                            break;
                    }
                    switch (result.Type)
                    {
                        case CertificateQueryResultType.System:
                        case CertificateQueryResultType.Missing:
                        case CertificateQueryResultType.Invalid:
                        case CertificateQueryResultType.Expired:
                        case CertificateQueryResultType.Good:
                            item.Group = listView2.Groups["checked"];
                            break;
                    }
                }
            }
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

        private void importWIFToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void importCertificateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SelectCertificateDialog dialog = new SelectCertificateDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                ContractListView.SelectedIndices.Clear();
                Account account = Program.CurrentWallet.Import(dialog.SelectedCertificate);
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
            Contract contract = (Contract)ContractListView.SelectedItems[0].Tag;
            Account account = Program.CurrentWallet.GetAccountByScriptHash(contract.ScriptHash);
            using (ViewPrivateKeyDialog dialog = new ViewPrivateKeyDialog(account, contract.ScriptHash))
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
            Contract[] contracts = ContractListView.SelectedItems.OfType<ListViewItem>().Select(p => (Contract)p.Tag).ToArray();
            foreach (Contract contract in contracts)
            {
                ContractListView.Items.RemoveByKey(contract.Address);
                Program.CurrentWallet.DeleteContract(contract.ScriptHash);
            }
            balance_changed = true;
        }
    }
}
