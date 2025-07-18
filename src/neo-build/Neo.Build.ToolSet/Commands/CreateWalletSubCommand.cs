// Copyright (C) 2015-2025 The Neo Project.
//
// CreateWalletSubCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core;
using Neo.Build.Core.Wallets;
using Neo.Build.ToolSet.Configuration;
using Neo.Build.ToolSet.Extensions;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet.Commands
{
    internal class CreateWalletSubCommand : Command
    {
        public CreateWalletSubCommand() : base("wallet", "Create a NEP6 wallet")
        {
            var isProdWalletOption = new Option<bool>(["--is-production", "-P"], GetDefaultIsProductionWallet, "Make wallet for development");
            var walletFilenameOption = new Option<string>(["--filename", "-f"], GetDefaultWalletFilename, "Wallet filename");
            var privateKeyOption = new Option<string>(["--private-key", "-Pk"], "Private key to use for the default account");
            var privateKeyFormatOption = new Option<PrivateKeyFormat>(["--key-format", "-Kf"], GetPrivateKeyFormat, "Format of the private key");
            var defaultAccountNameOption = new Option<string>(["--name", "-n"], "Name of default account [default: name{number}]");
            var stdOutOption = new Option<bool>(["--stdout"], "Print wallet to stdout");
            var passwordOption = new Option<string>(["--password", "-p"], "Password for production wallet");

            AddOption(isProdWalletOption);
            AddOption(walletFilenameOption);
            AddOption(privateKeyOption);
            AddOption(privateKeyFormatOption);
            AddOption(defaultAccountNameOption);
            AddOption(stdOutOption);
            AddOption(passwordOption);
        }

        public enum PrivateKeyFormat : byte
        {
            HexString = 0,
            WIFString = 1,
        }

        private static bool GetDefaultIsProductionWallet() =>
            false;

        private static string GetDefaultWalletFilename() =>
            FileNameDefaults.WalletName;

        private static PrivateKeyFormat GetPrivateKeyFormat() =>
            PrivateKeyFormat.HexString;

        public new sealed class Handler(
            INeoConfigurationOptions neoConfiguration) : ICommandHandler
        {
            public string? PrivateKey { get; set; }
            public string? Password { get; set; }
            public PrivateKeyFormat KeyFormat { get; set; } = GetPrivateKeyFormat();
            public string Filename { get; set; } = GetDefaultWalletFilename();
            public bool IsProduction { get; set; } = GetDefaultIsProductionWallet();
            public bool StdOut { get; set; } = false;
            public string? Name { get; set; }

            private readonly INeoConfigurationOptions _neoConfiguration = neoConfiguration;

            public int Invoke(InvocationContext context) =>
                InvokeAsync(context).ConfigureAwait(false).GetAwaiter().GetResult();

            public Task<int> InvokeAsync(InvocationContext context)
            {
                if (IsProduction && string.IsNullOrEmpty(Password))
                {
                    context.Console.ErrorMessage("Password is required.");
                    return Task.FromResult(NeoBuildErrorCodes.Wallet.InvalidPassword * -1);
                }

                if (IsProduction && Password?.Length < 8)
                {
                    context.Console.ErrorMessage($"Password length {Ansi.Text.UnderlinedOn}MUST BE{Ansi.Text.UnderlinedOff} a length of at least 8 characters.");
                    return Task.FromResult(NeoBuildErrorCodes.Wallet.InvalidPassword * -1);
                }

                KeyPair privateKey;

                if (string.IsNullOrEmpty(PrivateKey))
                    privateKey = new(RandomNumberGenerator.GetBytes(32));
                else
                {
                    privateKey = KeyFormat switch
                    {
                        PrivateKeyFormat.WIFString => new(Wallet.GetPrivateKeyFromWIF(PrivateKey)),
                        PrivateKeyFormat.HexString or _ => new(Convert.FromHexString(PrivateKey)),
                    };
                }

                Wallet wallet;

                if (IsProduction)
                    wallet = CreateProductionWallet(privateKey);
                else
                    wallet = CreateDevelopmentWallet(privateKey);

                if (StdOut == false)
                {
                    if (IsProduction == false)
                    {
                        context.Console.SetTerminalForegroundColor(ConsoleColor.Yellow);
                        context.Console.WriteLine($"WANRING: Private keys {Ansi.Text.UnderlinedOn}ARE NOT{Ansi.Text.UnderlinedOff} encrypted.{Ansi.Text.BoldOff}");
                        context.Console.ResetColor();
                    }

                    wallet.Save();
                    context.Console.WriteLine("Wallet file '{0}' saved.", new FileInfo(Filename).FullName);
                }
                else
                {
                    if (wallet is DevWallet devWallet)
                        context.Console.WriteLine($"{devWallet}");
                    else
                    {
                        context.Console.SetTerminalForegroundColor(ConsoleColor.Yellow);
                        context.Console.WriteLine($"WARNING: Production wallets {Ansi.Text.UnderlinedOn}WILL NOT{Ansi.Text.UnderlinedOff} be displayed for security reasons.");
                        context.Console.ResetColor();
                    }
                }

                return Task.FromResult(0);
            }

            private Wallet CreateProductionWallet(KeyPair privateKey)
            {
                var fileInfo = new FileInfo(Filename);
                var wallet = new NEP6Wallet(fileInfo.FullName, Password, ProtocolSettings.Default);
                var walletAccount = wallet.CreateAccount(privateKey.PrivateKey);
                var walletKey = walletAccount.GetKey();

                walletAccount.Label = Name;
                return wallet;
            }

            private Wallet CreateDevelopmentWallet(KeyPair privateKey)
            {
                var protocolSettings = ProtocolSettings.Default with
                {
                    Network = _neoConfiguration.ProtocolOptions.Network,
                    AddressVersion = _neoConfiguration.ProtocolOptions.AddressVersion,
                    ValidatorsCount = 1,
                    StandbyCommittee = [privateKey.PublicKey],
                    InitialGasDistribution = _neoConfiguration.ProtocolOptions.InitialGasDistribution,
                    MaxTraceableBlocks = _neoConfiguration.ProtocolOptions.MaxTraceableBlocks,
                    MaxTransactionsPerBlock = _neoConfiguration.ProtocolOptions.MaxTransactionsPerBlock,
                    MemoryPoolMaxTransactions = _neoConfiguration.ProtocolOptions.MemoryPoolMaxTransactions,
                    MillisecondsPerBlock = _neoConfiguration.ProtocolOptions.MillisecondsPerBlock,
                    SeedList = [$"{_neoConfiguration.NetworkOptions.Listen}:{_neoConfiguration.NetworkOptions.Port}"]
                };

                var fileInfo = new FileInfo(Filename);
                var wallet = new DevWallet(fileInfo.FullName, protocolSettings);
                var walletAccount = wallet.CreateAccount(privateKey.PrivateKey, Name);
                var walletKey = walletAccount.GetKey();

                wallet.CreateMultiSigAccount([walletKey.PublicKey], "node1", isDefaultAccount: true);

                return wallet;
            }
        }
    }
}
