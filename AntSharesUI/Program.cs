using AntShares.Core;
using AntShares.Implementations.Blockchains.LevelDB;
using AntShares.Implementations.Wallets.EntityFramework;
using AntShares.Network;
using AntShares.Properties;
using AntShares.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace AntShares
{
    internal static class Program
    {
        public static LocalNode LocalNode;
        public static UserWallet CurrentWallet;
        public static MainForm MainForm;

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
                using (UpdateDialog dialog = new UpdateDialog { LatestVersion = latest })
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

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if DEBUG
            Exception ex = (Exception)e.ExceptionObject;
            File.WriteAllText("error.log", $"{ex.Message}\r\n{ex.StackTrace}");
#endif
        }

        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!CheckVersion()) return;
            using (Blockchain.RegisterBlockchain(new LevelDBBlockchain(Settings.Default.DataDirectoryPath)))
            using (LocalNode = new LocalNode())
            {
                LocalNode.UpnpEnabled = true;
                Application.Run(MainForm = new MainForm());
            }
        }
    }
}
