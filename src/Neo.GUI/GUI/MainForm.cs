// Copyright (C) 2015-2024 The Neo Project.
//
// MainForm.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.IO.Actors;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Properties;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml.Linq;
using static Neo.Program;
using static Neo.SmartContract.Helper;
using VMArray = Neo.VM.Types.Array;

namespace Neo.GUI
{
    internal partial class MainForm : Form
    {
        private bool check_nep5_balance = false;
        private DateTime persistence_time = DateTime.MinValue;
        private IActorRef actor;

        public MainForm(XDocument xdoc = null)
        {
            InitializeComponent();

            toolStripProgressBar1.Maximum = (int)Service.NeoSystem.Settings.TimePerBlock.TotalSeconds;

            if (xdoc != null)
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                Version latest = Version.Parse(xdoc.Element("update").Attribute("latest").Value);
                if (version < latest)
                {
                    toolStripStatusLabel3.Tag = xdoc;
                    toolStripStatusLabel3.Text += $": {latest}";
                    toolStripStatusLabel3.Visible = true;
                }
            }
        }

        private void AddAccount(WalletAccount account, bool selected = false)
        {
            ListViewItem item = listView1.Items[account.Address];
            if (item != null)
            {
                if (!account.WatchOnly && ((WalletAccount)item.Tag).WatchOnly)
                {
                    listView1.Items.Remove(item);
                    item = null;
                }
            }
            if (item == null)
            {
                string groupName = account.WatchOnly ? "watchOnlyGroup" : IsSignatureContract(account.Contract.Script) ? "standardContractGroup" : "nonstandardContractGroup";
                item = listView1.Items.Add(new ListViewItem(new[]
                {
                    new ListViewItem.ListViewSubItem
                    {
                        Name = "address",
                        Text = account.Address
                    },
                    new ListViewItem.ListViewSubItem
                    {
                        Name = NativeContract.NEO.Symbol
                    },
                    new ListViewItem.ListViewSubItem
                    {
                        Name = NativeContract.GAS.Symbol
                    }
                }, -1, listView1.Groups[groupName])
                {
                    Name = account.Address,
                    Tag = account
                });
            }
            item.Selected = selected;
        }

        private void Blockchain_PersistCompleted(Blockchain.PersistCompleted e)
        {
            if (IsDisposed) return;
            persistence_time = DateTime.UtcNow;
            if (Service.CurrentWallet != null)
                check_nep5_balance = true;
            BeginInvoke(new Action(RefreshConfirmations));
        }

