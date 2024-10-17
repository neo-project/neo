// Copyright (C) 2015-2024 The Neo Project.
//
// CryptoLib.RIPEMD160.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.SmartContract;

namespace Neo.VM.Benchmark.NativeContract.CryptoLib;

public class CryptoLib_RIPEMD160 : Benchmark_Native
{

    protected override SmartContract.Native.NativeContract Native { get; }
    protected override string Method { get; }

    protected override object[][] Params
    {
        get
        {
            var random = new Random();
            return
            [
                [RandomBytes(1, random)],
                [RandomBytes(10, random)],
                [RandomBytes(100, random)],
                [RandomBytes(1000, random)],
                [RandomBytes(10000, random)],
                [RandomBytes(65535, random)],
                [RandomBytes(100000, random)],
                [RandomBytes(500000, random)],
                [RandomBytes(1000000, random)],
                [RandomBytes(ushort.MaxValue * 2, random)]
            ];
        }
    }

    private object RandomBytes(int length, Random random)
    {
        byte[] buffer = new byte[length];
        random.NextBytes(buffer);
        return buffer;
    }

    //     public static byte[] RIPEMD160(byte[] data)
    //     {
    //         return data.RIPEMD160();
    //     }

    // methodConvert.CallContractMethod(NativeContract.StdLib.Hash, "itoa", 1, true);
    // Loop start. Prepare arguments and call CryptoLib's verifyWithECDsa.
    // vrf.EmitPush((byte)NamedCurveHash.secp256k1Keccak256); // push Koblitz curve identifier and Keccak256 hasher.
    // vrf.Emit(OpCode.LDLOC0,                // load signatures.
    //     OpCode.LDLOC3,             // load sigCnt.
    //     OpCode.PICKITEM,           // pick signature at index sigCnt.
    //     OpCode.LDLOC1,             // load pubs.
    //     OpCode.LDLOC4,             // load pubCnt.
    //     OpCode.PICKITEM,           // pick pub at index pubCnt.
    //     OpCode.LDLOC2,             // load msg.
    //     OpCode.PUSH4, OpCode.PACK); // pack 4 arguments for 'verifyWithECDsa' call.
    // EmitAppCallNoArgs(vrf, CryptoLib.CryptoLib.Hash, "verifyWithECDsa", CallFlags.None); // emit the call to 'verifyWithECDsa' itself.


}
