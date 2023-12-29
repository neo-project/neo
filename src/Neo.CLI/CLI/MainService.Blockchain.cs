// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-cli is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Linq;

namespace Neo.CLI
{
    partial class MainService
    {
        /// <summary>
        /// Process "export blocks" command
        /// </summary>
        /// <param name="start">Start</param>
        /// <param name="count">Number of blocks</param>
        /// <param name="path">Path</param>
        [ConsoleCommand("export blocks", Category = "Blockchain Commands")]
        private void OnExportBlocksStartCountCommand(uint start, uint count = uint.MaxValue, string? path = null)
        {
            uint height = NativeContract.Ledger.CurrentIndex(NeoSystem.StoreView);
            if (height < start)
            {
                ConsoleHelper.Error("invalid start height.");
                return;
            }

            count = Math.Min(count, height - start + 1);

            if (string.IsNullOrEmpty(path))
            {
                path = $"chain.{start}.acc";
            }

            WriteBlocks(start, count, path, true);
        }

        [ConsoleCommand("show block", Category = "Blockchain Commands")]
        private void OnShowBlockCommand(string indexOrHash)
        {
            lock (syncRoot)
            {
                Block? block = null;

                if (uint.TryParse(indexOrHash, out var index))
                    block = NativeContract.Ledger.GetBlock(_neoSystem.StoreView, index);
                else if (UInt256.TryParse(indexOrHash, out var hash))
                    block = NativeContract.Ledger.GetBlock(_neoSystem.StoreView, hash);
                else
                {
                    ConsoleHelper.Error("Enter a valid block index or hash.");
                    return;
                }

                if (block is null)
                {
                    ConsoleHelper.Error($"Block {indexOrHash} doesn't exist.");
                    return;
                }

                DateTime blockDatetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                blockDatetime = blockDatetime.AddMilliseconds(block.Timestamp).ToLocalTime();

                ConsoleHelper.Info("", "-------------", "Block", "-------------");
                ConsoleHelper.Info();
                ConsoleHelper.Info("", "      Timestamp: ", $"{blockDatetime}");
                ConsoleHelper.Info("", "          Index: ", $"{block.Index}");
                ConsoleHelper.Info("", "           Hash: ", $"{block.Hash}");
                ConsoleHelper.Info("", "          Nonce: ", $"{block.Nonce}");
                ConsoleHelper.Info("", "     MerkleRoot: ", $"{block.MerkleRoot}");
                ConsoleHelper.Info("", "       PrevHash: ", $"{block.PrevHash}");
                ConsoleHelper.Info("", "  NextConsensus: ", $"{block.NextConsensus}");
                ConsoleHelper.Info("", "   PrimaryIndex: ", $"{block.PrimaryIndex}");
                ConsoleHelper.Info("", "  PrimaryPubKey: ", $"{NativeContract.NEO.GetCommittee(_neoSystem.GetSnapshot())[block.PrimaryIndex]}");
                ConsoleHelper.Info("", "        Version: ", $"{block.Version}");
                ConsoleHelper.Info("", "           Size: ", $"{block.Size} Byte(s)");
                ConsoleHelper.Info();

                ConsoleHelper.Info("", "-------------", "Witness", "-------------");
                ConsoleHelper.Info();
                ConsoleHelper.Info("", "    Invocation Script: ", $"{Convert.ToBase64String(block.Witness.InvocationScript.Span)}");
                ConsoleHelper.Info("", "  Verification Script: ", $"{Convert.ToBase64String(block.Witness.VerificationScript.Span)}");
                ConsoleHelper.Info("", "           ScriptHash: ", $"{block.Witness.ScriptHash}");
                ConsoleHelper.Info("", "                 Size: ", $"{block.Witness.Size} Byte(s)");
                ConsoleHelper.Info();

                ConsoleHelper.Info("", "-------------", "Transactions", "-------------");
                ConsoleHelper.Info();

                if (block.Transactions.Length == 0)
                {
                    ConsoleHelper.Info("", "  No Transaction(s)");
                }
                else
                {
                    foreach (var tx in block.Transactions)
                        ConsoleHelper.Info($"  {tx.Hash}");
                }
                ConsoleHelper.Info();
                ConsoleHelper.Info("", "--------------------------------------");
            }
        }