        private static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }

        private void Service_WalletChanged(object sender, Wallet wallet)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<Wallet>(Service_WalletChanged), sender, wallet);
                return;
            }

            listView3.Items.Clear();
            修改密码CToolStripMenuItem.Enabled = wallet != null;
            交易TToolStripMenuItem.Enabled = wallet != null;
            signDataToolStripMenuItem.Enabled = wallet != null;
            deployContractToolStripMenuItem.Enabled = wallet != null;
            invokeContractToolStripMenuItem.Enabled = wallet != null;
            选举EToolStripMenuItem.Enabled = wallet != null;
            创建新地址NToolStripMenuItem.Enabled = wallet != null;
            导入私钥IToolStripMenuItem.Enabled = wallet != null;
            创建智能合约SToolStripMenuItem.Enabled = wallet != null;
            listView1.Items.Clear();
            if (wallet != null)
            {
                foreach (WalletAccount account in wallet.GetAccounts().ToArray())
                {
                    AddAccount(account);
                }
            }
            check_nep5_balance = true;
        }

        private void RefreshConfirmations()
        {
            foreach (ListViewItem item in listView3.Items)
            {
                uint? height = item.Tag as uint?;
                int? confirmations = (int)NativeContract.Ledger.CurrentIndex(Service.NeoSystem.StoreView) - (int?)height + 1;
                if (confirmations <= 0) confirmations = null;
                item.SubItems["confirmations"].Text = confirmations?.ToString() ?? Strings.Unconfirmed;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            actor = Service.NeoSystem.ActorSystem.ActorOf(EventWrapper<Blockchain.PersistCompleted>.Props(Blockchain_PersistCompleted));
            Service.WalletChanged += Service_WalletChanged;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (actor != null)
                Service.NeoSystem.ActorSystem.Stop(actor);
            Service.WalletChanged -= Service_WalletChanged;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            uint height = NativeContract.Ledger.CurrentIndex(Service.NeoSystem.StoreView);
            uint headerHeight = Service.NeoSystem.HeaderCache.Last?.Index ?? height;

            lbl_height.Text = $"{height}/{headerHeight}";
            lbl_count_node.Text = Service.LocalNode.ConnectedCount.ToString();
            TimeSpan persistence_span = DateTime.UtcNow - persistence_time;
            if (persistence_span < TimeSpan.Zero) persistence_span = TimeSpan.Zero;
            if (persistence_span > Service.NeoSystem.Settings.TimePerBlock)
            {
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                toolStripProgressBar1.Value = persistence_span.Seconds;
                toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
            }
            if (Service.CurrentWallet is null) return;
            if (!check_nep5_balance || persistence_span < TimeSpan.FromSeconds(2)) return;
            check_nep5_balance = false;
            UInt160[] addresses = Service.CurrentWallet.GetAccounts().Select(p => p.ScriptHash).ToArray();
            if (addresses.Length == 0) return;
            using var snapshot = Service.NeoSystem.GetSnapshot();
            foreach (UInt160 assetId in NEP5Watched)
            {
                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    for (int i = addresses.Length - 1; i >= 0; i--)
                        sb.EmitDynamicCall(assetId, "balanceOf", addresses[i]);
                    sb.Emit(OpCode.DEPTH, OpCode.PACK);
                    sb.EmitDynamicCall(assetId, "decimals");
                    sb.EmitDynamicCall(assetId, "name");
                    script = sb.ToArray();
                }
                using ApplicationEngine engine = ApplicationEngine.Run(script, snapshot, gas: 0_20000000L * addresses.Length);
                if (engine.State.HasFlag(VMState.FAULT)) continue;
                string name = engine.ResultStack.Pop().GetString();
                byte decimals = (byte)engine.ResultStack.Pop().GetInteger();
                BigInteger[] balances = ((VMArray)engine.ResultStack.Pop()).Select(p => p.GetInteger()).ToArray();
                string symbol = null;
                if (assetId.Equals(NativeContract.NEO.Hash))
                    symbol = NativeContract.NEO.Symbol;
                else if (assetId.Equals(NativeContract.GAS.Hash))
                    symbol = NativeContract.GAS.Symbol;
                if (symbol != null)
                    for (int i = 0; i < addresses.Length; i++)
                        listView1.Items[addresses[i].ToAddress(Service.NeoSystem.Settings.AddressVersion)].SubItems[symbol].Text = new BigDecimal(balances[i], decimals).ToString();
                BigInteger amount = balances.Sum();
                if (amount == 0)
                {
                    listView2.Items.RemoveByKey(assetId.ToString());
                    continue;
                }
                BigDecimal balance = new BigDecimal(amount, decimals);
                if (listView2.Items.ContainsKey(assetId.ToString()))
                {
                    listView2.Items[assetId.ToString()].SubItems["value"].Text = balance.ToString();
                }
                else
                {
                    listView2.Items.Add(new ListViewItem(new[]
                    {
                        new ListViewItem.ListViewSubItem
                        {
                            Name = "name",
                            Text = name
                        },
                        new ListViewItem.ListViewSubItem
                        {
                            Name = "type",
                            Text = "NEP-5"
                        },
                        new ListViewItem.ListViewSubItem
                        {
                            Name = "value",
                            Text = balance.ToString()
                        },
                        new ListViewItem.ListViewSubItem
                        {
                            ForeColor = Color.Gray,
                            Name = "issuer",
                            Text = $"ScriptHash:{assetId}"
                        }
                    }, -1)
                    {
                        Name = assetId.ToString(),
                        UseItemStyleForSubItems = false
                    });
                }
            }
        }

        private void 创建钱包数据库NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using CreateWalletDialog dialog = new CreateWalletDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return;
            Service.CreateWallet(dialog.WalletPath, dialog.Password);
        }

        private void 打开钱包数据库OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using OpenWalletDialog dialog = new OpenWalletDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return;
            try
            {
                Service.OpenWallet(dialog.WalletPath, dialog.Password);
            }
            catch (CryptographicException)
            {
                MessageBox.Show(Strings.PasswordIncorrect);
            }
        }

        private void 修改密码CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using ChangePasswordDialog dialog = new ChangePasswordDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return;
            if (Service.CurrentWallet.ChangePassword(dialog.OldPassword, dialog.NewPassword))
            {
                if (Service.CurrentWallet is NEP6Wallet wallet)
                    wallet.Save();
                MessageBox.Show(Strings.ChangePasswordSuccessful);
            }
            else
            {
                MessageBox.Show(Strings.PasswordIncorrect);
            }
        }

        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void 转账TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Transaction tx;
            using (TransferDialog dialog = new TransferDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                tx = dialog.GetTransaction();
            }
            using (InvokeContractDialog dialog = new InvokeContractDialog(tx))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                tx = dialog.GetTransaction();
            }
            Helper.SignAndShowInformation(tx);
        }

        private void 签名SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using SigningTxDialog dialog = new SigningTxDialog();
            dialog.ShowDialog();
        }

        private void deployContractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] script;
                using (DeployContractDialog dialog = new DeployContractDialog())
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    script = dialog.GetScript();
                }
                using (InvokeContractDialog dialog = new InvokeContractDialog(script))
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    Helper.SignAndShowInformation(dialog.GetTransaction());
                }
            }
            catch { }
        }

        private void invokeContractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using InvokeContractDialog dialog = new InvokeContractDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return;
            try
            {
                Helper.SignAndShowInformation(dialog.GetTransaction());
            }
            catch
            {
                return;
            }
        }

        private void 选举EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] script;
                using (ElectionDialog dialog = new ElectionDialog())
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    script = dialog.GetScript();
                }
                using (InvokeContractDialog dialog = new InvokeContractDialog(script))
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    Helper.SignAndShowInformation(dialog.GetTransaction());
                }
            }
            catch { }
        }

        private void signDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using SigningDialog dialog = new SigningDialog();
            dialog.ShowDialog();
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void 官网WToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenBrowser("https://neo.org/");
        }

        private void 开发人员工具TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Helper.Show<DeveloperToolsForm>();
        }

        private void consoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Helper.Show<ConsoleForm>();
        }

        private void 关于AntSharesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"{Strings.AboutMessage} {Strings.AboutVersion}{Assembly.GetExecutingAssembly().GetName().Version}", Strings.About);
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            查看私钥VToolStripMenuItem.Enabled =
                listView1.SelectedIndices.Count == 1 &&
                !((WalletAccount)listView1.SelectedItems[0].Tag).WatchOnly &&
                IsSignatureContract(((WalletAccount)listView1.SelectedItems[0].Tag).Contract.Script);
            viewContractToolStripMenuItem.Enabled =
                listView1.SelectedIndices.Count == 1 &&
                !((WalletAccount)listView1.SelectedItems[0].Tag).WatchOnly;
            voteToolStripMenuItem.Enabled =
                listView1.SelectedIndices.Count == 1 &&
                !((WalletAccount)listView1.SelectedItems[0].Tag).WatchOnly &&
                !string.IsNullOrEmpty(listView1.SelectedItems[0].SubItems[NativeContract.NEO.Symbol].Text) &&
                decimal.Parse(listView1.SelectedItems[0].SubItems[NativeContract.NEO.Symbol].Text) > 0;
            复制到剪贴板CToolStripMenuItem.Enabled = listView1.SelectedIndices.Count == 1;
            删除DToolStripMenuItem.Enabled = listView1.SelectedIndices.Count > 0;
        }

        private void 创建新地址NToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.SelectedIndices.Clear();
            WalletAccount account = Service.CurrentWallet.CreateAccount();
            AddAccount(account, true);
            if (Service.CurrentWallet is NEP6Wallet wallet)
                wallet.Save();
        }

        private void importWIFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using ImportPrivateKeyDialog dialog = new ImportPrivateKeyDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return;
            listView1.SelectedIndices.Clear();
            foreach (string wif in dialog.WifStrings)
            {
                WalletAccount account;
                try
                {
                    account = Service.CurrentWallet.Import(wif);
                }
                catch (FormatException)
                {
                    continue;
                }
                AddAccount(account, true);
            }
            if (Service.CurrentWallet is NEP6Wallet wallet)
                wallet.Save();
        }

        private void importWatchOnlyAddressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string text = InputBox.Show(Strings.Address, Strings.ImportWatchOnlyAddress);
            if (string.IsNullOrEmpty(text)) return;
            using (StringReader reader = new StringReader(text))
            {
                while (true)
                {
                    string address = reader.ReadLine();
                    if (address == null) break;
                    address = address.Trim();
                    if (string.IsNullOrEmpty(address)) continue;
                    UInt160 scriptHash;
                    try
                    {
                        scriptHash = address.ToScriptHash(Service.NeoSystem.Settings.AddressVersion);
                    }
                    catch (FormatException)
                    {
                        continue;
                    }
                    WalletAccount account = Service.CurrentWallet.CreateAccount(scriptHash);
                    AddAccount(account, true);
                }
            }
            if (Service.CurrentWallet is NEP6Wallet wallet)
                wallet.Save();
        }

        private void 多方签名MToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using CreateMultiSigContractDialog dialog = new CreateMultiSigContractDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return;
            Contract contract = dialog.GetContract();
            if (contract == null)
            {
                MessageBox.Show(Strings.AddContractFailedMessage);
                return;
            }
            WalletAccount account = Service.CurrentWallet.CreateAccount(contract, dialog.GetKey());
            if (Service.CurrentWallet is NEP6Wallet wallet)
                wallet.Save();
            listView1.SelectedIndices.Clear();
            AddAccount(account, true);
        }

        private void 自定义CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using ImportCustomContractDialog dialog = new ImportCustomContractDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return;
            Contract contract = dialog.GetContract();
            WalletAccount account = Service.CurrentWallet.CreateAccount(contract, dialog.GetKey());
            if (Service.CurrentWallet is NEP6Wallet wallet)
                wallet.Save();
            listView1.SelectedIndices.Clear();
            AddAccount(account, true);
        }

        private void 查看私钥VToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WalletAccount account = (WalletAccount)listView1.SelectedItems[0].Tag;
            using ViewPrivateKeyDialog dialog = new ViewPrivateKeyDialog(account);
            dialog.ShowDialog();
        }

        private void viewContractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WalletAccount account = (WalletAccount)listView1.SelectedItems[0].Tag;
            using ViewContractDialog dialog = new ViewContractDialog(account.Contract);
            dialog.ShowDialog();
        }

        private void voteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                WalletAccount account = (WalletAccount)listView1.SelectedItems[0].Tag;
                byte[] script;
                using (VotingDialog dialog = new VotingDialog(account.ScriptHash))
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    script = dialog.GetScript();
                }
                using (InvokeContractDialog dialog = new InvokeContractDialog(script))
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    Helper.SignAndShowInformation(dialog.GetTransaction());
                }
            }
            catch { }
        }

        private void 复制到剪贴板CToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(listView1.SelectedItems[0].Text);
            }
            catch (ExternalException) { }
        }

        private void 删除DToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Strings.DeleteAddressConfirmationMessage, Strings.DeleteAddressConfirmationCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;
            WalletAccount[] accounts = listView1.SelectedItems.OfType<ListViewItem>().Select(p => (WalletAccount)p.Tag).ToArray();
            foreach (WalletAccount account in accounts)
            {
                listView1.Items.RemoveByKey(account.Address);
                Service.CurrentWallet.DeleteAccount(account.ScriptHash);
            }
            if (Service.CurrentWallet is NEP6Wallet wallet)
                wallet.Save();
            check_nep5_balance = true;
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count == 0) return;
            Clipboard.SetDataObject(listView3.SelectedItems[0].SubItems[1].Text);
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0) return;
            OpenBrowser($"https://neoscan.io/address/{listView1.SelectedItems[0].Text}");
        }

        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count == 0) return;
            OpenBrowser($"https://neoscan.io/asset/{listView2.SelectedItems[0].Name[2..]}");
        }

        private void listView3_DoubleClick(object sender, EventArgs e)
        {
            if (listView3.SelectedIndices.Count == 0) return;
            OpenBrowser($"https://neoscan.io/transaction/{listView3.SelectedItems[0].Name[2..]}");
        }

        private void toolStripStatusLabel3_Click(object sender, EventArgs e)
        {
            using UpdateDialog dialog = new UpdateDialog((XDocument)toolStripStatusLabel3.Tag);
            dialog.ShowDialog();
        }
    }
}
