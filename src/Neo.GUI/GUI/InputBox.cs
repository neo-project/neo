// Copyright (C) 2015-2024 The Neo Project.
//
// InputBox.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Windows.Forms;

namespace Neo.GUI
{
    internal partial class InputBox : Form
    {
        private InputBox(string text, string caption, string content)
        {
            InitializeComponent();
            Text = caption;
            groupBox1.Text = text;
            textBox1.Text = content;
        }

        public static string Show(string text, string caption, string content = "")
        {
            using InputBox dialog = new InputBox(text, caption, content);
            if (dialog.ShowDialog() != DialogResult.OK) return null;
            return dialog.textBox1.Text;
        }
    }
}
