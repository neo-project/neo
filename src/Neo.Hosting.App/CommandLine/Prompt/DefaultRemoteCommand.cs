// Copyright (C) 2015-2024 The Neo Project.
//
// DefaultRemoteCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Helpers;
using System;
using System.CommandLine;

namespace Neo.Hosting.App.CommandLine.Prompt
{
    internal sealed class DefaultRemoteCommand : Command
    {
        private static string? s_executablePath;

        public DefaultRemoteCommand() : base(ExecutableName, $"Your are connected to {ExecutablePath}")
        {
            var walletCommand = new WalletCommand();
            AddCommand(walletCommand);
        }

        public static string ExecutableName => $"{Environment.UserName}@{Environment.MachineName}:~$";

        public static string ExecutablePath =>
            s_executablePath ??= $"{EnvironmentUtility.AddOrGetServicePipeName()}";
    }
}
