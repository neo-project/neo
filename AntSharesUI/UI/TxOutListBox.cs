using AntShares.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    [DefaultEvent(nameof(ItemsChanged))]
    internal partial class TxOutListBox : UserControl
    {
        public event EventHandler ItemsChanged;

        public RegisterTransaction Asset { get; set; }
        public int ItemCount => listBox1.Items.Count;
        public IEnumerable<TxOutListBoxItem> Items => listBox1.Items.OfType<TxOutListBoxItem>();
        public bool ReadOnly
        {
            get
            {
                return !panel1.Enabled;
            }
            set
            {
                panel1.Enabled = !value;
            }
        }
        public UInt160 ScriptHash { get; set; }

        public TxOutListBox()
        {
            InitializeComponent();
        }

        public void Clear()
        {
            if (listBox1.Items.Count > 0)
            {
                listBox1.Items.Clear();
                button2.Enabled = false;
                ItemsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetItems(IEnumerable<TransactionOutput> outputs)
        {
            listBox1.Items.Clear();
            foreach (TransactionOutput output in outputs)
            {
                RegisterTransaction asset = (RegisterTransaction)Blockchain.Default.GetTransaction(output.AssetId);
                listBox1.Items.Add(new TxOutListBoxItem
                {
                    Output = output,
                    AssetName = $"{asset.GetName()} ({asset.Issuer})"
                });
            }
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button2.Enabled = listBox1.SelectedIndices.Count > 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (PayToDialog dialog = new PayToDialog(asset: Asset, scriptHash: ScriptHash))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                listBox1.Items.Add(new TxOutListBoxItem
                {
                    Output = dialog.GetOutput(),
                    AssetName = dialog.AssetName
                });
                ItemsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            while (listBox1.SelectedIndices.Count > 0)
            {
                listBox1.Items.RemoveAt(listBox1.SelectedIndices[0]);
            }
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
