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
using System.Text;
using System.Windows.Forms;

namespace Neo.GUI
{
    internal class TextBoxWriter : TextWriter
    {
        private readonly TextBoxBase textBox;

        public override Encoding Encoding => Encoding.UTF8;

        public TextBoxWriter(TextBoxBase textBox)
        {
            this.textBox = textBox;
        }

        public override void Write(char value)
        {
            textBox.Invoke(new Action(() => { textBox.Text += value; }));
        }

        public override void Write(string value)
        {
            textBox.Invoke(new Action<string>(textBox.AppendText), value);
        }
    }
}
