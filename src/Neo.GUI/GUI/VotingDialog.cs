// Copyright (C) 2015-2024 The Neo Project.
//
// VotingDialog.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System.Linq;
using System.Windows.Forms;

namespace Neo.GUI
{
    internal partial class VotingDialog : Form
    {
        private readonly UInt160 script_hash;

        public byte[] GetScript()
        {
            ECPoint[] pubkeys = textBox1.Lines.Select(p => ECPoint.Parse(p, ECCurve.Secp256r1)).ToArray();
            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitDynamicCall(NativeContract.NEO.Hash, "vote", new ContractParameter
            {
                Type = ContractParameterType.Hash160,
                Value = script_hash
            }, new ContractParameter
            {
                Type = ContractParameterType.Array,
                Value = pubkeys.Select(p => new ContractParameter
                {
                    Type = ContractParameterType.PublicKey,
                    Value = p
                }).ToArray()
            });
            return sb.ToArray();
        }

        public VotingDialog(UInt160 script_hash)
        {
            InitializeComponent();
            this.script_hash = script_hash;
            label1.Text = script_hash.ToAddress(Program.Service.NeoSystem.Settings.AddressVersion);
        }
    }
}