        [ConsoleCommand("show tx", Category = "Blockchain Commands")]
        public void OnShowTransactionCommand(UInt256 hash)
        {
            lock (syncRoot)
            {
                var tx = NativeContract.Ledger.GetTransactionState(_neoSystem.StoreView, hash);

                if (tx is null)
                {
                    ConsoleHelper.Error($"Transaction {hash} doesn't exist.");
                    return;
                }

                var block = NativeContract.Ledger.GetHeader(_neoSystem.StoreView, tx.BlockIndex);

                DateTime transactionDatetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                transactionDatetime = transactionDatetime.AddMilliseconds(block.Timestamp).ToLocalTime();

                ConsoleHelper.Info("", "-------------", "Transaction", "-------------");
                ConsoleHelper.Info();
                ConsoleHelper.Info("", "        Timestamp: ", $"{transactionDatetime}");
                ConsoleHelper.Info("", "             Hash: ", $"{tx.Transaction.Hash}");
                ConsoleHelper.Info("", "            Nonce: ", $"{tx.Transaction.Nonce}");
                ConsoleHelper.Info("", "           Sender: ", $"{tx.Transaction.Sender}");
                ConsoleHelper.Info("", "  ValidUntilBlock: ", $"{tx.Transaction.ValidUntilBlock}");
                ConsoleHelper.Info("", "       FeePerByte: ", $"{tx.Transaction.FeePerByte}");
                ConsoleHelper.Info("", "       NetworkFee: ", $"{tx.Transaction.NetworkFee}");
                ConsoleHelper.Info("", "        SystemFee: ", $"{tx.Transaction.SystemFee}");
                ConsoleHelper.Info("", "           Script: ", $"{Convert.ToBase64String(tx.Transaction.Script.Span)}");
                ConsoleHelper.Info("", "          Version: ", $"{tx.Transaction.Version}");
                ConsoleHelper.Info("", "       BlockIndex: ", $"{block.Index}");
                ConsoleHelper.Info("", "        BlockHash: ", $"{block.Hash}");
                ConsoleHelper.Info("", "             Size: ", $"{tx.Transaction.Size} Byte(s)");
                ConsoleHelper.Info();

                ConsoleHelper.Info("", "-------------", "Signers", "-------------");
                ConsoleHelper.Info();

                foreach (var signer in tx.Transaction.Signers)
                {
                    if (signer.Rules.Length == 0)
                        ConsoleHelper.Info("", "             Rules: ", "[]");
                    else
                        ConsoleHelper.Info("", "             Rules: ", $"[{string.Join(", ", signer.Rules.Select(s => $"\"{s.ToJson()}\""))}]");
                    ConsoleHelper.Info("", "           Account: ", $"{signer.Account}");
                    ConsoleHelper.Info("", "            Scopes: ", $"{signer.Scopes}");
                    if (signer.AllowedContracts.Length == 0)
                        ConsoleHelper.Info("", "  AllowedContracts: ", "[]");
                    else
                        ConsoleHelper.Info("", "  AllowedContracts: ", $"[{string.Join(", ", signer.AllowedContracts.Select(s => s.ToString()))}]");
                    if (signer.AllowedGroups.Length == 0)
                        ConsoleHelper.Info("", "     AllowedGroups: ", "[]");
                    else
                        ConsoleHelper.Info("", "     AllowedGroups: ", $"[{string.Join(", ", signer.AllowedGroups.Select(s => s.ToString()))}]");
                    ConsoleHelper.Info("", "              Size: ", $"{signer.Size} Byte(s)");
                    ConsoleHelper.Info();
                }

                ConsoleHelper.Info("", "-------------", "Witnesses", "-------------");
                ConsoleHelper.Info();
                foreach (var witness in tx.Transaction.Witnesses)
                {
                    ConsoleHelper.Info("", "    InvocationScript: ", $"{Convert.ToBase64String(witness.InvocationScript.Span)}");
                    ConsoleHelper.Info("", "  VerificationScript: ", $"{Convert.ToBase64String(witness.VerificationScript.Span)}");
                    ConsoleHelper.Info("", "          ScriptHash: ", $"{witness.ScriptHash}");
                    ConsoleHelper.Info("", "                Size: ", $"{witness.Size} Byte(s)");
                    ConsoleHelper.Info();
                }

