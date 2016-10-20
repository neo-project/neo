using AntShares.Core;
using AntShares.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal static class Helper
    {
        private static Dictionary<Type, Form> tool_forms = new Dictionary<Type, Form>();

        private static void Helper_FormClosing(object sender, FormClosingEventArgs e)
        {
            tool_forms.Remove(sender.GetType());
        }

        public static void Show<T>() where T : Form, new()
        {
            Type t = typeof(T);
            if (!tool_forms.ContainsKey(t))
            {
                tool_forms.Add(t, new T());
                tool_forms[t].FormClosing += Helper_FormClosing;
            }
            tool_forms[t].Show();
            tool_forms[t].Activate();
        }

        public static void SignAndShowInformation(Transaction tx)
        {
            if (tx == null)
            {
                MessageBox.Show(Strings.InsufficientFunds);
                return;
            }
            if (tx.Attributes.All(p => p.Usage != TransactionAttributeUsage.Vote) && tx.Outputs.Any(p => p.AssetId.Equals(Blockchain.AntShare.Hash)) && Settings.Default.Votes.Count > 0)
            {
                tx.Attributes = tx.Attributes.Concat(Settings.Default.Votes.OfType<string>().Select(p => new TransactionAttribute
                {
                    Usage = TransactionAttributeUsage.Vote,
                    Data = UInt256.Parse(p).ToArray()
                })).ToArray();
            }
            SignatureContext context;
            try
            {
                context = new SignatureContext(tx);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show(Strings.UnsynchronizedBlock);
                return;
            }
            Program.CurrentWallet.Sign(context);
            if (context.Completed)
            {
                context.Signable.Scripts = context.GetScripts();
                Program.CurrentWallet.SaveTransaction(tx);
                Program.LocalNode.Relay(tx);
                InformationBox.Show(tx.Hash.ToString(), Strings.SendTxSucceedMessage, Strings.SendTxSucceedTitle);
            }
            else
            {
                InformationBox.Show(context.ToString(), Strings.IncompletedSignatureMessage, Strings.IncompletedSignatureTitle);
            }
        }
    }
}
