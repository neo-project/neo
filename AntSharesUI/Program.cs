using AntShares.Core;
using AntShares.Implementations.Blockchains.LevelDB;
using AntShares.Implementations.Wallets.EntityFramework;
using AntShares.Network;
using AntShares.Properties;
using AntShares.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
            using (FileStream fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter w = new StreamWriter(fs))
            {
                w.WriteLine(ex.Message);
                w.WriteLine(ex.StackTrace);
                AggregateException ex2 = ex as AggregateException;
                if (ex2 != null)
                {
                    foreach (Exception inner in ex2.InnerExceptions)
                    {
                        w.WriteLine();
                        w.WriteLine(inner.Message);
                        w.WriteLine(inner.StackTrace);
                    }
                }
            }
#endif
        }

        private static bool InstallCertificate()
        {
            using (X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            using (X509Certificate2 cert = new X509Certificate2(Resources.OnchainCertificate))
            {
                store.Open(OpenFlags.ReadOnly);
                if (store.Certificates.Contains(cert)) return true;
            }
            using (X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            using (X509Certificate2 cert = new X509Certificate2(Resources.OnchainCertificate))
            {
                try
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(cert);
                    return true;
                }
                catch (CryptographicException)
                {
                    if (MessageBox.Show("小蚁需要安装Onchain的根证书才能对区块链上的资产进行认证，是否现在就安装证书？", "安装证书", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes) return true;
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = Environment.CurrentDirectory
                    });
                    return false;
                }
            }
        }

        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!CheckVersion()) return;
            if (!InstallCertificate()) return;
            using (Blockchain.RegisterBlockchain(new LevelDBBlockchain(Settings.Default.DataDirectoryPath)))
            using (LocalNode = new LocalNode())
            {
                LocalNode.UpnpEnabled = true;
                Application.Run(MainForm = new MainForm());
            }
        }
    }
}
