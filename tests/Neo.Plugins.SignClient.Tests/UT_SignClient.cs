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
using Moq;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.Sign;
using Neo.SmartContract;
using Neo.UnitTests;
using Neo.Wallets;
using Servicepb;
using Signpb;

using ExtensiblePayload = Neo.Network.P2P.Payloads.ExtensiblePayload;

namespace Neo.Plugins.SignClient.Tests
{
    [TestClass]
    public class UT_SignClient
    {
        const string PrivateKey = "0101010101010101010101010101010101010101010101010101010101010101";
        const string PublicKey = "026ff03b949241ce1dadd43519e6960e0a85b41a69a05c328103aa2bce1594ca16";

        private static readonly uint s_testNetwork = TestProtocolSettings.Default.Network;

        private static readonly ECPoint s_publicKey = ECPoint.DecodePoint(PublicKey.HexToBytes(), ECCurve.Secp256r1);

        private static SignClient NewClient(Block? block, ExtensiblePayload? payload)
        {
            // When test sepcific endpoint, set SIGN_SERVICE_ENDPOINT
            // For example:
            // export SIGN_SERVICE_ENDPOINT=http://127.0.0.1:9991
            // or
            // export SIGN_SERVICE_ENDPOINT=vsock://2345:9991
            var endpoint = Environment.GetEnvironmentVariable("SIGN_SERVICE_ENDPOINT");
            if (endpoint is not null)
            {
                var section = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [SignSettings.SectionName + ":Name"] = "SignClient",
                        [SignSettings.SectionName + ":Endpoint"] = endpoint,
                    })
                    .Build()
                    .GetSection(SignSettings.SectionName);
                return new SignClient(new SignSettings(section));
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
                        return new() { Status = AccountStatus.Single };
                    return new() { Status = AccountStatus.NoSuchAccount };
                });

            // setup SignBlock
            mockClient.Setup(c => c.SignBlock(
                    It.IsAny<SignBlockRequest>(),
                    It.IsAny<Metadata?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>())
                )
                .Returns<SignBlockRequest, Metadata?, DateTime?, CancellationToken>((req, _, _, _) =>
                {
                    if (req.PublicKey.ToByteArray().ToHexString() == PublicKey)
                    {
                        var sign = Crypto.Sign(block!.GetSignData(s_testNetwork), PrivateKey.HexToBytes(), ECCurve.Secp256r1);
                        return new() { Signature = ByteString.CopyFrom(sign) };
                    }
                    throw new RpcException(new Status(StatusCode.NotFound, "no such account"));
                });

            // setup SignExtensiblePayload
            mockClient.Setup(c => c.SignExtensiblePayload(
                    It.IsAny<SignExtensiblePayloadRequest>(),
                    It.IsAny<Metadata?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>())
                )
                .Returns<SignExtensiblePayloadRequest, Metadata?, DateTime?, CancellationToken>((req, _, _, _) =>
                {
                    var script = Contract.CreateSignatureRedeemScript(s_publicKey);
                    var res = new SignExtensiblePayloadResponse();
                    foreach (var scriptHash in req.ScriptHashes)
                    {
                        if (scriptHash.ToByteArray().ToHexString() == script.ToScriptHash().GetSpan().ToHexString())
                        {
                            var contract = new AccountContract() { Script = ByteString.CopyFrom(script) };
                            contract.Parameters.Add((uint)ContractParameterType.Signature);

                            var sign = Crypto.Sign(payload!.GetSignData(s_testNetwork), PrivateKey.HexToBytes(), ECCurve.Secp256r1);
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
        public void TestSignBlock()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var block = TestUtils.MakeBlock(snapshotCache, UInt256.Zero, 0);
            using var signClient = NewClient(block, null);

            // sign with public key
            var signature = signClient.SignBlock(block, s_publicKey, s_testNetwork);
            Assert.IsNotNull(signature);

            // verify signature
            var signData = block.GetSignData(s_testNetwork);
            var verified = Crypto.VerifySignature(signData, signature.Span, s_publicKey);
            Assert.IsTrue(verified);

            var privateKey = Enumerable.Repeat((byte)0x0f, 32).ToArray();
            var keypair = new KeyPair(privateKey);

            // sign with a not exists private key
            var action = () => { _ = signClient.SignBlock(block, keypair.PublicKey, s_testNetwork); };
            Assert.ThrowsExactly<SignException>(action);
        }

        [TestMethod]
        public void TestSignExtensiblePayload()
        {
            var script = Contract.CreateSignatureRedeemScript(s_publicKey);
            var signer = script.ToScriptHash();
            var payload = new ExtensiblePayload()
            {
                Category = "test",
                ValidBlockStart = 1,
                ValidBlockEnd = 100,
                Sender = signer,
                Data = new byte[] { 1, 2, 3 },
                Witness = null!
            };
            using var signClient = NewClient(null, payload);
            using var store = new MemoryStore();
            using var snapshot = new StoreCache(store, false);

            var witness = signClient.SignExtensiblePayload(payload, snapshot, s_testNetwork);
            Assert.AreEqual(witness.VerificationScript.Span.ToHexString(), script.ToHexString());

            var signature = witness.InvocationScript[^64..].ToArray();
            var verified = Crypto.VerifySignature(payload.GetSignData(s_testNetwork), signature, s_publicKey);
            Assert.IsTrue(verified);
        }

        [TestMethod]
        public void TestGetAccountStatus()
        {
            using var signClient = NewClient(null, null);

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
