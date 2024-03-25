// Copyright (C) 2015-2024 The Neo Project.
//
// DefaultRootCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.CommandLine;
using System.IO;

namespace Neo.Service.App.Commands
{
    internal class DefaultRootCommand : Command
    {
        private static string? s_executablePath;
        private static string? s_executableName;

        public DefaultRootCommand() : base(ExecutableName, "NEO Blockchain CommandLine Tool")
        {
            var exportCommand = new ExportCommand();
            var runCommand = new RunCommand();
            var walletCommand = new WalletCommand();

            AddCommand(exportCommand);
            AddCommand(runCommand);
            AddCommand(walletCommand);
        }

        public static string ExecutableName =>
            s_executableName ??= Path.GetFileNameWithoutExtension(ExecutablePath).Replace(" ", "");

        public static string ExecutablePath =>
            s_executablePath ??= Environment.GetCommandLineArgs()[0];
    }
}
