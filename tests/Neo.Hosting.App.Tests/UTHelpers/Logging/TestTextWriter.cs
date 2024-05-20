// Copyright (C) 2015-2024 The Neo Project.
//
// UT_TestTextWriter.cs file belongs to the neo project and is free
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
using Neo.Hosting.App.Tests.UTHelpers.Logging;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.UTHelpers.Logging
{
    internal class TestTextWriter
        (ITestOutputHelper testOutputHelper) : TextWriter
    {
        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        public override Encoding Encoding => Utility.StrictUTF8;

        private void WriteOutput(string? value, params object[] args)
        {
            if (string.IsNullOrEmpty(value))
                _testOutputHelper.WriteLine(string.Empty);
            else
                _testOutputHelper.WriteLine(value, args);
        }

        public override void Write(string? value)
        {
            WriteOutput(value);
        }

        public override void Write([StringSyntax("CompositeFormat")] string format, object? arg0)
        {
            WriteOutput(format, arg0);
        }

        public override void Write([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1)
        {
            WriteOutput(format, arg0, arg1);
        }

        public override void Write([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1, object? arg2)
        {
            WriteOutput(format, arg0, arg1, arg2);
        }

        public override void Write([StringSyntax("CompositeFormat")] string format, params object?[] arg)
        {
            WriteOutput(format, arg);
        }

        public override void WriteLine()
        {
            WriteOutput(null);
        }

        public override void WriteLine(string? value)
        {
            WriteOutput(value);
        }

        public override void WriteLine([StringSyntax("CompositeFormat")] string format, object? arg0)
        {
            WriteOutput(format, arg0);
        }

        public override void WriteLine([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1)
        {
            WriteOutput(format, arg0, arg1);
        }

        public override void WriteLine([StringSyntax("CompositeFormat")] string format, object? arg0, object? arg1, object? arg2)
        {
            WriteOutput(format, arg0, arg1, arg2);
        }

        public override void WriteLine([StringSyntax("CompositeFormat")] string format, params object?[] arg)
        {
            WriteOutput(format, arg);
        }
    }
}
