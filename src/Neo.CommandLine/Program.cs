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

using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Neo.CommandLine
{
    public sealed partial class Program
    {
        internal static readonly TextWriter ConsoleOut = Console.Out;
        internal static readonly TextReader ConsoleIn = Console.In;

        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand("NEO command-lime tool.");
            var showCommand = new Command("show", "Display data from the blockchain.");
            var contractCommand = new Command("contract", "Smart contract management.");

            #region Show Commands

            #region Block

            var showBlockCommand = new Command("block", "Display block information.");
            var blockIndexOrHashArgument = new Argument<string>("index|hash", "Block Index or Hash.");

            showBlockCommand.Add(blockIndexOrHashArgument);
            showBlockCommand.SetHandler(
                (indexOrHash) => ConsoleOut.WriteLine($"Hello Block {indexOrHash}!"),
                blockIndexOrHashArgument);

            #endregion

            #region Transaction

            var showTransactionCommand = new Command("transaction", "Display transaction information.");
            var transactionHashArgument = new Argument<string>("hash", "Transaction Hash.");

            showTransactionCommand.Add(transactionHashArgument);
            showTransactionCommand.SetHandler(
                (txHash) => ConsoleOut.WriteLine($"Hello Transaction {txHash}!"),
                transactionHashArgument);

            #endregion

            #region Contract

            var showContractCommand = new Command("contract", "Display contract information.");
            var contractIdOrHashArgument = new Argument<string>("id|hash", "Contract ID or Hash.");

            showContractCommand.Add(contractIdOrHashArgument);
            showContractCommand.SetHandler(
                (idOrHash) => ConsoleOut.WriteLine($"Hello Contract {idOrHash}!"),
                contractIdOrHashArgument);

            #endregion

            rootCommand.Add(showCommand);
            showCommand.Add(showBlockCommand);
            showCommand.Add(showTransactionCommand);
            showCommand.Add(showContractCommand);

            #endregion

            #region Contract Commands

            #region Deploy

            var contractDeployCommand = new Command("deploy", "Deploy smart contract to blockchain.");
            var contractDeployNefFileArgument = new Argument<FileInfo>("nef", "Neo smart contract binary file.");
            var contractDeployDataParameterOption = new Option<string>("--data", "Set data parameter.");

            contractDeployCommand.Add(contractDeployNefFileArgument);
            contractDeployCommand.Add(contractDeployDataParameterOption);
            contractDeployCommand.SetHandler(
                (nefFileInfo, dataParameter) => ConsoleOut.WriteLine($"Hello Contract Deploy \"{nefFileInfo}\" {dataParameter}!"),
                contractDeployNefFileArgument,
                contractDeployDataParameterOption);

            #endregion

            #region Update

            var contractUpdateCommand = new Command("update", "Update smart contract on chain.");
            var contractUpdateScriptHashArgument = new Argument<string>("scriptHash", "Neo smart contract hash.");
            var contractUpdateNefFileOption = new Option<FileInfo>("--nef", "Neo smart contract binary file.");
            var contractUpdateSenderOption = new Option<string>("--sender", "Payer of the network fee.");
            var contractUpdateSignerOption = new Option<string>("--signer", "Signer of the transaction.");
            var contractUpdateDataParameterOption = new Option<string>("--data", "Set data parameter.");

            contractUpdateCommand.Add(contractUpdateScriptHashArgument);
            contractUpdateCommand.Add(contractUpdateNefFileOption);
            contractUpdateCommand.Add(contractUpdateSenderOption);
            contractUpdateCommand.Add(contractUpdateSignerOption);
            contractUpdateCommand.Add(contractUpdateDataParameterOption);
            contractUpdateCommand.SetHandler(
                (scriptHash, nefFileInfo, txSender, txSigner, dataParameter) => ConsoleOut.WriteLine($"Hello Contract Update {scriptHash} \"{nefFileInfo}\" {txSender} {txSigner} {dataParameter}!"),
                contractUpdateScriptHashArgument,
                contractUpdateNefFileOption,
                contractUpdateSenderOption,
                contractUpdateSignerOption,
                contractUpdateDataParameterOption);

            #endregion

            rootCommand.Add(contractCommand);
            contractCommand.Add(contractDeployCommand);
            contractCommand.Add(contractUpdateCommand);

            #endregion

            await rootCommand.InvokeAsync(args);
        }
    }
}

