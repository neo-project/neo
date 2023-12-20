// Copyright (C) 2015-2024 The Neo Project.
//
// TransactionWrapper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;

namespace Neo.GUI.Wrappers
{
    internal class TransactionWrapper
    {
        [Category("Basic")]
        public byte Version { get; set; }
        [Category("Basic")]
        public uint Nonce { get; set; }
        [Category("Basic")]
        public List<SignerWrapper> Signers { get; set; }
        [Category("Basic")]
        public long SystemFee { get; set; }
        [Category("Basic")]
        public long NetworkFee { get; set; }
        [Category("Basic")]
        public uint ValidUntilBlock { get; set; }
        [Category("Basic")]
        public List<TransactionAttributeWrapper> Attributes { get; set; } = new List<TransactionAttributeWrapper>();
        [Category("Basic")]
        [Editor(typeof(ScriptEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(HexConverter))]
        public byte[] Script { get; set; }
        [Category("Basic")]
        public List<WitnessWrapper> Witnesses { get; set; } = new List<WitnessWrapper>();

        public Transaction Unwrap()
        {
            return new Transaction
            {
                Version = Version,
                Nonce = Nonce,
                Signers = Signers.Select(p => p.Unwrap()).ToArray(),
                SystemFee = SystemFee,
                NetworkFee = NetworkFee,
                ValidUntilBlock = ValidUntilBlock,
                Attributes = Attributes.Select(p => p.Unwrap()).ToArray(),
                Script = Script,
                Witnesses = Witnesses.Select(p => p.Unwrap()).ToArray()
            };
        }
    }
}
