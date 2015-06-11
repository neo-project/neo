using AntShares.UI;
using AntShares.Wallets;
using System;
using System.Windows.Forms;

namespace AntShares
{
    internal static class Program
    {
        public static UserWallet CurrentWallet;

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
