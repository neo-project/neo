using AntShares.Core;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.Network;
using AntShares.Properties;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class DeveloperToolsForm : Form
    {
        public DeveloperToolsForm()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = textBox1.TextLength > 0;
            button2.Enabled = textBox1.TextLength > 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SignatureContext context = SignatureContext.Parse(textBox1.Text);
            context.Signable.Scripts = context.GetScripts();
            InformationBox.Show(context.Signable.ToArray().ToHexString(), Strings.RelayTitle);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SignatureContext context = SignatureContext.Parse(textBox1.Text);
            context.Signable.Scripts = context.GetScripts();
            IInventory inventory = (IInventory)context.Signable;
            Program.LocalNode.Relay(inventory);
            InformationBox.Show(inventory.Hash.ToString(), Strings.RelaySuccessText, Strings.RelaySuccessTitle);
        }
    }
}
