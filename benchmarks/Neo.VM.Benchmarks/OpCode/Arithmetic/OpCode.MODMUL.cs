// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.MODMUL.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Extensions;
using System.Numerics;

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_MODMUL
{
    [ParamsSource(nameof(ScriptParams))]
    public Script _script = new("0c04ffffff7f0c0100b8".HexToBytes());
    private BenchmarkEngine _engine;

    public static IEnumerable<Script> ScriptParams()
    {
        string[] scripts = [
"05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008011a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008002ffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0500000000000000000000000000000000000000000000000000000000000000800200000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080030000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f11050000000000000000000000000000000000000000000000000000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f1102ffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f110200000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f1103ffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f11030000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f1104ffffffffffffffffffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f10050000000000000000000000000000000000000000000000000000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f1011a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f1002ffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f100200000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f1003ffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f10030000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f1004ffffffffffffffffffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7f11a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7f0200000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7f03ffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7f030000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f020000008011a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f020000008002ffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f020000008003ffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080030000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f020000008004ffffffffffffffffffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7f11a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7f02ffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7f0200000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7f030000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03000000000000008011a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03000000000000008002ffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0300000000000000800200000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03000000000000008003ffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7f11a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7f0200000080a5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7f030000000000000080a5",
            "05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f11a5",
            "05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080a5",
            "05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080a5",
            "05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "0500000000000000000000000000000000000000000000000000000000000000801105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "0500000000000000000000000000000000000000000000000000000000000000801102ffffff7fa5",
            "050000000000000000000000000000000000000000000000000000000000000080110200000080a5",
            "0500000000000000000000000000000000000000000000000000000000000000801103ffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008011030000000000000080a5",
            "0500000000000000000000000000000000000000000000000000000000000000801104ffffffffffffffffffffffffffffff7fa5",
            "0500000000000000000000000000000000000000000000000000000000000000801005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "0500000000000000000000000000000000000000000000000000000000000000801011a5",
            "0500000000000000000000000000000000000000000000000000000000000000801002ffffff7fa5",
            "050000000000000000000000000000000000000000000000000000000000000080100200000080a5",
            "0500000000000000000000000000000000000000000000000000000000000000801003ffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008010030000000000000080a5",
            "0500000000000000000000000000000000000000000000000000000000000000801004ffffffffffffffffffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008002ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008002ffffff7f11a5",
            "05000000000000000000000000000000000000000000000000000000000000008002ffffff7f0200000080a5",
            "05000000000000000000000000000000000000000000000000000000000000008002ffffff7f03ffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008002ffffff7f030000000000000080a5",
            "05000000000000000000000000000000000000000000000000000000000000008002ffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "050000000000000000000000000000000000000000000000000000000000000080020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "050000000000000000000000000000000000000000000000000000000000000080020000008011a5",
            "050000000000000000000000000000000000000000000000000000000000000080020000008002ffffff7fa5",
            "050000000000000000000000000000000000000000000000000000000000000080020000008003ffffffffffffff7fa5",
            "0500000000000000000000000000000000000000000000000000000000000000800200000080030000000000000080a5",
            "050000000000000000000000000000000000000000000000000000000000000080020000008004ffffffffffffffffffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7f11a5",
            "05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7f02ffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7f0200000080a5",
            "05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7f030000000000000080a5",
            "05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008003000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008003000000000000008011a5",
            "05000000000000000000000000000000000000000000000000000000000000008003000000000000008002ffffff7fa5",
            "0500000000000000000000000000000000000000000000000000000000000000800300000000000000800200000080a5",
            "05000000000000000000000000000000000000000000000000000000000000008003000000000000008003ffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008003000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7f11a5",
            "05000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7f0200000080a5",
            "05000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "05000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7f030000000000000080a5",
            "1105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "1105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "1105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080a5",
            "1105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "1105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080a5",
            "1105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "1105000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "1105000000000000000000000000000000000000000000000000000000000000008002ffffff7fa5",
            "110500000000000000000000000000000000000000000000000000000000000000800200000080a5",
            "1105000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7fa5",
            "11050000000000000000000000000000000000000000000000000000000000000080030000000000000080a5",
            "1105000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "111005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "1110050000000000000000000000000000000000000000000000000000000000000080a5",
            "111002ffffff7fa5",
            "11100200000080a5",
            "111003ffffffffffffff7fa5",
            "1110030000000000000080a5",
            "111004ffffffffffffffffffffffffffffff7fa5",
            "1102ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "1102ffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "1102ffffff7f0200000080a5",
            "1102ffffff7f03ffffffffffffff7fa5",
            "1102ffffff7f030000000000000080a5",
            "1102ffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "11020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "110200000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "11020000008002ffffff7fa5",
            "11020000008003ffffffffffffff7fa5",
            "110200000080030000000000000080a5",
            "11020000008004ffffffffffffffffffffffffffffff7fa5",
            "1103ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "1103ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "1103ffffffffffffff7f02ffffff7fa5",
            "1103ffffffffffffff7f0200000080a5",
            "1103ffffffffffffff7f030000000000000080a5",
            "1103ffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "1103000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "11030000000000000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "1103000000000000008002ffffff7fa5",
            "110300000000000000800200000080a5",
            "1103000000000000008003ffffffffffffff7fa5",
            "1103000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "1104ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "1104ffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "1104ffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "1104ffffffffffffffffffffffffffffff7f0200000080a5",
            "1104ffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "1104ffffffffffffffffffffffffffffff7f030000000000000080a5",
            "1005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "1005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f11a5",
            "1005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "1005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080a5",
            "1005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "1005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080a5",
            "1005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "1005000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "1005000000000000000000000000000000000000000000000000000000000000008011a5",
            "1005000000000000000000000000000000000000000000000000000000000000008002ffffff7fa5",
            "100500000000000000000000000000000000000000000000000000000000000000800200000080a5",
            "1005000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7fa5",
            "10050000000000000000000000000000000000000000000000000000000000000080030000000000000080a5",
            "1005000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "101105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "1011050000000000000000000000000000000000000000000000000000000000000080a5",
            "101102ffffff7fa5",
            "10110200000080a5",
            "101103ffffffffffffff7fa5",
            "1011030000000000000080a5",
            "101104ffffffffffffffffffffffffffffff7fa5",
            "1002ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "1002ffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "1002ffffff7f11a5",
            "1002ffffff7f0200000080a5",
            "1002ffffff7f03ffffffffffffff7fa5",
            "1002ffffff7f030000000000000080a5",
            "1002ffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "10020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "100200000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "10020000008011a5",
            "10020000008002ffffff7fa5",
            "10020000008003ffffffffffffff7fa5",
            "100200000080030000000000000080a5",
            "10020000008004ffffffffffffffffffffffffffffff7fa5",
            "1003ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "1003ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "1003ffffffffffffff7f11a5",
            "1003ffffffffffffff7f02ffffff7fa5",
            "1003ffffffffffffff7f0200000080a5",
            "1003ffffffffffffff7f030000000000000080a5",
            "1003ffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "1003000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "10030000000000000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "1003000000000000008011a5",
            "1003000000000000008002ffffff7fa5",
            "100300000000000000800200000080a5",
            "1003000000000000008003ffffffffffffff7fa5",
            "1003000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "1004ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "1004ffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "1004ffffffffffffffffffffffffffffff7f11a5",
            "1004ffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "1004ffffffffffffffffffffffffffffff7f0200000080a5",
            "1004ffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "1004ffffffffffffffffffffffffffffff7f030000000000000080a5",
            "02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f11a5",
            "02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080a5",
            "02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080a5",
            "02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f05000000000000000000000000000000000000000000000000000000000000008011a5",
            "02ffffff7f0500000000000000000000000000000000000000000000000000000000000000800200000080a5",
            "02ffffff7f05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7fa5",
            "02ffffff7f050000000000000000000000000000000000000000000000000000000000000080030000000000000080a5",
            "02ffffff7f05000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f1105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f11050000000000000000000000000000000000000000000000000000000000000080a5",
            "02ffffff7f110200000080a5",
            "02ffffff7f1103ffffffffffffff7fa5",
            "02ffffff7f11030000000000000080a5",
            "02ffffff7f1104ffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f1005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f10050000000000000000000000000000000000000000000000000000000000000080a5",
            "02ffffff7f1011a5",
            "02ffffff7f100200000080a5",
            "02ffffff7f1003ffffffffffffff7fa5",
            "02ffffff7f10030000000000000080a5",
            "02ffffff7f1004ffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f0200000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "02ffffff7f020000008011a5",
            "02ffffff7f020000008003ffffffffffffff7fa5",
            "02ffffff7f0200000080030000000000000080a5",
            "02ffffff7f020000008004ffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f03ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "02ffffff7f03ffffffffffffff7f11a5",
            "02ffffff7f03ffffffffffffff7f0200000080a5",
            "02ffffff7f03ffffffffffffff7f030000000000000080a5",
            "02ffffff7f03ffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f030000000000000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "02ffffff7f03000000000000008011a5",
            "02ffffff7f0300000000000000800200000080a5",
            "02ffffff7f03000000000000008003ffffffffffffff7fa5",
            "02ffffff7f03000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f04ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "02ffffff7f04ffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "02ffffff7f04ffffffffffffffffffffffffffffff7f11a5",
            "02ffffff7f04ffffffffffffffffffffffffffffff7f0200000080a5",
            "02ffffff7f04ffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "02ffffff7f04ffffffffffffffffffffffffffffff7f030000000000000080a5",
            "020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f11a5",
            "020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080a5",
            "020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "020000008005000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "020000008005000000000000000000000000000000000000000000000000000000000000008011a5",
            "020000008005000000000000000000000000000000000000000000000000000000000000008002ffffff7fa5",
            "020000008005000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7fa5",
            "0200000080050000000000000000000000000000000000000000000000000000000000000080030000000000000080a5",
            "020000008005000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "02000000801105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "020000008011050000000000000000000000000000000000000000000000000000000000000080a5",
            "02000000801102ffffff7fa5",
            "02000000801103ffffffffffffff7fa5",
            "020000008011030000000000000080a5",
            "02000000801104ffffffffffffffffffffffffffffff7fa5",
            "02000000801005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "020000008010050000000000000000000000000000000000000000000000000000000000000080a5",
            "02000000801011a5",
            "02000000801002ffffff7fa5",
            "02000000801003ffffffffffffff7fa5",
            "020000008010030000000000000080a5",
            "02000000801004ffffffffffffffffffffffffffffff7fa5",
            "020000008002ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "020000008002ffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "020000008002ffffff7f11a5",
            "020000008002ffffff7f03ffffffffffffff7fa5",
            "020000008002ffffff7f030000000000000080a5",
            "020000008002ffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "020000008003ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "020000008003ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "020000008003ffffffffffffff7f11a5",
            "020000008003ffffffffffffff7f02ffffff7fa5",
            "020000008003ffffffffffffff7f030000000000000080a5",
            "020000008003ffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "020000008003000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "0200000080030000000000000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "020000008003000000000000008011a5",
            "020000008003000000000000008002ffffff7fa5",
            "020000008003000000000000008003ffffffffffffff7fa5",
            "020000008003000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "020000008004ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "020000008004ffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "020000008004ffffffffffffffffffffffffffffff7f11a5",
            "020000008004ffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "020000008004ffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "020000008004ffffffffffffffffffffffffffffff7f030000000000000080a5",
            "03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f11a5",
            "03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080a5",
            "03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080a5",
            "03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008011a5",
            "03ffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008002ffffff7fa5",
            "03ffffffffffffff7f0500000000000000000000000000000000000000000000000000000000000000800200000080a5",
            "03ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080030000000000000080a5",
            "03ffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f1105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f11050000000000000000000000000000000000000000000000000000000000000080a5",
            "03ffffffffffffff7f1102ffffff7fa5",
            "03ffffffffffffff7f110200000080a5",
            "03ffffffffffffff7f11030000000000000080a5",
            "03ffffffffffffff7f1104ffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f1005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f10050000000000000000000000000000000000000000000000000000000000000080a5",
            "03ffffffffffffff7f1011a5",
            "03ffffffffffffff7f1002ffffff7fa5",
            "03ffffffffffffff7f100200000080a5",
            "03ffffffffffffff7f10030000000000000080a5",
            "03ffffffffffffff7f1004ffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f02ffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "03ffffffffffffff7f02ffffff7f11a5",
            "03ffffffffffffff7f02ffffff7f0200000080a5",
            "03ffffffffffffff7f02ffffff7f030000000000000080a5",
            "03ffffffffffffff7f02ffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f0200000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "03ffffffffffffff7f020000008011a5",
            "03ffffffffffffff7f020000008002ffffff7fa5",
            "03ffffffffffffff7f0200000080030000000000000080a5",
            "03ffffffffffffff7f020000008004ffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f030000000000000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "03ffffffffffffff7f03000000000000008011a5",
            "03ffffffffffffff7f03000000000000008002ffffff7fa5",
            "03ffffffffffffff7f0300000000000000800200000080a5",
            "03ffffffffffffff7f03000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f04ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03ffffffffffffff7f04ffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "03ffffffffffffff7f04ffffffffffffffffffffffffffffff7f11a5",
            "03ffffffffffffff7f04ffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "03ffffffffffffff7f04ffffffffffffffffffffffffffffff7f0200000080a5",
            "03ffffffffffffff7f04ffffffffffffffffffffffffffffff7f030000000000000080a5",
            "03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f11a5",
            "03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080a5",
            "03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "03000000000000008005000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03000000000000008005000000000000000000000000000000000000000000000000000000000000008011a5",
            "03000000000000008005000000000000000000000000000000000000000000000000000000000000008002ffffff7fa5",
            "0300000000000000800500000000000000000000000000000000000000000000000000000000000000800200000080a5",
            "03000000000000008005000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7fa5",
            "03000000000000008005000000000000000000000000000000000000000000000000000000000000008004ffffffffffffffffffffffffffffff7fa5",
            "0300000000000000801105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03000000000000008011050000000000000000000000000000000000000000000000000000000000000080a5",
            "0300000000000000801102ffffff7fa5",
            "030000000000000080110200000080a5",
            "0300000000000000801103ffffffffffffff7fa5",
            "0300000000000000801104ffffffffffffffffffffffffffffff7fa5",
            "0300000000000000801005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03000000000000008010050000000000000000000000000000000000000000000000000000000000000080a5",
            "0300000000000000801011a5",
            "0300000000000000801002ffffff7fa5",
            "030000000000000080100200000080a5",
            "0300000000000000801003ffffffffffffff7fa5",
            "0300000000000000801004ffffffffffffffffffffffffffffff7fa5",
            "03000000000000008002ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03000000000000008002ffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "03000000000000008002ffffff7f11a5",
            "03000000000000008002ffffff7f0200000080a5",
            "03000000000000008002ffffff7f03ffffffffffffff7fa5",
            "03000000000000008002ffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "030000000000000080020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "0300000000000000800200000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "030000000000000080020000008011a5",
            "030000000000000080020000008002ffffff7fa5",
            "030000000000000080020000008003ffffffffffffff7fa5",
            "030000000000000080020000008004ffffffffffffffffffffffffffffff7fa5",
            "03000000000000008003ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03000000000000008003ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "03000000000000008003ffffffffffffff7f11a5",
            "03000000000000008003ffffffffffffff7f02ffffff7fa5",
            "03000000000000008003ffffffffffffff7f0200000080a5",
            "03000000000000008003ffffffffffffff7f04ffffffffffffffffffffffffffffff7fa5",
            "03000000000000008004ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "03000000000000008004ffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "03000000000000008004ffffffffffffffffffffffffffffff7f11a5",
            "03000000000000008004ffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "03000000000000008004ffffffffffffffffffffffffffffff7f0200000080a5",
            "03000000000000008004ffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f11a5",
            "04ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080a5",
            "04ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008011a5",
            "04ffffffffffffffffffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008002ffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f0500000000000000000000000000000000000000000000000000000000000000800200000080a5",
            "04ffffffffffffffffffffffffffffff7f05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080030000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f1105ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f11050000000000000000000000000000000000000000000000000000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f1102ffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f110200000080a5",
            "04ffffffffffffffffffffffffffffff7f1103ffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f11030000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f1005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f10050000000000000000000000000000000000000000000000000000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f1011a5",
            "04ffffffffffffffffffffffffffffff7f1002ffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f100200000080a5",
            "04ffffffffffffffffffffffffffffff7f1003ffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f10030000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f02ffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f02ffffff7f11a5",
            "04ffffffffffffffffffffffffffffff7f02ffffff7f0200000080a5",
            "04ffffffffffffffffffffffffffffff7f02ffffff7f03ffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f02ffffff7f030000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f0200000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f020000008011a5",
            "04ffffffffffffffffffffffffffffff7f020000008002ffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f020000008003ffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f0200000080030000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f03ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f03ffffffffffffff7f11a5",
            "04ffffffffffffffffffffffffffffff7f03ffffffffffffff7f02ffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f03ffffffffffffff7f0200000080a5",
            "04ffffffffffffffffffffffffffffff7f03ffffffffffffff7f030000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f030000000000000080050000000000000000000000000000000000000000000000000000000000000080a5",
            "04ffffffffffffffffffffffffffffff7f03000000000000008011a5",
            "04ffffffffffffffffffffffffffffff7f03000000000000008002ffffff7fa5",
            "04ffffffffffffffffffffffffffffff7f0300000000000000800200000080a5",
            "04ffffffffffffffffffffffffffffff7f03000000000000008003ffffffffffffff7fa5",


        ];

        return scripts.Select(p => new Script(p.HexToBytes()));
    }

    [IterationSetup]
    public void Setup()
    {
        _engine = new BenchmarkEngine();
        _engine.LoadScript(_script);
        _engine.ExecuteUntil(VM.OpCode.MODMUL);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public void Bench()
    {
        _engine.ExecuteNext();

    }

    void CreateBenchScript(BigInteger a, BigInteger b, BigInteger m, VM.OpCode opcode)
    {
        var builder = new InstructionBuilder();

        builder.Push(a);
        builder.Push(b);
        builder.Push(m);
        builder.AddInstruction(opcode);

        Console.WriteLine($"\"{builder.ToArray().ToHexString()}\",");
    }
}


// | Method | _script       | Mean     | Error     | StdDev    | Median   |
// |------- |-------------- |---------:|----------:|----------:|---------:|
// | Bench  | Neo.VM.Script | 2.656 us | 0.0613 us | 0.1626 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.502 us | 0.1919 us | 0.5381 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.709 us | 0.0574 us | 0.1503 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.187 us | 0.1312 us | 0.3502 us | 3.100 us |
// | Bench  | Neo.VM.Script | 3.166 us | 0.1324 us | 0.3535 us | 3.100 us |
// | Bench  | Neo.VM.Script | 3.074 us | 0.1357 us | 0.3644 us | 3.000 us |
// | Bench  | Neo.VM.Script | 3.153 us | 0.1687 us | 0.4591 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.724 us | 0.1193 us | 0.3226 us | 2.650 us |
// | Bench  | Neo.VM.Script | 2.490 us | 0.1036 us | 0.2837 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.756 us | 0.1060 us | 0.2920 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.720 us | 0.0924 us | 0.2483 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.970 us | 0.1358 us | 0.3739 us | 2.850 us |
// | Bench  | Neo.VM.Script | 3.175 us | 0.1349 us | 0.3623 us | 3.100 us |
// | Bench  | Neo.VM.Script | 2.879 us | 0.1181 us | 0.3193 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.947 us | 0.1015 us | 0.2813 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.248 us | 0.0521 us | 0.1391 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.291 us | 0.0813 us | 0.2156 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.275 us | 0.0813 us | 0.2184 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.284 us | 0.0682 us | 0.1844 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.402 us | 0.1375 us | 0.3716 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.731 us | 0.0617 us | 0.1657 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.429 us | 0.0431 us | 0.0991 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.894 us | 0.0932 us | 0.2566 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.851 us | 0.0679 us | 0.1800 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.859 us | 0.0806 us | 0.2208 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.204 us | 0.0477 us | 0.1026 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.856 us | 0.1060 us | 0.2884 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.646 us | 0.0981 us | 0.2653 us | 2.500 us |
// | Bench  | Neo.VM.Script | 3.219 us | 0.1180 us | 0.3251 us | 3.100 us |
// | Bench  | Neo.VM.Script | 2.950 us | 0.0619 us | 0.1529 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.027 us | 0.0948 us | 0.2548 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.894 us | 0.0641 us | 0.1745 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.147 us | 0.1489 us | 0.4076 us | 2.950 us |
// | Bench  | Neo.VM.Script | 2.674 us | 0.0934 us | 0.2508 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.729 us | 0.0878 us | 0.2328 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.989 us | 0.0685 us | 0.1828 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.983 us | 0.0627 us | 0.1294 us | 3.000 us |
// | Bench  | Neo.VM.Script | 3.141 us | 0.1311 us | 0.3543 us | 3.000 us |
// | Bench  | Neo.VM.Script | 3.770 us | 0.3092 us | 0.9116 us | 3.400 us |
// | Bench  | Neo.VM.Script | 2.802 us | 0.1397 us | 0.3848 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.010 us | 0.1297 us | 0.3528 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.797 us | 0.0890 us | 0.2437 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.727 us | 0.0779 us | 0.2078 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.734 us | 0.3262 us | 0.9515 us | 3.350 us |
// | Bench  | Neo.VM.Script | 2.900 us | 0.0499 us | 0.0926 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.275 us | 0.3243 us | 0.9561 us | 2.750 us |
// | Bench  | Neo.VM.Script | 2.795 us | 0.1635 us | 0.4503 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.484 us | 0.2709 us | 0.7684 us | 3.100 us |
// | Bench  | Neo.VM.Script | 3.079 us | 0.0967 us | 0.2646 us | 3.000 us |
// | Bench  | Neo.VM.Script | 3.270 us | 0.2042 us | 0.5658 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.737 us | 0.1074 us | 0.2923 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.718 us | 0.0820 us | 0.2216 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.605 us | 0.2976 us | 0.8775 us | 3.100 us |
// | Bench  | Neo.VM.Script | 3.182 us | 0.1515 us | 0.4173 us | 3.000 us |
// | Bench  | Neo.VM.Script | 1.856 us | 0.0349 us | 0.0835 us | 1.800 us |
// | Bench  | Neo.VM.Script | 3.217 us | 0.1338 us | 0.3664 us | 3.100 us |
// | Bench  | Neo.VM.Script | 3.638 us | 0.3193 us | 0.9416 us | 3.400 us |
// | Bench  | Neo.VM.Script | 3.506 us | 0.3240 us | 0.9552 us | 3.150 us |
// | Bench  | Neo.VM.Script | 2.736 us | 0.0987 us | 0.2735 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.976 us | 0.1442 us | 0.3899 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.791 us | 0.1255 us | 0.3393 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.710 us | 0.0522 us | 0.0928 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.310 us | 0.0760 us | 0.2016 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.278 us | 0.0709 us | 0.1917 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.175 us | 0.0455 us | 0.0447 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.595 us | 0.1899 us | 0.5386 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.080 us | 0.0443 us | 0.0414 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.169 us | 0.0474 us | 0.1171 us | 2.100 us |
// | Bench  | Neo.VM.Script | 3.187 us | 0.3346 us | 0.9864 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.274 us | 0.0960 us | 0.2613 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.702 us | 0.1292 us | 0.3471 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.801 us | 0.1052 us | 0.2809 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.823 us | 0.0924 us | 0.2514 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.802 us | 0.1300 us | 0.3560 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.879 us | 0.1118 us | 0.3002 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.931 us | 0.0859 us | 0.2262 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.355 us | 0.3332 us | 0.9823 us | 2.950 us |
// | Bench  | Neo.VM.Script | 2.629 us | 0.0563 us | 0.1502 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.052 us | 0.1428 us | 0.3811 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.941 us | 0.1265 us | 0.3442 us | 2.800 us |
// | Bench  | Neo.VM.Script | 3.012 us | 0.1128 us | 0.3069 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.600 us | 0.1050 us | 0.2786 us | 2.550 us |
// | Bench  | Neo.VM.Script | 2.750 us | 0.1030 us | 0.2837 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.651 us | 0.0950 us | 0.2535 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.681 us | 0.0739 us | 0.1997 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.940 us | 0.3097 us | 0.9132 us | 3.650 us |
// | Bench  | Neo.VM.Script | 3.215 us | 0.1946 us | 0.5261 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.922 us | 0.0940 us | 0.2509 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.569 us | 0.0487 us | 0.0479 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.179 us | 0.2765 us | 0.8108 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.895 us | 0.1411 us | 0.3741 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.968 us | 0.1051 us | 0.2895 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.853 us | 0.0608 us | 0.1569 us | 2.800 us |
// | Bench  | Neo.VM.Script | 3.108 us | 0.1290 us | 0.3465 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.792 us | 0.1460 us | 0.3998 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.701 us | 0.0844 us | 0.2310 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.223 us | 0.2127 us | 0.6035 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.024 us | 0.1016 us | 0.2765 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.942 us | 0.0706 us | 0.1896 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.579 us | 0.0549 us | 0.0787 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.417 us | 0.0518 us | 0.0759 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.809 us | 0.0574 us | 0.0893 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.775 us | 0.0587 us | 0.1418 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.780 us | 0.0592 us | 0.0887 us | 2.800 us |
// | Bench  | Neo.VM.Script | 3.438 us | 0.2988 us | 0.8811 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.637 us | 0.0564 us | 0.1262 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.540 us | 0.0930 us | 0.2531 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.720 us | 0.0939 us | 0.2539 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.884 us | 0.1132 us | 0.3079 us | 2.800 us |
// | Bench  | Neo.VM.Script | 3.085 us | 0.1446 us | 0.3981 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.821 us | 0.0585 us | 0.0927 us | 2.800 us |
// | Bench  | Neo.VM.Script | 1.962 us | 0.0574 us | 0.1513 us | 1.900 us |
// | Bench  | Neo.VM.Script | 2.236 us | 0.0635 us | 0.1728 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.634 us | 0.1476 us | 0.3939 us | 2.500 us |
// | Bench  | Neo.VM.Script | 1.858 us | 0.0407 us | 0.1036 us | 1.800 us |
// | Bench  | Neo.VM.Script | 1.835 us | 0.0403 us | 0.0876 us | 1.800 us |
// | Bench  | Neo.VM.Script | 1.925 us | 0.0640 us | 0.1685 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.881 us | 0.0416 us | 0.1109 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.918 us | 0.0468 us | 0.1313 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.926 us | 0.0419 us | 0.0724 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.881 us | 0.0410 us | 0.0403 us | 1.900 us |
// | Bench  | Neo.VM.Script | 2.047 us | 0.0540 us | 0.1522 us | 2.000 us |
// | Bench  | Neo.VM.Script | 1.959 us | 0.0427 us | 0.0725 us | 2.000 us |
// | Bench  | Neo.VM.Script | 2.002 us | 0.0778 us | 0.2144 us | 1.900 us |
// | Bench  | Neo.VM.Script | 3.374 us | 0.2921 us | 0.8614 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.459 us | 0.0866 us | 0.2342 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.568 us | 0.1048 us | 0.2816 us | 2.500 us |
// | Bench  | Neo.VM.Script | 3.158 us | 0.2878 us | 0.8440 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.479 us | 0.0819 us | 0.2201 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.429 us | 0.0716 us | 0.1924 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.389 us | 0.0515 us | 0.0875 us | 2.400 us |
// | Bench  | Neo.VM.Script | 3.144 us | 0.2878 us | 0.8487 us | 2.850 us |
// | Bench  | Neo.VM.Script | 2.580 us | 0.0947 us | 0.2576 us | 2.550 us |
// | Bench  | Neo.VM.Script | 2.643 us | 0.0553 us | 0.1078 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.628 us | 0.0976 us | 0.2639 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.432 us | 0.0617 us | 0.1659 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.433 us | 0.0576 us | 0.1547 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.661 us | 0.1661 us | 0.4491 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.417 us | 0.0522 us | 0.0857 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.693 us | 0.0601 us | 0.1624 us | 2.700 us |
// | Bench  | Neo.VM.Script | 1.821 us | 0.0397 us | 0.0641 us | 1.800 us |
// | Bench  | Neo.VM.Script | 3.155 us | 0.2969 us | 0.8754 us | 2.750 us |
// | Bench  | Neo.VM.Script | 2.535 us | 0.0869 us | 0.2349 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.642 us | 0.1286 us | 0.3454 us | 2.500 us |
// | Bench  | Neo.VM.Script | 3.004 us | 0.1970 us | 0.5524 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.739 us | 0.2885 us | 0.8507 us | 3.500 us |
// | Bench  | Neo.VM.Script | 2.844 us | 0.1112 us | 0.3082 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.180 us | 0.0459 us | 0.1209 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.412 us | 0.1131 us | 0.3133 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.530 us | 0.1699 us | 0.4679 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.372 us | 0.1220 us | 0.3297 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.323 us | 0.0893 us | 0.2491 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.832 us | 0.2551 us | 0.7320 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.203 us | 0.0470 us | 0.0758 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.200 us | 0.0707 us | 0.1960 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.323 us | 0.0957 us | 0.2553 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.264 us | 0.0852 us | 0.2303 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.092 us | 0.0370 us | 0.0289 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.255 us | 0.0597 us | 0.1615 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.205 us | 0.0479 us | 0.1042 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.239 us | 0.0485 us | 0.1234 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.366 us | 0.2187 us | 0.6448 us | 2.100 us |
// | Bench  | Neo.VM.Script | 1.909 us | 0.0481 us | 0.1307 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.851 us | 0.0408 us | 0.0692 us | 1.800 us |
// | Bench  | Neo.VM.Script | 1.792 us | 0.0370 us | 0.0289 us | 1.800 us |
// | Bench  | Neo.VM.Script | 2.144 us | 0.0465 us | 0.0838 us | 2.100 us |
// | Bench  | Neo.VM.Script | 1.960 us | 0.0513 us | 0.1370 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.910 us | 0.0551 us | 0.1462 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.914 us | 0.0454 us | 0.1226 us | 1.900 us |
// | Bench  | Neo.VM.Script | 2.616 us | 0.2345 us | 0.6876 us | 2.200 us |
// | Bench  | Neo.VM.Script | 1.941 us | 0.0486 us | 0.1323 us | 1.900 us |
// | Bench  | Neo.VM.Script | 2.388 us | 0.2354 us | 0.6940 us | 2.000 us |
// | Bench  | Neo.VM.Script | 1.945 us | 0.0779 us | 0.2107 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.913 us | 0.0607 us | 0.1701 us | 1.800 us |
// | Bench  | Neo.VM.Script | 1.899 us | 0.0449 us | 0.1222 us | 1.900 us |
// | Bench  | Neo.VM.Script | 2.216 us | 0.0473 us | 0.0888 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.275 us | 0.0931 us | 0.2516 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.213 us | 0.0583 us | 0.1585 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.862 us | 0.2492 us | 0.7347 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.737 us | 0.2452 us | 0.7191 us | 2.300 us |
// | Bench  | Neo.VM.Script | 1.884 us | 0.0582 us | 0.1572 us | 1.800 us |
// | Bench  | Neo.VM.Script | 2.189 us | 0.0478 us | 0.0685 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.260 us | 0.0794 us | 0.2160 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.241 us | 0.0644 us | 0.1751 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.213 us | 0.0404 us | 0.1079 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.410 us | 0.1685 us | 0.4641 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.192 us | 0.0382 us | 0.0862 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.105 us | 0.0443 us | 0.0510 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.262 us | 0.0825 us | 0.2245 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.185 us | 0.0395 us | 0.1020 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.192 us | 0.0475 us | 0.1208 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.187 us | 0.0476 us | 0.1149 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.245 us | 0.0712 us | 0.1924 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.113 us | 0.0461 us | 0.0900 us | 2.100 us |
// | Bench  | Neo.VM.Script | 1.893 us | 0.0532 us | 0.1421 us | 1.900 us |
// | Bench  | Neo.VM.Script | 2.184 us | 0.0618 us | 0.1649 us | 2.100 us |
// | Bench  | Neo.VM.Script | 1.884 us | 0.0407 us | 0.0754 us | 1.900 us |
// | Bench  | Neo.VM.Script | 3.011 us | 0.2046 us | 0.5600 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.225 us | 0.0472 us | 0.0840 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.156 us | 0.0436 us | 0.0705 us | 2.150 us |
// | Bench  | Neo.VM.Script | 2.171 us | 0.0472 us | 0.1218 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.275 us | 0.0815 us | 0.2232 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.291 us | 0.0793 us | 0.2076 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.625 us | 0.0555 us | 0.0639 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.469 us | 0.0528 us | 0.1323 us | 2.500 us |
// | Bench  | Neo.VM.Script | 3.352 us | 0.2968 us | 0.8656 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.888 us | 0.0615 us | 0.1214 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.860 us | 0.0969 us | 0.2636 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.121 us | 0.0452 us | 0.0588 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.971 us | 0.1401 us | 0.3813 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.138 us | 0.0465 us | 0.0815 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.745 us | 0.0583 us | 0.1005 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.790 us | 0.0591 us | 0.1416 us | 2.800 us |
// | Bench  | Neo.VM.Script | 3.546 us | 0.3644 us | 1.0688 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.765 us | 0.0591 us | 0.1051 us | 2.800 us |
// | Bench  | Neo.VM.Script | 1.881 us | 0.0410 us | 0.0403 us | 1.900 us |
// | Bench  | Neo.VM.Script | 2.071 us | 0.0689 us | 0.1791 us | 2.000 us |
// | Bench  | Neo.VM.Script | 1.992 us | 0.0460 us | 0.1260 us | 2.000 us |
// | Bench  | Neo.VM.Script | 1.962 us | 0.0441 us | 0.1216 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.930 us | 0.0426 us | 0.0925 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.928 us | 0.0425 us | 0.0858 us | 1.900 us |
// | Bench  | Neo.VM.Script | 2.606 us | 0.0778 us | 0.2118 us | 2.550 us |
// | Bench  | Neo.VM.Script | 1.897 us | 0.0418 us | 0.0917 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.951 us | 0.0429 us | 0.1161 us | 1.900 us |
// | Bench  | Neo.VM.Script | 2.540 us | 0.2526 us | 0.7448 us | 2.200 us |
// | Bench  | Neo.VM.Script | 1.925 us | 0.0425 us | 0.0950 us | 1.900 us |
// | Bench  | Neo.VM.Script | 1.901 us | 0.0550 us | 0.1469 us | 1.900 us |
// | Bench  | Neo.VM.Script | 2.572 us | 0.1218 us | 0.3251 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.492 us | 0.0878 us | 0.2417 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.452 us | 0.0743 us | 0.2010 us | 2.400 us |
// | Bench  | Neo.VM.Script | 3.187 us | 0.2777 us | 0.8189 us | 2.750 us |
// | Bench  | Neo.VM.Script | 3.451 us | 0.3107 us | 0.9161 us | 3.100 us |
// | Bench  | Neo.VM.Script | 2.577 us | 0.1539 us | 0.4134 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.376 us | 0.0513 us | 0.1343 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.581 us | 0.1378 us | 0.3677 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.507 us | 0.0908 us | 0.2423 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.802 us | 0.1230 us | 0.3304 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.819 us | 0.1120 us | 0.3141 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.513 us | 0.0805 us | 0.2107 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.592 us | 0.0946 us | 0.2443 us | 2.500 us |
// | Bench  | Neo.VM.Script | 3.176 us | 0.3288 us | 0.9694 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.476 us | 0.3363 us | 0.9916 us | 3.100 us |
// | Bench  | Neo.VM.Script | 2.634 us | 0.0527 us | 0.1200 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.691 us | 0.0619 us | 0.1673 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.394 us | 0.0514 us | 0.0801 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.467 us | 0.0791 us | 0.2166 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.397 us | 0.0515 us | 0.1410 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.521 us | 0.0914 us | 0.2455 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.862 us | 0.0558 us | 0.1164 us | 2.800 us |
// | Bench  | Neo.VM.Script | 3.352 us | 0.3121 us | 0.9201 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.402 us | 0.0461 us | 0.0911 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.473 us | 0.0518 us | 0.1308 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.573 us | 0.0549 us | 0.1388 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.518 us | 0.0515 us | 0.0529 us | 2.500 us |
// | Bench  | Neo.VM.Script | 3.462 us | 0.2939 us | 0.8433 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.940 us | 0.0546 us | 0.1438 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.078 us | 0.1349 us | 0.3693 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.888 us | 0.0571 us | 0.1503 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.648 us | 0.0971 us | 0.2658 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.797 us | 0.1176 us | 0.3279 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.921 us | 0.3680 us | 1.0849 us | 3.550 us |
// | Bench  | Neo.VM.Script | 2.778 us | 0.0590 us | 0.1402 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.890 us | 0.3414 us | 1.0067 us | 3.450 us |
// | Bench  | Neo.VM.Script | 2.547 us | 0.1118 us | 0.3061 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.707 us | 0.0589 us | 0.1581 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.124 us | 0.2961 us | 0.8401 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.537 us | 0.0917 us | 0.2542 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.432 us | 0.0525 us | 0.1364 us | 2.400 us |
// | Bench  | Neo.VM.Script | 3.037 us | 0.2799 us | 0.8252 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.461 us | 0.1846 us | 0.5084 us | 2.250 us |
// | Bench  | Neo.VM.Script | 2.311 us | 0.1061 us | 0.2940 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.844 us | 0.2772 us | 0.8131 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.474 us | 0.1505 us | 0.4196 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.297 us | 0.0885 us | 0.2437 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.176 us | 0.0466 us | 0.1135 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.179 us | 0.0473 us | 0.1221 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.375 us | 0.0455 us | 0.0809 us | 2.400 us |
// | Bench  | Neo.VM.Script | 3.225 us | 0.1957 us | 0.5257 us | 3.050 us |
// | Bench  | Neo.VM.Script | 2.466 us | 0.0728 us | 0.1967 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.721 us | 0.1128 us | 0.3068 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.284 us | 0.0926 us | 0.2457 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.459 us | 0.0829 us | 0.2256 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.794 us | 0.1584 us | 0.4364 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.695 us | 0.1276 us | 0.3429 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.567 us | 0.0804 us | 0.2132 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.624 us | 0.0466 us | 0.1168 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.133 us | 0.2026 us | 0.5713 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.626 us | 0.0937 us | 0.2518 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.552 us | 0.0720 us | 0.1921 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.698 us | 0.1247 us | 0.3434 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.655 us | 0.0801 us | 0.2221 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.697 us | 0.0755 us | 0.2055 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.982 us | 0.1129 us | 0.3109 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.450 us | 0.0526 us | 0.0516 us | 2.450 us |
// | Bench  | Neo.VM.Script | 2.611 us | 0.0561 us | 0.1220 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.268 us | 0.2991 us | 0.8820 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.562 us | 0.0539 us | 0.1301 us | 2.550 us |
// | Bench  | Neo.VM.Script | 2.590 us | 0.0486 us | 0.1200 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.002 us | 0.1186 us | 0.3207 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.669 us | 0.0487 us | 0.0479 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.949 us | 0.0902 us | 0.2453 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.744 us | 0.1327 us | 0.3677 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.851 us | 0.1547 us | 0.4209 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.412 us | 0.2462 us | 0.7143 us | 3.100 us |
// | Bench  | Neo.VM.Script | 3.618 us | 0.2569 us | 0.7493 us | 3.300 us |
// | Bench  | Neo.VM.Script | 3.128 us | 0.1107 us | 0.3030 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.580 us | 0.0555 us | 0.1461 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.584 us | 0.0550 us | 0.1033 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.705 us | 0.0499 us | 0.1095 us | 2.650 us |
// | Bench  | Neo.VM.Script | 2.886 us | 0.0597 us | 0.0875 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.117 us | 0.1336 us | 0.3679 us | 2.950 us |
// | Bench  | Neo.VM.Script | 3.006 us | 0.0568 us | 0.1487 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.577 us | 0.1677 us | 0.4592 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.453 us | 0.1107 us | 0.3013 us | 2.350 us |
// | Bench  | Neo.VM.Script | 2.531 us | 0.0728 us | 0.1944 us | 2.500 us |
// | Bench  | Neo.VM.Script | 3.448 us | 0.3196 us | 0.9423 us | 3.050 us |
// | Bench  | Neo.VM.Script | 2.622 us | 0.0821 us | 0.2192 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.329 us | 0.0432 us | 0.0710 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.845 us | 0.2345 us | 0.6914 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.131 us | 0.0410 us | 0.0403 us | 2.150 us |
// | Bench  | Neo.VM.Script | 3.693 us | 0.3065 us | 0.8988 us | 3.300 us |
// | Bench  | Neo.VM.Script | 2.296 us | 0.0843 us | 0.2335 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.299 us | 0.0888 us | 0.2371 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.211 us | 0.0447 us | 0.0934 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.297 us | 0.0989 us | 0.2709 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.969 us | 0.2432 us | 0.7057 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.348 us | 0.0503 us | 0.0738 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.454 us | 0.0526 us | 0.1377 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.708 us | 0.0580 us | 0.1509 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.719 us | 0.0638 us | 0.1703 us | 2.650 us |
// | Bench  | Neo.VM.Script | 2.515 us | 0.1096 us | 0.2962 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.538 us | 0.1054 us | 0.2832 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.545 us | 0.0839 us | 0.2284 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.589 us | 0.0529 us | 0.1318 us | 2.600 us |
// | Bench  | Neo.VM.Script | 1.963 us | 0.0667 us | 0.1722 us | 1.900 us |
// | Bench  | Neo.VM.Script | 3.235 us | 0.3015 us | 0.8844 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.583 us | 0.0874 us | 0.2408 us | 2.500 us |
// | Bench  | Neo.VM.Script | 3.578 us | 0.3178 us | 0.9271 us | 3.050 us |
// | Bench  | Neo.VM.Script | 2.655 us | 0.1041 us | 0.2814 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.618 us | 0.0760 us | 0.2043 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.759 us | 0.1328 us | 0.3590 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.950 us | 0.2288 us | 0.6453 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.844 us | 0.0662 us | 0.1834 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.729 us | 0.0581 us | 0.1448 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.495 us | 0.0533 us | 0.1181 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.764 us | 0.1146 us | 0.3117 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.758 us | 0.1058 us | 0.2825 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.703 us | 0.1126 us | 0.3082 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.895 us | 0.1390 us | 0.3782 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.106 us | 0.1768 us | 0.4840 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.487 us | 0.1009 us | 0.2675 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.705 us | 0.1090 us | 0.2984 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.709 us | 0.0582 us | 0.1584 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.069 us | 0.1393 us | 0.3837 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.267 us | 0.1836 us | 0.5087 us | 3.100 us |
// | Bench  | Neo.VM.Script | 3.007 us | 0.0695 us | 0.1890 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.858 us | 0.0622 us | 0.1714 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.706 us | 0.0974 us | 0.2618 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.685 us | 0.0727 us | 0.2003 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.969 us | 0.1456 us | 0.3937 us | 2.800 us |
// | Bench  | Neo.VM.Script | 3.192 us | 0.1950 us | 0.5434 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.848 us | 0.0605 us | 0.1528 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.933 us | 0.0767 us | 0.2072 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.547 us | 0.0803 us | 0.2213 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.644 us | 0.1098 us | 0.3007 us | 2.500 us |
// | Bench  | Neo.VM.Script | 3.061 us | 0.2912 us | 0.8586 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.694 us | 0.0894 us | 0.2417 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.546 us | 0.1034 us | 0.2779 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.151 us | 0.0473 us | 0.1278 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.285 us | 0.0949 us | 0.2583 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.304 us | 0.1161 us | 0.3118 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.262 us | 0.0788 us | 0.2077 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.455 us | 0.1519 us | 0.4210 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.292 us | 0.0849 us | 0.2295 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.326 us | 0.0955 us | 0.2583 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.368 us | 0.0711 us | 0.1922 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.672 us | 0.1089 us | 0.2906 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.465 us | 0.0860 us | 0.2310 us | 2.400 us |
// | Bench  | Neo.VM.Script | 3.153 us | 0.2934 us | 0.8650 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.670 us | 0.0613 us | 0.1636 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.331 us | 0.0487 us | 0.0479 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.729 us | 0.1741 us | 0.4738 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.563 us | 0.0643 us | 0.1749 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.444 us | 0.0522 us | 0.0512 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.682 us | 0.1029 us | 0.2746 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.883 us | 0.0966 us | 0.2628 us | 2.750 us |
// | Bench  | Neo.VM.Script | 2.655 us | 0.0947 us | 0.2608 us | 2.550 us |
// | Bench  | Neo.VM.Script | 2.578 us | 0.0984 us | 0.2676 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.562 us | 0.0680 us | 0.1839 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.761 us | 0.1190 us | 0.3258 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.597 us | 0.0540 us | 0.0809 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.660 us | 0.0575 us | 0.1545 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.769 us | 0.0588 us | 0.1030 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.555 us | 0.0858 us | 0.2305 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.499 us | 0.0501 us | 0.1329 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.660 us | 0.0999 us | 0.2817 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.795 us | 0.0598 us | 0.1586 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.999 us | 0.1264 us | 0.3459 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.143 us | 0.1734 us | 0.4746 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.667 us | 0.0675 us | 0.1812 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.712 us | 0.1016 us | 0.2730 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.136 us | 0.1273 us | 0.3508 us | 3.000 us |
// | Bench  | Neo.VM.Script | 3.141 us | 0.1141 us | 0.3143 us | 3.000 us |
// | Bench  | Neo.VM.Script | 3.277 us | 0.1066 us | 0.2935 us | 3.200 us |
// | Bench  | Neo.VM.Script | 3.013 us | 0.0706 us | 0.1871 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.631 us | 0.0562 us | 0.1379 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.609 us | 0.0560 us | 0.1053 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.104 us | 0.1276 us | 0.3557 us | 2.950 us |
// | Bench  | Neo.VM.Script | 3.155 us | 0.1232 us | 0.3371 us | 3.000 us |
// | Bench  | Neo.VM.Script | 3.765 us | 0.3251 us | 0.9586 us | 3.350 us |
// | Bench  | Neo.VM.Script | 2.569 us | 0.1243 us | 0.3360 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.478 us | 0.0534 us | 0.1127 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.652 us | 0.1093 us | 0.2935 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.792 us | 0.0954 us | 0.2595 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.806 us | 0.0701 us | 0.1860 us | 2.750 us |
// | Bench  | Neo.VM.Script | 2.865 us | 0.1478 us | 0.3971 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.239 us | 0.0621 us | 0.1701 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.362 us | 0.0912 us | 0.2449 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.658 us | 0.0951 us | 0.2587 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.365 us | 0.0943 us | 0.2548 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.274 us | 0.0790 us | 0.2149 us | 2.200 us |
// | Bench  | Neo.VM.Script | 3.118 us | 0.2473 us | 0.7135 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.204 us | 0.0479 us | 0.0968 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.180 us | 0.0443 us | 0.0414 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.380 us | 0.0634 us | 0.1681 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.382 us | 0.0515 us | 0.1233 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.583 us | 0.1001 us | 0.2773 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.833 us | 0.0920 us | 0.2504 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.819 us | 0.1012 us | 0.2787 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.854 us | 0.1110 us | 0.2886 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.381 us | 0.2943 us | 0.8678 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.566 us | 0.0918 us | 0.2514 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.657 us | 0.1507 us | 0.3997 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.900 us | 0.0977 us | 0.2625 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.390 us | 0.0605 us | 0.1625 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.881 us | 0.0790 us | 0.2175 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.514 us | 0.0747 us | 0.1995 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.487 us | 0.0532 us | 0.1024 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.801 us | 0.1410 us | 0.3907 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.862 us | 0.1503 us | 0.4166 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.996 us | 0.1315 us | 0.3554 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.016 us | 0.1229 us | 0.3365 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.767 us | 0.1496 us | 0.4121 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.548 us | 0.0611 us | 0.1652 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.623 us | 0.0666 us | 0.1813 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.697 us | 0.0927 us | 0.2553 us | 2.600 us |
// | Bench  | Neo.VM.Script | 4.226 us | 0.4011 us | 1.1763 us | 3.700 us |
// | Bench  | Neo.VM.Script | 3.005 us | 0.1070 us | 0.2947 us | 2.900 us |
