// Copyright (C) 2015-2024 The Neo Project.
//
// UT_MemberDataCases.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Hosting;
using Neo.Hosting.App;
using Neo.Hosting.App.Tests;
using Neo.Hosting.App.Tests.UTHelpers;
using Neo.Hosting.App.Tests.UTHelpers.DataCases;

namespace Neo.Hosting.App.Tests.UTHelpers.DataCases
{
    internal static class UT_MemberDataCases
    {
        public static TheoryData<object, byte[]> Struffer_ReadWrite_Cases =>
            new()
            {
                { (byte)0x01, [0x01,] },
                { (sbyte)0x7f, [0x7f,] },
                { (short)0x0102, [0x02, 0x01,] },
                { (ushort)0xf0f1u, [0xf1, 0xf0,] },
                { 0xd0d1d2d3, [0xd3, 0xd2, 0xd1, 0xd0,] },
                { 0x12345678u, [0x78, 0x56, 0x34, 0x12,] },
                { 0x1234567890abcdef, [0xef, 0xcd, 0xab, 0x90, 0x78, 0x56, 0x34, 0x12,] },
                { 0xdeadc0debad0c0deul, [0xde, 0xc0, 0xd0, 0xba, 0xde, 0xc0, 0xad, 0xde,] },
            };

        public static TheoryData<string, byte[]> Struffer_ReadWrite_String_Cases =>
            new()
            {
                { "Hello", [0x05, 0x00, 0x00, 0x00, 0x48, 0x65, 0x6c, 0x6c, 0x6f,] },
                { "World", [0x05, 0x00, 0x00, 0x00, 0x57, 0x6f, 0x72, 0x6c, 0x64,] },
            };
    }
}
