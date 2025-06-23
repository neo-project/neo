// Copyright (C) 2015-2025 The Neo Project.
//
// SignClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Neo.ConsoleService;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Sign;
using Neo.SmartContract;
using Servicepb;
using Signpb;
using System.Diagnostics.CodeAnalysis;
using static Neo.SmartContract.Helper;

using ExtensiblePayload = Neo.Network.P2P.Payloads.ExtensiblePayload;

namespace Neo.Plugins.SignClient
{
    /// <summary>
    /// A signer that uses a client to sign transactions.
    /// </summary>
    public class SignClient : Plugin, ISigner
    {
        private GrpcChannel? _channel;

        private SecureSign.SecureSignClient? _client;

        private string _name = string.Empty;

        public override string Description => "Signer plugin for signer service.";

        public override string ConfigFile => System.IO.Path.Combine(RootPath, "SignClient.json");

        public SignClient() { }

        public SignClient(Settings settings)
        {
            Reset(settings);
        }

        // It's for test now.
        internal SignClient(string name, SecureSign.SecureSignClient client)
        {
            Reset(name, client);
        }

        private void Reset(string name, SecureSign.SecureSignClient? client)
        {
            if (_client is not null) SignerManager.UnregisterSigner(_name);

            _name = name;
            _client = client;
            if (!string.IsNullOrEmpty(_name)) SignerManager.RegisterSigner(_name, this);
        }

        private ServiceConfig GetServiceConfig(Settings settings)
        {
            var methodConfig = new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialBackoff = TimeSpan.FromMilliseconds(50),
                    MaxBackoff = TimeSpan.FromMilliseconds(200),
                    BackoffMultiplier = 1.5,
                    RetryableStatusCodes = {
                        StatusCode.Cancelled,
                        StatusCode.DeadlineExceeded,
                        StatusCode.ResourceExhausted,
                        StatusCode.Unavailable,
                        StatusCode.Aborted,
                        StatusCode.Internal,
                        StatusCode.DataLoss,
                        StatusCode.Unknown
                    }
                }
            };

