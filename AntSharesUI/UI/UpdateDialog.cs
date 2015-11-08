using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class UpdateDialog : Form
    {
        private Version latest;

        public Version LatestVersion
        {
            get
            {
                return latest;
            }
            set
            {
                latest = value;
                textBox1.Text = value.ToString();
            }
        }

        public UpdateDialog()
        {
            InitializeComponent();
            textBox2.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.antshares.com/");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start($"https://www.antshares.com/client/{latest}.zip");
        }
    }
}
