// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Neo.GUI
{
    internal partial class UpdateDialog : Form
    {
        private readonly HttpClient http = new();
        private readonly string download_url;
        private string download_path;

        public UpdateDialog(XDocument xdoc)
        {
            InitializeComponent();
            Version latest = Version.Parse(xdoc.Element("update").Attribute("latest").Value);
            textBox1.Text = latest.ToString();
            XElement release = xdoc.Element("update").Elements("release").First(p => p.Attribute("version").Value == latest.ToString());
            textBox2.Text = release.Element("changes").Value.Replace("\n", Environment.NewLine);
            download_url = release.Attribute("file").Value;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://neo.org/");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(download_url);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            download_path = "update.zip";
            using (Stream responseStream = await http.GetStreamAsync(download_url))
            using (FileStream fileStream = new(download_path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await responseStream.CopyToAsync(fileStream);
            }
            DirectoryInfo di = new DirectoryInfo("update");
            if (di.Exists) di.Delete(true);
            di.Create();
            ZipFile.ExtractToDirectory(download_path, di.Name);
            FileSystemInfo[] fs = di.GetFileSystemInfos();
            if (fs.Length == 1 && fs[0] is DirectoryInfo directory)
            {
                directory.MoveTo("update2");
                di.Delete();
                Directory.Move("update2", di.Name);
            }
            File.WriteAllBytes("update.bat", Resources.UpdateBat);
            Close();
            if (Program.MainForm != null) Program.MainForm.Close();
            Process.Start("update.bat");
        }
    }
}
