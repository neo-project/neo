using AntShares.Core;
using AntShares.Core.Scripts;
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class MainForm : Form
    {
        private static readonly UInt160 RecycleScriptHash = new byte[] { (byte)ScriptOp.OP_DUP, (byte)ScriptOp.OP_HASH160, (byte)ScriptOp.OP_CALL, (byte)InterfaceOp.CHAIN_HEIGHT, (byte)ScriptOp.OP_CALL, (byte)InterfaceOp.CHAIN_GETHEADER, (byte)ScriptOp.OP_TOALTSTACK, (byte)ScriptOp.OP_CALL, (byte)InterfaceOp.HEADER_NEXTMINER, (byte)ScriptOp.OP_EQUALVERIFY, 0xB0/*OP_EVAL*/ }.ToScriptHash();
        private bool balance_changed = false;
        private DateTime persistence_time = DateTime.MinValue;

        public MainForm()
        {
            InitializeComponent();
        }

        private void AddContractToListView(Contract contract, bool selected = false)
        {
            listView1.Items.Add(new ListViewItem(new[] { contract.Address, contract.GetType().ToString() })
            {
                Name = contract.Address,
                Tag = contract
            }).Selected = selected;
        }

        private void Blockchain_PersistCompleted(object sender, Block block)
        {
            persistence_time = DateTime.Now;
        }

        private void ChangeWallet(UserWallet wallet)
        {
            if (Program.CurrentWallet != null)
            {
                Program.CurrentWallet.BalanceChanged -= CurrentWallet_BalanceChanged;
                Program.CurrentWallet.TransactionsChanged -= CurrentWallet_TransactionsChanged;
                Program.CurrentWallet.Dispose();
            }
            Program.CurrentWallet = wallet;
            listView3.Items.Clear();
            if (Program.CurrentWallet != null)
            {
                CurrentWallet_TransactionsChanged(null, Program.CurrentWallet.LoadTransactions());
                Program.CurrentWallet.BalanceChanged += CurrentWallet_BalanceChanged;
                Program.CurrentWallet.TransactionsChanged += CurrentWallet_TransactionsChanged;
            }
            修改密码CToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            重建钱包数据库RToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            交易TToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            高级AToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            创建新地址NToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            导入私钥IToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            创建智能合约SToolStripMenuItem.Enabled = Program.CurrentWallet != null;
            listView1.Items.Clear();
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

        private void CurrentWallet_TransactionsChanged(object sender, IEnumerable<TransactionInfo> transactions)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, IEnumerable<TransactionInfo>>(CurrentWallet_TransactionsChanged), sender, transactions);
            }
            else
            {
                foreach (TransactionInfo info in transactions)
                {
                    string txid = info.Transaction.Hash.ToString();
                    if (listView3.Items.ContainsKey(txid))
                    {
                        listView3.Items[txid].Tag = info;
                    }
                    else
                    {
                        listView3.Items.Insert(0, new ListViewItem(new[]
                        {
                            new ListViewItem.ListViewSubItem
                            {
                                Name = "time",
                                Text = info.Time.ToString()
                            },
                            new ListViewItem.ListViewSubItem
                            {
                                Name = "hash",
                                Text = txid
                            },
                            new ListViewItem.ListViewSubItem
                            {
                                Name = "confirmations",
                                Text = "未确认"
                            }
                        }, -1)
                        {
                            Name = txid,
                            Tag = info
                        });
                    }
                }
                foreach (ListViewItem item in listView3.Items)
                {
                    item.SubItems["confirmations"].Text = (Blockchain.Default.Height - ((TransactionInfo)item.Tag).Height + 1)?.ToString() ?? "未确认";
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Program.LocalNode.Start(Settings.Default.NodePort);
            Blockchain.PersistCompleted += Blockchain_PersistCompleted;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Blockchain.PersistCompleted -= Blockchain_PersistCompleted;
            ChangeWallet(null);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lbl_height.Text = $"{Blockchain.Default.Height}/{Blockchain.Default.HeaderHeight}";
            lbl_count_node.Text = Program.LocalNode.RemoteNodeCount.ToString();
            TimeSpan persistence_span = DateTime.Now - persistence_time;
            if (persistence_span > Blockchain.TimePerBlock)
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                toolStripProgressBar1.Value = persistence_span.Seconds;
                toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
            }
            if (balance_changed)
            {
                IEnumerable<Coin> coins = Program.CurrentWallet?.FindCoins() ?? Enumerable.Empty<Coin>();
                var assets = coins.GroupBy(p => p.AssetId, (k, g) => new
                {
                    Asset = (RegisterTransaction)Blockchain.Default.GetTransaction(k),
                    Value = g.Sum(p => p.Value),
                    Available = g.Where(p => p.State == CoinState.Unspent).Sum(p => p.Value)
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
                byte[] cert_url_data = asset.Attributes.FirstOrDefault(p => p.Usage == TransactionAttributeUsage.CertUrl)?.Data;
                string cert_url = cert_url_data == null ? null : Encoding.UTF8.GetString(cert_url_data);
                using (CertificateQueryResult result = CertificateQueryService.Query(asset.Issuer, cert_url))
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
                            subitem.ForeColor = Color.Black;
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
                if (UserWallet.GetVersion(dialog.WalletPath) < Version.Parse("0.6.6043.32131"))
                {
                    if (MessageBox.Show("正在打开旧版本的钱包文件，是否尝试将文件升级为新版格式？\n注意，升级后将无法用旧版本的客户端打开该文件！", "钱包文件升级", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes)
                        return;
                    string path_old = Path.ChangeExtension(dialog.WalletPath, ".old.db3");
                    string path_new = Path.ChangeExtension(dialog.WalletPath, ".new.db3");
                    UserWallet.Migrate(dialog.WalletPath, path_new);
                    File.Move(dialog.WalletPath, path_old);
                    File.Move(path_new, dialog.WalletPath);
                    MessageBox.Show($"钱包文件迁移成功，旧的文件已经自动保存到以下位置：\n{path_old}");
                }
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

        private void 重建钱包数据库RToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();
            listView3.Items.Clear();
            Program.CurrentWallet.Rebuild();
        }

        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void 转账TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (TransferDialog dialog = new TransferDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Helper.SignAndShowInformation(dialog.GetTransaction());
            }
        }

        private void 交易TToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (TradeForm form = new TradeForm())
            {
                form.ShowDialog();
            }
        }

        private void 签名SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SigningDialog dialog = new SigningDialog())
            {
                dialog.ShowDialog();
            }
        }

        private void 提取小蚁币CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Helper.Show<ClaimForm>();
        }

        private void 注册资产RToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AssetRegisterDialog dialog = new AssetRegisterDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Helper.SignAndShowInformation(dialog.GetTransaction());
            }
        }

        private void 资产分发IToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (IssueDialog dialog = new IssueDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Helper.SignAndShowInformation(dialog.GetTransaction());
            }
        }

        private void 选举EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ElectionDialog dialog = new ElectionDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Helper.SignAndShowInformation(dialog.GetTransaction());
            }
        }

        private void 投票VToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (VotingDialog dialog = new VotingDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                Helper.SignAndShowInformation(dialog.GetTransaction());
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
            删除DToolStripMenuItem.Enabled = listView1.SelectedIndices.Count > 0;
        }

        private void 创建新地址NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.SelectedIndices.Clear();
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
                listView1.SelectedIndices.Clear();
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
                listView1.SelectedIndices.Clear();
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
                listView1.SelectedIndices.Clear();
                AddContractToListView(contract, true);
            }
        }

        private void 自定义CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ImportCustomContractDialog dialog = new ImportCustomContractDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                CustomContract contract = dialog.GetContract();
                Program.CurrentWallet.AddContract(contract);
                listView1.SelectedIndices.Clear();
                AddContractToListView(contract, true);
            }
        }

        private void 查看私钥VToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Contract contract = (Contract)listView1.SelectedItems[0].Tag;
            Account account = Program.CurrentWallet.GetAccountByScriptHash(contract.ScriptHash);
            using (ViewPrivateKeyDialog dialog = new ViewPrivateKeyDialog(account, contract.ScriptHash))
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
            Contract[] contracts = listView1.SelectedItems.OfType<ListViewItem>().Select(p => (Contract)p.Tag).ToArray();
            foreach (Contract contract in contracts)
            {
                listView1.Items.RemoveByKey(contract.Address);
                Program.CurrentWallet.DeleteContract(contract.ScriptHash);
            }
            balance_changed = true;
        }

        private void contextMenuStrip2_Opening(object sender, CancelEventArgs e)
        {
            删除DToolStripMenuItem1.Enabled = listView2.SelectedIndices.Count > 0;
        }

        private void 删除DToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count == 0) return;
            var delete = listView2.SelectedItems.OfType<ListViewItem>().Select(p => (RegisterTransaction)p.Tag).Select(p => new
            {
                Asset = p,
                Value = Program.CurrentWallet.GetAvailable(p.Hash)
            }).ToArray();
            if (MessageBox.Show("资产删除后将无法恢复，您确定要删除以下资产吗？\n"
                + string.Join("\n", delete.Select(p => $"{p.Asset.GetName()}:{p.Value}"))
                , "删除确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            ContractTransaction tx = Program.CurrentWallet.MakeTransaction(new ContractTransaction
            {
                Outputs = delete.Select(p => new TransactionOutput
                {
                    AssetId = p.Asset.Hash,
                    Value = p.Value,
                    ScriptHash = RecycleScriptHash
                }).ToArray()
            }, Fixed8.Zero);
            Helper.SignAndShowInformation(tx);
        }
    }
}
