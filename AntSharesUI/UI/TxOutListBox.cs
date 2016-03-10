using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class TxOutListBox : UserControl
    {
        public event EventHandler ItemsChanged;

        public int ItemCount => listBox1.Items.Count;
        public IEnumerable<TxOutListBoxItem> Items => listBox1.Items.OfType<TxOutListBoxItem>();

        public TxOutListBox()
        {
            InitializeComponent();
        }

        public void Clear()
        {
            listBox1.Items.Clear();
            button2.Enabled = false;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button2.Enabled = listBox1.SelectedIndices.Count > 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (PayToDialog dialog = new PayToDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                listBox1.Items.Add(new TxOutListBoxItem
                {
                    Account = dialog.Account,
                    Amount = dialog.Amount
                });
                if (ItemsChanged != null) ItemsChanged(this, EventArgs.Empty);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            while (listBox1.SelectedIndices.Count > 0)
            {
                listBox1.Items.RemoveAt(listBox1.SelectedIndices[0]);
            }
            if (ItemsChanged != null) ItemsChanged(this, EventArgs.Empty);
        }
    }
}
