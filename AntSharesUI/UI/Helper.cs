using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AntShares.UI
{
    internal static class Helper
    {
        private static Dictionary<Type, Form> tool_forms = new Dictionary<Type, Form>();

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

        private static void Helper_FormClosing(object sender, FormClosingEventArgs e)
        {
            tool_forms.Remove(sender.GetType());
        }
    }
}
