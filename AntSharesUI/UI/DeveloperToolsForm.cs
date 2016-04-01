using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.Network;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Transaction = AntShares.Core.Transaction;

namespace AntShares.UI
{
    internal partial class DeveloperToolsForm : Form
    {
        private static readonly int[] magic = { 38, 38, 40, 40, 37, 39, 37, 39, 65, 66, 65, 66 };
        private List<int> chars = new List<int>();

        public DeveloperToolsForm()
        {
            InitializeComponent();
            tabControl1.TabPages.Remove(tabPage100);
            textBox1.Text = Blockchain.StandbyMiners[0].EncodePoint(true).ToHexString();
            textBox2.Text = Wallet.ToAddress(Blockchain.GetMinerAddress(Blockchain.StandbyMiners));
        }

        private void DeveloperToolsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (!tabControl1.TabPages.Contains(tabPage100))
            {
                chars.Add(e.KeyValue);
                if (chars.Count >= magic.Length && chars.Skip(chars.Count - magic.Length).SequenceEqual(magic))
                {
                    tabControl1.TabPages.Add(tabPage100);
                    tabControl1.SelectedTab = tabPage100;
                    KeyDown -= DeveloperToolsForm_KeyDown;
                }
            }
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {
            button7.Enabled = textBox8.TextLength > 0;
            button8.Enabled = textBox8.TextLength > 0;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SignatureContext context = SignatureContext.Parse(textBox8.Text);
            context.Signable.Scripts = context.GetScripts();
            InformationBox.Show(context.Signable.ToArray().ToHexString(), "原始数据：");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            SignatureContext context = SignatureContext.Parse(textBox8.Text);
            context.Signable.Scripts = context.GetScripts();
            Inventory inventory = (Inventory)context.Signable;
            Program.LocalNode.Relay(inventory);
            InformationBox.Show(inventory.Hash.ToString(), "数据广播成功，这是广播数据的散列值：", "广播成功");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RegisterTransaction antshare = new RegisterTransaction
            {
                AssetType = AssetType.AntShare,
#if TESTNET
                Name = "[{'lang':'zh-CN','name':'小蚁股(测试)'},{'lang':'en','name':'AntShare(TestNet)'}]",
#else
                Name = "[{'lang':'zh-CN','name':'小蚁股'},{'lang':'en','name':'AntShare'}]",
#endif
                Amount = Fixed8.FromDecimal(numericUpDown1.Value),
                Issuer = ECPoint.Parse(textBox1.Text, ECCurve.Secp256r1),
                Admin = Wallet.ToScriptHash(textBox2.Text),
                Attributes = new TransactionAttribute[0],
                Inputs = new TransactionInput[0],
                Outputs = new TransactionOutput[0]
            };
            SignatureContext context = new SignatureContext(antshare);
            InformationBox.Show(context.ToString(), "小蚁股签名上下文：");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            RegisterTransaction antshare = textBox3.Text.HexToBytes().AsSerializable<RegisterTransaction>();
            using (IssueDialog dialog = new IssueDialog(antshare))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                IssueTransaction tx = dialog.GetTransaction();
                if (tx.Outputs.Sum(p => p.Value) != antshare.Amount)
                {
                    MessageBox.Show("发行量不等于总量！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                SignatureContext context = new SignatureContext(tx);
                InformationBox.Show(context.ToString(), "小蚁股发行签名上下文：");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            const uint BitCoinNonce = 2083236893; //向比特币致敬
            Block block = new Block
            {
                PrevBlock = UInt256.Zero,
                Timestamp = DateTime.Now.ToTimestamp(),
                Height = 0,
                Nonce = BitCoinNonce,
                NextMiner = Blockchain.GetMinerAddress(Blockchain.StandbyMiners),
                Script = new Script
                {
                    StackScript = new byte[0],
                    RedeemScript = new byte[0]
                },
                Transactions = new Transaction[]
                {
                    new MinerTransaction
                    {
                        Nonce = BitCoinNonce,
                        Attributes = new TransactionAttribute[0],
                        Inputs = new TransactionInput[0],
                        Outputs = new TransactionOutput[0],
                        Scripts = new Script[0]
                    },
                    textBox3.Text.HexToBytes().AsSerializable<RegisterTransaction>(),
                    new RegisterTransaction
                    {
                        AssetType = AssetType.AntCoin,
#if TESTNET
                        Name = "[{'lang':'zh-CN','name':'小蚁币(测试)'},{'lang':'en','name':'AntCoin(TestNet)'}]",
#else
                        Name = "[{'lang':'zh-CN','name':'小蚁币'},{'lang':'en','name':'AntCoin'}]",
#endif
                        Amount = Fixed8.FromDecimal(Blockchain.MintingAmount.Sum(p => p * Blockchain.DecrementInterval)),
                        Issuer = ECCurve.Secp256r1.Infinity,
                        Admin = new UInt160(),
                        Attributes = new TransactionAttribute[0],
                        Inputs = new TransactionInput[0],
                        Outputs = new TransactionOutput[0],
                        Scripts = new Script[0]
                    },
                    textBox5.Text.HexToBytes().AsSerializable<IssueTransaction>()
                }
            };
            block.RebuildMerkleRoot();
            InformationBox.Show(block.ToArray().ToHexString(), "创世区块：");
        }
    }
}
