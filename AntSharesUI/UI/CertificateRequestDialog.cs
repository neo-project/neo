using AntShares.UI.HyperLib;
using AntShares.Wallets;
using System;
using System.IO;
using System.Windows.Forms;

namespace AntShares.UI
{
    public partial class CertificateRequestDialog : Wizard
    {
        private Account account;

        public CertificateRequestDialog(Account account)
        {
            InitializeComponent();
            this.account = account;
        }

        private void CertificateRequestDialog_WizardStepChanged(object sender, WizardStepChangedEventArgs e)
        {
            if (radioButton1.Checked && e.CurrentStep == 3)
            {
                CurrentStep = e.PreviousStep == 2 ? 4 : 2;
            }
            else if (radioButton2.Checked && e.CurrentStep == 2)
            {
                CurrentStep = e.PreviousStep == 1 ? 3 : 1;
            }
            else if (e.CurrentStep == 4)
            {
                //TODO: 生成证书申请预览
            }
        }

        private void CertificateRequestDialog_WizardFinished(object sender, EventArgs e)
        {
            File.WriteAllBytes(textBox11.Text, textBox10.Text.HexToBytes());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;
            textBox11.Text = saveFileDialog1.FileName;
        }
    }
}
