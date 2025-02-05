// Copyright (C) 2015-2025 The Neo Project.
//
// ChangePasswordDialog.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Neo.GUI
{
    internal partial class ChangePasswordDialog : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string OldPassword
        {
            get
            {
                return textBox1.Text;
            }
            set
            {
                textBox1.Text = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string NewPassword
        {
            get
            {
                return textBox2.Text;
            }
            set
            {
                textBox2.Text = value;
                textBox3.Text = value;
            }
        }

        public ChangePasswordDialog()
        {
            InitializeComponent();
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = textBox1.TextLength > 0 && textBox2.TextLength > 0 && textBox3.Text == textBox2.Text;
        }
    }
}
