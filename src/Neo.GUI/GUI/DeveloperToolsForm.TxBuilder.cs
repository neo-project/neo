// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.GUI.Wrappers;
using Neo.SmartContract;
using System;

namespace Neo.GUI
{
    partial class DeveloperToolsForm
    {
        private void InitializeTxBuilder()
        {
            propertyGrid1.SelectedObject = new TransactionWrapper();
        }

        private void propertyGrid1_SelectedObjectsChanged(object sender, EventArgs e)
        {
            splitContainer1.Panel2.Enabled = propertyGrid1.SelectedObject != null;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            TransactionWrapper wrapper = (TransactionWrapper)propertyGrid1.SelectedObject;
            ContractParametersContext context = new ContractParametersContext(Program.Service.NeoSystem.StoreView, wrapper.Unwrap(), Program.Service.NeoSystem.Settings.Network);
            InformationBox.Show(context.ToString(), "ParametersContext", "ParametersContext");
        }
    }
}
