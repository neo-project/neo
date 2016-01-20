using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal partial class SelectCertificateDialog : Form
    {
        public X509Certificate2 SelectedCertificate
        {
            get
            {
                return (X509Certificate2)listBox1.SelectedItem;
            }
        }

        public SelectCertificateDialog()
        {
            InitializeComponent();
        }

        private void SelectCertificateDialog_Load(object sender, EventArgs e)
        {
            using (X509Store store = new X509Store())
            {
                store.Open(OpenFlags.ReadOnly);
                listBox1.Items.AddRange(store.Certificates.OfType<object>().ToArray());
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = listBox1.SelectedIndices.Count == 1;
        }
    }
}
