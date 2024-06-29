// Copyright (C) 2015-2024 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using Neo.Extensions;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Neo.Hosting.App
{
    public sealed partial class Program
    {
        internal const string DEFAULT_VERSION_STRING = "0.0.0";

        internal static int ApplicationVersionNumber =>
            AssemblyUtility.GetVersionNumber();

        internal static Version ApplicationVersion =>
            Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0");

        internal static bool IsRunningAsService =>
            (SystemdHelpers.IsSystemdService() || WindowsServiceHelpers.IsWindowsService()) || Environment.UserInteractive == false;

        static Task<int> Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();

            return Task.FromResult(-1);
        }
    }
}
