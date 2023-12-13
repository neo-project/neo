// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Windows.Forms;

namespace Neo.GUI
{
    internal partial class InformationBox : Form
    {
        public InformationBox()
        {
            InitializeComponent();
        }

        public static DialogResult Show(string text, string message = null, string title = null)
        {
            using InformationBox box = new InformationBox();
            box.textBox1.Text = text;
            if (message != null)
            {
                box.label1.Text = message;
            }
            if (title != null)
            {
                box.Text = title;
            }
            return box.ShowDialog();
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            textBox1.SelectAll();
            textBox1.Copy();
        }
    }
}
