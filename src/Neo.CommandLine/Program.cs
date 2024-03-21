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

using Neo.CommandLine.Extensions;
using Neo.CommandLine.Handlers;
using Neo.CommandLine.Utilities;
using Neo.Extensions;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Neo.CommandLine
{
    public sealed partial class Program
    {
        internal readonly static int ApplicationVersion = AssemblyUtilities.GetVersionNumber();

        static async Task<int> Main(string[] args)
        {
            ConsoleUtilities.EnableAnsi();

            var rootCommand = new RootCommand("NEO command-lime tool.");
            var showCommand = new Command("show", "Print on chain data.");
            var contractCommand = new Command("contract", "Smart contract management.");
            var networkBroadcastCommand = new Command("broadcast", "Network broadcast.");

            #region Global Options

            var requestTimeoutOption = new Option<uint>("--timeout", () => 30u, "Task timeout in seconds.");
            var remoteVersionServerNameOption = new Option<string>("--server", () => ".")
            {
                IsHidden = true,
            };

            rootCommand.AddGlobalOption(requestTimeoutOption);
            rootCommand.AddGlobalOption(remoteVersionServerNameOption);

            #endregion

            #region Show Commands

            #region Block

            var showBlockCommand = new Command("block", "Display block information.");
            var blockIndexOrHashArgument = new Argument<string>("index|hash", "Block Index or Hash.");

            showBlockCommand.Add(blockIndexOrHashArgument);
            showBlockCommand.SetHandler(
                ShowCommandHandler.OnBlock,
                blockIndexOrHashArgument);

            #endregion

            #region Transaction

            var showTransactionCommand = new Command("transaction", "Display transaction information.");
            var transactionHashArgument = new Argument<string>("hash", "Transaction Hash.");

            showTransactionCommand.Add(transactionHashArgument);
            showTransactionCommand.SetHandler(
                ShowCommandHandler.OnTransaction,
                transactionHashArgument);

            #endregion

            #region Contract

            var showContractCommand = new Command("contract", "Display contract information.");
            var contractIdOrHashArgument = new Argument<string>("id|hash", "Contract ID or Hash.");

            showContractCommand.Add(contractIdOrHashArgument);
            showContractCommand.SetHandler(
                ShowCommandHandler.OnContract,
                contractIdOrHashArgument);

            #endregion

            #region Remote Version

            var showRemoteVersionCommand = new Command("info", "Display node information.");


            showRemoteVersionCommand.SetHandler(
                ShowCommandHandler.OnRemoteVersion,
                remoteVersionServerNameOption,
                requestTimeoutOption);

            #endregion

            rootCommand.Add(showCommand);
            showCommand.Add(showBlockCommand);
            showCommand.Add(showTransactionCommand);
            showCommand.Add(showContractCommand);
            showCommand.Add(showRemoteVersionCommand);

            #endregion

            #region Contract Commands

            #region Deploy

            var contractDeployCommand = new Command("deploy", "Deploy smart contract to blockchain.");
            var contractDeployNefFileArgument = new Argument<FileInfo>("nef", "Neo smart contract binary file.");
            var contractDeployDataParameterOption = new Option<string?>("--data", "Set data parameter.");

            contractDeployCommand.Add(contractDeployNefFileArgument);
            contractDeployCommand.Add(contractDeployDataParameterOption);
            contractDeployCommand.SetHandler(
                ContractCommandHandler.OnDeploy,
                contractDeployNefFileArgument,
                contractDeployDataParameterOption);

            #endregion

            #region Update

            var contractUpdateCommand = new Command("update", "Update smart contract on chain.");
            var contractUpdateScriptHashArgument = new Argument<string>("scripthash", "Neo smart contract hash.");
            var contractUpdateNefFileArgument = new Argument<FileInfo>("nef", "Neo smart contract binary file.");
            var contractUpdateSenderOption = new Option<string?>("--sender", "Payer of the network fee.");
            var contractUpdateSignerOption = new Option<string?>("--signer", "Signer of the transaction.");
            var contractUpdateDataParameterOption = new Option<string?>("--data", "Set data parameter.");

            contractUpdateCommand.Add(contractUpdateScriptHashArgument);
            contractUpdateCommand.Add(contractUpdateNefFileArgument);
            contractUpdateCommand.Add(contractUpdateSenderOption);
            contractUpdateCommand.Add(contractUpdateSignerOption);
            contractUpdateCommand.Add(contractUpdateDataParameterOption);
            contractUpdateCommand.SetHandler(
                ContractCommandHandler.OnUpdate,
                contractUpdateScriptHashArgument,
                contractUpdateNefFileArgument,
                contractUpdateSenderOption,
                contractUpdateSignerOption,
                contractUpdateDataParameterOption);

            #endregion

            #region Invoke

            var contractInvokeCommand = new Command("invoke", "Invoke smart contract method on chain.");
            var contractInvokeScriptHashArgument = new Argument<string>("scripthash", "Neo smart contract hash.");
            var contractInvokeMethodNameArgument = new Argument<string>("--method", "Neo smart contract method name.");
            var contractInvokeMethodParametersOptions = new Option<IEnumerable<string>?>("--params", "Neo smart contract method params.")
            {
                AllowMultipleArgumentsPerToken = true
            };
            var contractInvokeSenderOption = new Option<string>("--sender", "Payer of the network fee.");
            var contractInvokeSignerOption = new Option<string>("--signer", "Signer of the transaction.");

            contractInvokeCommand.Add(contractInvokeScriptHashArgument);
            contractInvokeCommand.Add(contractInvokeMethodNameArgument);
            contractInvokeCommand.Add(contractInvokeMethodParametersOptions);
            contractInvokeCommand.Add(contractInvokeSenderOption);
            contractInvokeCommand.Add(contractInvokeSignerOption);
            contractInvokeCommand.SetHandler(
                ContractCommandHandler.OnInvoke,
                contractInvokeScriptHashArgument,
                contractInvokeMethodNameArgument,
                contractInvokeMethodParametersOptions,
                contractInvokeSenderOption,
                contractInvokeSignerOption);

            #endregion

            rootCommand.Add(contractCommand);
            contractCommand.Add(contractDeployCommand);
            contractCommand.Add(contractUpdateCommand);
            contractCommand.Add(contractInvokeCommand);

            #endregion

            #region Broadcast Commands

            #region Address

            var networkBroadcastAddressCommand = new Command("address", "Broadcast address capabilities.");
            var networkBroadcastAddressIpAddressArgument = new Argument<IPAddress>("ip", "Ip Address.");
            var networkBroadcastAddressPortArgument = new Argument<ushort>("port", "listening port.");

            networkBroadcastAddressCommand.Add(networkBroadcastAddressIpAddressArgument);
            networkBroadcastAddressCommand.Add(networkBroadcastAddressPortArgument);
            networkBroadcastAddressCommand.SetHandler(
                NetworkCommandHandler.OnBroadcastAddress,
                networkBroadcastAddressIpAddressArgument,
                networkBroadcastAddressPortArgument);

            #endregion

            #region Block

            var networkBroadcastBlockCommand = new Command("block", "Broadcast a block.");
            var networkBroadcastBlockIndexOrHashArgument = new Argument<string>("index|hash", "Block index or UInt256 hash.");

            networkBroadcastBlockCommand.Add(networkBroadcastBlockIndexOrHashArgument);
            networkBroadcastBlockCommand.SetHandler(
                NetworkCommandHandler.OnBroadcastBlock,
                networkBroadcastBlockIndexOrHashArgument);

            #endregion

            #region Get Commands

            var networkBroadcastGetCommand = new Command("get", "Send request network packets.");

            networkBroadcastCommand.Add(networkBroadcastGetCommand);

            #region Headers

            var networkBroadcastGetHeadersCommand = new Command("header", "Send request header network packet.");
            var networkBroadcastGetHeadersIndexArgument = new Argument<uint>("index", "Header Index.");

            networkBroadcastGetCommand.Add(networkBroadcastGetHeadersCommand);
            networkBroadcastGetHeadersCommand.Add(networkBroadcastGetHeadersIndexArgument);
            networkBroadcastGetHeadersCommand.SetHandler(
                NetworkCommandHandler.OnBroadcastGetHeader,
                networkBroadcastGetHeadersIndexArgument);

            rootCommand.Add(networkBroadcastCommand);
            networkBroadcastCommand.Add(networkBroadcastAddressCommand);
            networkBroadcastCommand.Add(networkBroadcastBlockCommand);

            #endregion

            #endregion

            #endregion

            #region CommandLine Defaults

            var cb = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler((e, c) =>
                {
                    c.Console.Error.WriteLine($"{e.GetType().Name}: \"{e.Message}\"",
                        textColor: AnsiColor.Red);
#if DEBUG
                    c.Console.Error.WriteLine($"{e.StackTrace}",
                        textColor: AnsiColor.Red);
#endif
                    c.ExitCode = 1;
                })
                .Build();

            #endregion

            return await cb.InvokeAsync(args);
        }
    }
}

