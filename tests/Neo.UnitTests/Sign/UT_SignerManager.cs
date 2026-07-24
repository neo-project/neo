// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SignerManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Sign;
using System;

namespace Neo.UnitTests.Sign
{
    [TestClass]
    public class UT_SignerManager
    {
        private sealed class StubSigner : ISigner
        {
            public Witness SignExtensiblePayload(ExtensiblePayload payload, DataCache snapshot, uint network)
                => throw new NotImplementedException();

            public ReadOnlyMemory<byte> SignBlock(Block block, ECPoint publicKey, uint network)
                => throw new NotImplementedException();

            public bool ContainsSignable(ECPoint publicKey) => false;
        }

        [TestMethod]
        public void Register_Get_Unregister_RoundTrip()
        {
            const string name = "ut-signer-manager-a";
            var signer = new StubSigner();
            try
            {
                SignerManager.RegisterSigner(name, signer);
                Assert.AreSame(signer, SignerManager.GetSignerOrDefault(name));
                Assert.IsTrue(SignerManager.UnregisterSigner(name));
                Assert.IsNull(SignerManager.GetSignerOrDefault(name));
                Assert.IsFalse(SignerManager.UnregisterSigner(name));
            }
            finally
            {
                SignerManager.UnregisterSigner(name);
            }
        }

        [TestMethod]
        public void RegisterSigner_InvalidArgs_Throw()
        {
            Assert.ThrowsExactly<ArgumentException>(() => SignerManager.RegisterSigner("", new StubSigner()));
            Assert.ThrowsExactly<ArgumentException>(() => SignerManager.RegisterSigner(null, new StubSigner()));
            Assert.ThrowsExactly<ArgumentNullException>(() => SignerManager.RegisterSigner("ut-signer-null", null));
        }

        [TestMethod]
        public void RegisterSigner_DuplicateName_Throws()
        {
            const string name = "ut-signer-manager-dup";
            try
            {
                SignerManager.RegisterSigner(name, new StubSigner());
                Assert.ThrowsExactly<InvalidOperationException>(() => SignerManager.RegisterSigner(name, new StubSigner()));
            }
            finally
            {
                SignerManager.UnregisterSigner(name);
            }
        }

        [TestMethod]
        public void GetSignerOrDefault_EmptyName_WithoutSingleSigner_ReturnsNull()
        {
            // Empty name returns the only registered signer when Count == 1; otherwise null.
            // SignerManager is process-wide, so establish "multiple signers" with unique names
            // rather than assuming the global dictionary is empty or already multi-valued.
            const string nameA = "ut-signer-manager-empty-a";
            const string nameB = "ut-signer-manager-empty-b";
            try
            {
                SignerManager.RegisterSigner(nameA, new StubSigner());
                SignerManager.RegisterSigner(nameB, new StubSigner());
                Assert.IsNull(SignerManager.GetSignerOrDefault(""));
            }
            finally
            {
                SignerManager.UnregisterSigner(nameA);
                SignerManager.UnregisterSigner(nameB);
            }
        }

        [TestMethod]
        public void UnregisterSigner_EmptyName_ReturnsFalse()
        {
            Assert.IsFalse(SignerManager.UnregisterSigner(""));
            Assert.IsFalse(SignerManager.UnregisterSigner(null));
        }
    }
}
