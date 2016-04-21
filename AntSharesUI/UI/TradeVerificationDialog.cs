using AntShares.Core;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class TradeVerificationDialog : Form
    {
        public TradeVerificationDialog(IEnumerable<TransactionOutput> outputs)
        {
            InitializeComponent();
            txOutListBox1.SetItems(outputs);
        }
    }
}
