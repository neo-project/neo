// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using Neo.CLI;
using Neo.GUI;
using Neo.SmartContract.Native;

namespace Neo
{
    static class Program
    {
        public static MainService Service = new MainService();
        public static MainForm MainForm;
        public static UInt160[] NEP5Watched = { NativeContract.NEO.Hash, NativeContract.GAS.Hash };

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            using FileStream fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None);
            using StreamWriter w = new StreamWriter(fs);
            if (e.ExceptionObject is Exception ex)
            {
                PrintErrorLogs(w, ex);
            }
            else
            {
                w.WriteLine(e.ExceptionObject.GetType());
                w.WriteLine(e.ExceptionObject);
            }
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            XDocument xdoc = null;
            try
            {
                xdoc = XDocument.Load("https://raw.githubusercontent.com/neo-project/neo-gui/master/update.xml");
            }
            catch { }
            if (xdoc != null)
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                Version minimum = Version.Parse(xdoc.Element("update").Attribute("minimum").Value);
                if (version < minimum)
                {
                    using UpdateDialog dialog = new UpdateDialog(xdoc);
                    dialog.ShowDialog();
                    return;
                }
            }
            Service.Start(args);
            Application.Run(MainForm = new MainForm(xdoc));
            Service.Stop();
        }

        private static void PrintErrorLogs(StreamWriter writer, Exception ex)
        {
            writer.WriteLine(ex.GetType());
            writer.WriteLine(ex.Message);
            writer.WriteLine(ex.StackTrace);
            if (ex is AggregateException ex2)
            {
                foreach (Exception inner in ex2.InnerExceptions)
                {
                    writer.WriteLine();
                    PrintErrorLogs(writer, inner);
                }
            }
            else if (ex.InnerException != null)
            {
                writer.WriteLine();
                PrintErrorLogs(writer, ex.InnerException);
            }
        }
    }
}
