// Copyright (C) 2015-2025 The Neo Project.
//
// UT_SignClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.Sign;
using Neo.SmartContract;
using Neo.Wallets;
using Servicepb;
using Signpb;

namespace Neo.Plugins.SignClient.Tests
{
    [TestClass]
    public class UT_SignClient
    {
        const string PrivateKey = "0101010101010101010101010101010101010101010101010101010101010101";
        const string PublicKey = "026ff03b949241ce1dadd43519e6960e0a85b41a69a05c328103aa2bce1594ca16";
        const uint TestNetwork = 0x334F454Eu;

        private static readonly ECPoint s_publicKey = ECPoint.DecodePoint(PublicKey.HexToBytes(), ECCurve.Secp256r1);

        private static SignClient NewClient()
        {
            // for test sign service, set SIGN_SERVICE_ENDPOINT env
            var endPoint = Environment.GetEnvironmentVariable("SIGN_SERVICE_ENDPOINT");
            if (endPoint is not null)
            {
                var section = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [Settings.SectionName + ":Name"] = "SignClient",
                        [Settings.SectionName + ":EndPoint"] = endPoint
                    })
                    .Build()
                    .GetSection(Settings.SectionName);
                return new SignClient(new Settings(section));
            }

            var mockClient = new Mock<SecureSign.SecureSignClient>();

            // setup GetAccountStatus
            mockClient.Setup(c => c.GetAccountStatus(
                    It.IsAny<GetAccountStatusRequest>(),
                    It.IsAny<Metadata?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>())
                )
                .Returns<GetAccountStatusRequest, Metadata?, DateTime?, CancellationToken>((req, _, _, _) =>
                {
                    if (req.PublicKey.ToByteArray().ToHexString() == PublicKey)
                        return new GetAccountStatusResponse() { Status = AccountStatus.Single };
                    return new GetAccountStatusResponse() { Status = AccountStatus.NoSuchAccount };
                });

            // setup SignWithPublicKey
            mockClient.Setup(c => c.SignWithPublicKey(
                    It.IsAny<SignWithPublicKeyRequest>(),
                    It.IsAny<Metadata?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>())
                )
                .Returns<SignWithPublicKeyRequest, Metadata?, DateTime?, CancellationToken>((req, _, _, _) =>
                {
                    if (req.PublicKey.ToByteArray().ToHexString() == PublicKey)
                    {
                        var sign = Crypto.Sign(req.SignData.ToByteArray(), PrivateKey.HexToBytes(), ECCurve.Secp256r1);
                        return new SignWithPublicKeyResponse() { Signature = ByteString.CopyFrom(sign) };
                    }
                    throw new RpcException(new Status(StatusCode.NotFound, "no such account"));
                });

            // setup SignWithScriptHashes
            mockClient.Setup(c => c.SignWithScriptHashes(
                    It.IsAny<SignWithScriptHashesRequest>(),
                    It.IsAny<Metadata?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>())
                )
                .Returns<SignWithScriptHashesRequest, Metadata?, DateTime?, CancellationToken>((req, _, _, _) =>
                {
                    var script = Contract.CreateSignatureRedeemScript(s_publicKey);
                    var res = new SignWithScriptHashesResponse();
                    foreach (var scriptHash in req.ScriptHashes)
                    {
                        if (scriptHash.ToByteArray().ToHexString() == script.ToScriptHash().GetSpan().ToHexString())
                        {
                            var contract = new AccountContract() { Script = ByteString.CopyFrom(script) };
                            contract.Parameters.Add((uint)ContractParameterType.Signature);

                            var sign = Crypto.Sign(req.SignData.ToByteArray(), PrivateKey.HexToBytes(), ECCurve.Secp256r1);
                            var signs = new AccountSigns() { Status = AccountStatus.Single, Contract = contract };
                            signs.Signs.Add(new AccountSign()
                            {
                                PublicKey = ByteString.CopyFrom(s_publicKey.EncodePoint(false).ToArray()),
                                Signature = ByteString.CopyFrom(sign)
                            });

                            res.Signs.Add(signs);
                        }
                        else
                        {
                            res.Signs.Add(new AccountSigns() { Status = AccountStatus.NoSuchAccount });
                        }
                    }
                    return res;
                });

            return new SignClient("TestSignClient", mockClient.Object);
        }

        [TestMethod]
        public void TestSignPublicKey()
        {
            using var signClient = NewClient();

            // sign with public key
            var signature = signClient.Sign([1, 2, 3], s_publicKey);
            Assert.IsNotNull(signature);

            // verify signature
            var verified = Crypto.VerifySignature([1, 2, 3], signature.Span, s_publicKey);
            Assert.IsTrue(verified);

            var privateKey = Enumerable.Repeat((byte)0x0f, 32).ToArray();
            var keypair = new KeyPair(privateKey);

            // sign with a not exists private key
            var action = () => { _ = signClient.Sign([1, 2, 3], keypair.PublicKey); };
            Assert.ThrowsExactly<SignException>(action);
        }

        [TestMethod]
        public void TestSignContext()
        {
            using var signClient = NewClient();
            using var store = new MemoryStore();
            using var snapshot = new StoreCache(store, false);

            // get account of the public key
            var script = Contract.CreateSignatureRedeemScript(s_publicKey);
            var tx = new Transaction
            {
                Script = script,
                Signers = [new() { Account = script.ToScriptHash() }],
                Attributes = [],
                Witnesses = [Witness.Empty],
            };

            var context = new ContractParametersContext(snapshot, tx, TestNetwork);
            var ok = signClient.Sign(context);
            Assert.IsTrue(ok);
        }

        [TestMethod]
        public void TestGetAccountStatus()
        {
            using var signClient = NewClient();

            // exists
            var contains = signClient.ContainsSignable(s_publicKey);
            Assert.IsTrue(contains);

            var privateKey = Enumerable.Repeat((byte)0x0f, 32).ToArray();
            var keypair = new KeyPair(privateKey);

            // not exists
            contains = signClient.ContainsSignable(keypair.PublicKey);
            Assert.IsFalse(contains);

            // exists
            signClient.AccountStatusCommand(PublicKey);

            // not exists
            signClient.AccountStatusCommand(keypair.PublicKey.EncodePoint(true).ToHexString());
        }
    }
}
