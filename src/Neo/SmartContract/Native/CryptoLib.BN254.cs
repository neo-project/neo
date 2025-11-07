// Copyright (C) 2015-2025 The Neo Project.
//
// CryptoLib.BN254.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using System;

namespace Neo.SmartContract.Native
{
    partial class CryptoLib
    {
        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 19)]
        public static byte[] Bn254Add(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return BN254.Add(input);
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 19)]
        public static byte[] Bn254Mul(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return BN254.Mul(input);
        }

        [ContractMethod(Hardfork.HF_Gorgon, CpuFee = 1 << 21)]
        public static byte[] Bn254Pairing(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);

            return BN254.Pairing(input);
        }
    }
}
