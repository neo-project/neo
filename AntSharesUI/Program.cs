using AntShares.Core;
using AntShares.Implementations.Blockchains.LevelDB;
using AntShares.Implementations.Wallets.EntityFramework;
using AntShares.Network;
using AntShares.Properties;
using AntShares.UI;
using System;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace AntShares
{
    internal static class Program
    {
        public static LocalNode LocalNode;
        public static UserWallet CurrentWallet;

        private static bool CheckVersion()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("https://www.antshares.com/client/version.xml");
                Version minimum = Version.Parse(doc.GetElementsByTagName("version")[0].Attributes["minimum"].Value);
                Version latest = Version.Parse(doc.GetElementsByTagName("version")[0].Attributes["latest"].Value);
                Version self = Assembly.GetExecutingAssembly().GetName().Version;
                if (self >= latest) return true;
                using (UpdateDialog dialog = new UpdateDialog())
                {
                    dialog.ShowDialog();
                }
                return self >= minimum;
            }
            catch
            {
                return true;
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            WindowsFormsSynchronizationContext.AutoInstall = false;
            if (!CheckVersion()) return;
            using (Blockchain.RegisterBlockchain(new LevelDBBlockchain(Settings.Default.DataDirectoryPath)))
            using (LocalNode = new LocalNode())
            {
                Application.Run(new MainForm());
            }
        }
    }
}