                ConsoleHelper.Info("", "-------------", "Attributes", "-------------");
                ConsoleHelper.Info();
                if (tx.Transaction.Attributes.Length == 0)
                {
                    ConsoleHelper.Info("", "  No Attribute(s).");
                }
                else
                {
                    foreach (var attribute in tx.Transaction.Attributes)
                    {
                        switch (attribute)
                        {
                            case Conflicts c:
                                ConsoleHelper.Info("", "  Type: ", $"{c.Type}");
                                ConsoleHelper.Info("", "  Hash: ", $"{c.Hash}");
                                ConsoleHelper.Info("", "  Size: ", $"{c.Size} Byte(s)");
                                break;
                            case OracleResponse o:
                                ConsoleHelper.Info("", "    Type: ", $"{o.Type}");
                                ConsoleHelper.Info("", "      Id: ", $"{o.Id}");
                                ConsoleHelper.Info("", "    Code: ", $"{o.Code}");
                                ConsoleHelper.Info("", "  Result: ", $"{Convert.ToBase64String(o.Result.Span)}");
                                ConsoleHelper.Info("", "    Size: ", $"{o.Size} Byte(s)");
                                break;
                            case HighPriorityAttribute p:
                                ConsoleHelper.Info("", "    Type: ", $"{p.Type}");
                                break;
                            case NotValidBefore n:
                                ConsoleHelper.Info("", "    Type: ", $"{n.Type}");
                                ConsoleHelper.Info("", "  Height: ", $"{n.Height}");
                                break;
                            default:
                                ConsoleHelper.Info("", "  Type: ", $"{attribute.Type}");
                                ConsoleHelper.Info("", "  Size: ", $"{attribute.Size} Byte(s)");
                                break;
                        }
                    }
                }
                ConsoleHelper.Info();
                ConsoleHelper.Info("", "--------------------------------------");
            }
        }

        [ConsoleCommand("show contract", Category = "Blockchain Commands")]
        public void OnShowContractCommand(string nameOrHash)
        {
            lock (syncRoot)
            {
                ContractState? contract = null;

                if (UInt160.TryParse(nameOrHash, out var scriptHash))
                    contract = NativeContract.ContractManagement.GetContract(_neoSystem.StoreView, scriptHash);
                else
                {
                    var nativeContract = NativeContract.Contracts.SingleOrDefault(s => s.Name.Equals(nameOrHash, StringComparison.InvariantCultureIgnoreCase));

                    if (nativeContract != null)
                        contract = NativeContract.ContractManagement.GetContract(_neoSystem.StoreView, nativeContract.Hash);
                }

                if (contract is null)
                {
                    ConsoleHelper.Error($"Contract {nameOrHash} doesn't exist.");
                    return;
                }

                ConsoleHelper.Info("", "-------------", "Contract", "-------------");
                ConsoleHelper.Info();
                ConsoleHelper.Info("", "                Name: ", $"{contract.Manifest.Name}");
                ConsoleHelper.Info("", "                Hash: ", $"{contract.Hash}");
                ConsoleHelper.Info("", "                  Id: ", $"{contract.Id}");
                ConsoleHelper.Info("", "       UpdateCounter: ", $"{contract.UpdateCounter}");
                ConsoleHelper.Info("", "  SupportedStandards: ", $"{string.Join(" ", contract.Manifest.SupportedStandards)}");
                ConsoleHelper.Info("", "            Checksum: ", $"{contract.Nef.CheckSum}");
                ConsoleHelper.Info("", "            Compiler: ", $"{contract.Nef.Compiler}");
                ConsoleHelper.Info("", "          SourceCode: ", $"{contract.Nef.Source}");
                ConsoleHelper.Info("", "              Trusts: ", $"[{string.Join(", ", contract.Manifest.Trusts.Select(s => s.ToJson()?.GetString()))}]");
                if (contract.Manifest.Extra is not null)
                {
                    foreach (var extra in contract.Manifest.Extra.Properties)
                    {
                        ConsoleHelper.Info("", $"  {extra.Key,18}: ", $"{extra.Value?.GetString()}");
                    }
                }
                ConsoleHelper.Info();

                ConsoleHelper.Info("", "-------------", "Groups", "-------------");
                ConsoleHelper.Info();
                if (contract.Manifest.Groups.Length == 0)
                {
                    ConsoleHelper.Info("", "  No Group(s).");
                }
                else
                {
                    foreach (var group in contract.Manifest.Groups)
                    {
                        ConsoleHelper.Info("", "     PubKey: ", $"{group.PubKey}");
                        ConsoleHelper.Info("", "  Signature: ", $"{Convert.ToBase64String(group.Signature)}");
                    }
                }
                ConsoleHelper.Info();

                ConsoleHelper.Info("", "-------------", "Permissions", "-------------");
                ConsoleHelper.Info();
                foreach (var permission in contract.Manifest.Permissions)
                {
                    ConsoleHelper.Info("", "  Contract: ", $"{permission.Contract.ToJson()?.GetString()}");
                    if (permission.Methods.IsWildcard)
                        ConsoleHelper.Info("", "   Methods: ", "*");
                    else
                        ConsoleHelper.Info("", "   Methods: ", $"{string.Join(", ", permission.Methods)}");
                    ConsoleHelper.Info();
                }

                ConsoleHelper.Info("", "-------------", "Methods", "-------------");
                ConsoleHelper.Info();
                foreach (var method in contract.Manifest.Abi.Methods)
                {
                    ConsoleHelper.Info("", "        Name: ", $"{method.Name}");
                    ConsoleHelper.Info("", "        Safe: ", $"{method.Safe}");
                    ConsoleHelper.Info("", "      Offset: ", $"{method.Offset}");
                    ConsoleHelper.Info("", "  Parameters: ", $"[{string.Join(", ", method.Parameters.Select(s => s.Type.ToString()))}]");
                    ConsoleHelper.Info("", "  ReturnType: ", $"{method.ReturnType}");
                    ConsoleHelper.Info();
                }

                ConsoleHelper.Info("", "-------------", "Script", "-------------");
                ConsoleHelper.Info();
                ConsoleHelper.Info($"  {Convert.ToBase64String(contract.Nef.Script.Span)}");
                ConsoleHelper.Info();
                ConsoleHelper.Info("", "--------------------------------");
            }
        }
    }
}
