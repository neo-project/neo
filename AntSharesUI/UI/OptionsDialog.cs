using AntShares.Properties;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class OptionsDialog : Form
    {
        public OptionsDialog()
        {
            InitializeComponent();
        }

        private void OptionsDialog_Load(object sender, EventArgs e)
        {
            textBox1.Lines = Settings.Default.Votes.OfType<string>().ToArray();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UInt256 ignore;
            Settings.Default.Votes.Clear();
            Settings.Default.Votes.AddRange(textBox1.Lines.Where(p => UInt256.TryParse(p, out ignore)).ToArray());
            Settings.Default.Save();
        }
    }
}
