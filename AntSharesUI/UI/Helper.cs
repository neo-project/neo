using AntShares.Core;
using System;
using System.Collections.Generic;
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
                MessageBox.Show("余额不足以支付系统费用。");
                return;
            }
            SignatureContext context = new SignatureContext(tx);
            Program.CurrentWallet.Sign(context);
            if (context.Completed)
            {
                context.Signable.Scripts = context.GetScripts();
                Program.LocalNode.Relay(tx);
                InformationBox.Show(tx.Hash.ToString(), "交易已发送，这是交易编号(TXID)：", "交易成功");
            }
            else
            {
                InformationBox.Show(context.ToString(), "交易构造完成，但没有足够的签名：", "签名不完整");
            }
        }
    }
}