            return new ServiceConfig { MethodConfigs = { methodConfig } };
        }

        private void Reset(Settings settings)
        {
            // _settings = settings;
            var serviceConfig = GetServiceConfig(settings);
            var vsockAddress = settings.GetVsockAddress();

            GrpcChannel channel;
            if (vsockAddress is not null)
            {
                channel = Vsock.CreateChannel(vsockAddress, serviceConfig);
            }
            else
            {
                channel = GrpcChannel.ForAddress(settings.Endpoint, new() { ServiceConfig = serviceConfig });
            }

            _channel?.Dispose();
            _channel = channel;
            Reset(settings.Name, new SecureSign.SecureSignClient(_channel));
        }

        /// <summary>
        /// Get account status command
        /// </summary>
        /// <param name="hexPublicKey">The hex public key, compressed or uncompressed</param>
        [ConsoleCommand("get account status", Category = "Signer Commands", Description = "Get account status")]
        public void AccountStatusCommand(string hexPublicKey)
        {
            if (_client is null)
            {
                ConsoleHelper.Error("No signer service is connected");
                return;
            }

            try
            {
                var publicKey = ECPoint.DecodePoint(hexPublicKey.HexToBytes(), ECCurve.Secp256r1);
                var output = _client.GetAccountStatus(new()
                {
                    PublicKey = ByteString.CopyFrom(publicKey.EncodePoint(true))
                });
                ConsoleHelper.Info($"Account status: {output.Status}");
            }
            catch (RpcException rpcEx)
            {
                var message = rpcEx.StatusCode == StatusCode.Unavailable ?
                    "No available signer service" :
                    $"Failed to get account status: {rpcEx.StatusCode}: {rpcEx.Status.Detail}";
                ConsoleHelper.Error(message);
            }
            catch (FormatException formatEx)
            {
                ConsoleHelper.Error($"Invalid public key: {formatEx.Message}");
            }
        }

        private AccountStatus GetAccountStatus(ECPoint publicKey)
        {
            if (_client is null) throw new SignException("No signer service is connected");

            try
            {
                var output = _client.GetAccountStatus(new()
                {
                    PublicKey = ByteString.CopyFrom(publicKey.EncodePoint(true))
                });
                return output.Status;
            }
            catch (RpcException ex)
            {
                throw new SignException($"Get account status: {ex.Status}", ex);
            }
        }

        /// <summary>
        /// Check if the account is signable
        /// </summary>
        /// <param name="publicKey">The public key</param>
        /// <returns>True if the account is signable, false otherwise</returns>
        /// <exception cref="SignException">If no signer service is available, or other rpc error occurs.</exception>
        public bool ContainsSignable(ECPoint publicKey)
        {
            var status = GetAccountStatus(publicKey);
            return status == AccountStatus.Single || status == AccountStatus.Multiple;
        }

        private static bool TryDecodePublicKey(ByteString publicKey, [NotNullWhen(true)] out ECPoint? point)
        {
            try
            {
                point = ECPoint.DecodePoint(publicKey.Span, ECCurve.Secp256r1);
            }
            catch (FormatException) // add log later
            {
                point = null;
            }
            return point is not null;
        }

        internal Witness[] SignContext(ContractParametersContext context, IEnumerable<AccountSigns> signs)
        {
            var succeed = false;
            foreach (var (accountSigns, scriptHash) in signs.Zip(context.ScriptHashes))
            {
                var accountStatus = accountSigns.Status;
                if (accountStatus == AccountStatus.NoSuchAccount || accountStatus == AccountStatus.NoPrivateKey)
                {
                    succeed |= context.AddWithScriptHash(scriptHash); // Same as Wallet.Sign(context)
                    continue;
                }

                var contract = accountSigns.Contract;
                var accountContract = Contract.Create(
                    contract?.Parameters?.Select(p => (ContractParameterType)p).ToArray() ?? [],
                    contract?.Script?.ToByteArray() ?? []);
                if (accountStatus == AccountStatus.Multiple)
                {
                    if (!IsMultiSigContract(accountContract.Script, out int m, out ECPoint[] publicKeys))
                        throw new SignException("Sign context: multi-sign account but not multi-sign contract");

                    foreach (var sign in accountSigns.Signs)
                    {
                        if (!TryDecodePublicKey(sign.PublicKey, out var publicKey)) continue;

                        if (!publicKeys.Contains(publicKey))
                            throw new SignException($"Sign context: public key {publicKey} not in multi-sign contract");

                        var ok = context.AddSignature(accountContract, publicKey, sign.Signature.ToByteArray());
                        if (ok) m--;

                        succeed |= ok;
                        if (context.Completed || m <= 0) break;
                    }
                }
                else if (accountStatus == AccountStatus.Single)
                {
                    if (accountSigns.Signs is null || accountSigns.Signs.Count != 1)
                        throw new SignException($"Sign context: single account but {accountSigns.Signs?.Count} signs");

                    var sign = accountSigns.Signs[0];
                    if (!TryDecodePublicKey(sign.PublicKey, out var publicKey)) continue;
                    succeed |= context.AddSignature(accountContract, publicKey, sign.Signature.ToByteArray());
                }
            }

            if (!succeed) throw new SignException("Sign context: failed to sign");
            return context.GetWitnesses();
        }

        /// <summary>
        /// Signs the <see cref="ExtensiblePayload"/> with the signer.
        /// </summary>
        /// <param name="payload">The payload to sign</param>
        /// <param name="dataCache">The data cache</param>
        /// <param name="network">The network</param>
        /// <returns>The witnesses</returns>
        /// <exception cref="SignException">If no signer service is available, or other rpc error occurs.</exception>
        public Witness SignExtensiblePayload(ExtensiblePayload payload, DataCache dataCache, uint network)
        {
            if (_client is null) throw new SignException("No signer service is connected");

            try
            {
                var context = new ContractParametersContext(dataCache, payload, network);
                var output = _client.SignExtensiblePayload(new()
                {
                    Payload = new()
                    {
                        Category = payload.Category,
                        ValidBlockStart = payload.ValidBlockStart,
                        ValidBlockEnd = payload.ValidBlockEnd,
                        Sender = ByteString.CopyFrom(payload.Sender.GetSpan()),
                        Data = ByteString.CopyFrom(payload.Data.Span),
                    },
                    ScriptHashes = { context.ScriptHashes.Select(h160 => ByteString.CopyFrom(h160.GetSpan())) },
                    Network = network,
                });

                int signCount = output.Signs.Count, hashCount = context.ScriptHashes.Count;
                if (signCount != hashCount)
                {
                    throw new SignException($"Sign context: Signs.Count({signCount}) != Hashes.Count({hashCount})");
                }

                return SignContext(context, output.Signs)[0];
            }
            catch (RpcException ex)
            {
                throw new SignException($"Sign context: {ex.Status}", ex);
            }
        }

        /// <summary>
        /// Signs the specified data with the corresponding private key of the specified public key.
        /// </summary>
        /// <param name="block">The block to sign</param>
        /// <param name="publicKey">The public key</param>
        /// <param name="network">The network</param>
        /// <returns>The signature</returns>
        /// <exception cref="SignException">If no signer service is available, or other rpc error occurs.</exception>
        public ReadOnlyMemory<byte> SignBlock(Block block, ECPoint publicKey, uint network)
        {
            if (_client is null) throw new SignException("No signer service is connected");

            try
            {
                var output = _client.SignBlock(new()
                {
                    Block = new()
                    {
                        Header = new()
                        {
                            Version = block.Version,
                            PrevHash = ByteString.CopyFrom(block.PrevHash.GetSpan()),
                            MerkleRoot = ByteString.CopyFrom(block.MerkleRoot.GetSpan()),
                            Timestamp = block.Timestamp,
                            Nonce = block.Nonce,
                            Index = block.Index,
                            PrimaryIndex = block.PrimaryIndex,
                            NextConsensus = ByteString.CopyFrom(block.NextConsensus.GetSpan()),
                        },
                        TxHashes = { block.Transactions.Select(tx => ByteString.CopyFrom(tx.Hash.GetSpan())) },
                    },
                    PublicKey = ByteString.CopyFrom(publicKey.EncodePoint(true)),
                    Network = network,
                });

                return output.Signature.Memory;
            }
            catch (RpcException ex)
            {
                throw new SignException($"Sign with public key: {ex.Status}", ex);
            }
        }

        /// <inheritdoc/>
        protected override void Configure()
        {
            var config = GetConfiguration();
            if (config is not null) Reset(new Settings(config));
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Reset(string.Empty, null);
            _channel?.Dispose();
            base.Dispose();
        }
    }
}
