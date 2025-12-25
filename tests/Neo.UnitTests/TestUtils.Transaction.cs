// Copyright (C) 2015-2025 The Neo Project.
//
// TestUtils.Transaction.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Extensions.IO;
using Neo.Factories;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System.Numerics;

namespace Neo.UnitTests;

public partial class TestUtils
{
    public static Transaction CreateValidTx(DataCache snapshot, NEP6Wallet wallet, WalletAccount account)
    {
        return CreateValidTx(snapshot, wallet, account.ScriptHash, RandomNumberFactory.NextUInt32());
    }

    public static Transaction CreateValidTx(DataCache snapshot, NEP6Wallet wallet, UInt160 account, uint nonce)
    {
        var tx = wallet.MakeTransaction(snapshot, [
                new TransferOutput
                {
                    AssetId = NativeContract.Governance.GasTokenId,
                    ScriptHash = account,
                    Value = new BigDecimal(BigInteger.One, 8)
                }
            ],
            account);

        tx.Nonce = nonce;
        tx.Signers = [new Signer { Account = account, Scopes = WitnessScope.CalledByEntry }];
        var data = new ContractParametersContext(snapshot, tx, TestProtocolSettings.Default.Network);
        Assert.IsNull(data.GetSignatures(tx.Sender));
        Assert.IsTrue(wallet.Sign(data));
        Assert.IsTrue(data.Completed);
        Assert.HasCount(1, data.GetSignatures(tx.Sender)!);

        tx.Witnesses = data.GetWitnesses();
        return tx;
    }

    public static Transaction CreateValidTx(DataCache snapshot, NEP6Wallet wallet, UInt160 account, uint nonce, UInt256[] conflicts)
    {
        var tx = wallet.MakeTransaction(snapshot, [
                new TransferOutput
                {
                    AssetId = NativeContract.Governance.GasTokenId,
                    ScriptHash = account,
                    Value = new BigDecimal(BigInteger.One, 8)
                }
            ],
            account);
        tx.Attributes = conflicts.Select(conflict => new Conflicts { Hash = conflict }).ToArray();
        tx.Nonce = nonce;
        tx.Signers = [new Signer { Account = account, Scopes = WitnessScope.CalledByEntry }];
        var data = new ContractParametersContext(snapshot, tx, TestProtocolSettings.Default.Network);
        Assert.IsNull(data.GetSignatures(tx.Sender));
        Assert.IsTrue(wallet.Sign(data));
        Assert.IsTrue(data.Completed);
        Assert.HasCount(1, data.GetSignatures(tx.Sender)!);
        tx.Witnesses = data.GetWitnesses();
        return tx;
    }

    public static Transaction CreateRandomHashTransaction()
    {
        var randomBytes = new byte[16];
        TestRandom.NextBytes(randomBytes);
        return new Transaction
        {
            Script = randomBytes,
            Attributes = [],
            Signers = [new Signer { Account = UInt160.Zero }],
            Witnesses = [Witness.Empty],
        };
    }

    public static Transaction GetTransaction(UInt160 sender)
    {
        return new Transaction
        {
            Script = new[] { (byte)OpCode.PUSH2 },
            Attributes = [],
            Signers =
            [
                new()
                {
                    Account = sender,
                    Scopes = WitnessScope.CalledByEntry,
                    AllowedContracts = [],
                    AllowedGroups = [],
                    Rules = [],
                }
            ],
            Witnesses = [Witness.Empty],
        };
    }

    public static Transaction GetTransaction(UInt160 sender, UInt160 signer)
    {
        return new Transaction
        {
            Script = new[] { (byte)OpCode.PUSH2 },
            Attributes = [],
            Signers =
            [
                new Signer
                {
                    Account = sender,
                    Scopes = WitnessScope.CalledByEntry,
                    AllowedContracts = [],
                    AllowedGroups = [],
                    Rules = [],
                },
                new Signer
                {
                    Account = signer,
                    Scopes = WitnessScope.CalledByEntry,
                    AllowedContracts = [],
                    AllowedGroups = [],
                    Rules = [],
                }
            ],
            Witnesses =
            [
                new Witness
                {
                    InvocationScript = Memory<byte>.Empty,
                    VerificationScript = Memory<byte>.Empty,
                },
                new Witness
                {
                    InvocationScript = Memory<byte>.Empty,
                    VerificationScript = Memory<byte>.Empty,
                }
            ]
        };
    }

    public enum InvalidTransactionType
    {
        InsufficientBalance,
        InvalidSignature,
        InvalidScript,
        InvalidAttribute,
        Oversized,
        Expired,
        Conflicting
    }

    class InvalidAttribute : TransactionAttribute
    {
        public override TransactionAttributeType Type => (TransactionAttributeType)0xFF;
        public override bool AllowMultiple { get; }
        protected override void DeserializeWithoutType(ref MemoryReader reader) { }
        protected override void SerializeWithoutType(BinaryWriter writer) { }
    }

    public static void AddTransactionToBlockchain(DataCache snapshot, Transaction tx)
    {
        var block = new Block
        {
            Header = new Header
            {
                Index = NativeContract.Ledger.CurrentIndex(snapshot) + 1,
                PrevHash = NativeContract.Ledger.CurrentHash(snapshot),
                MerkleRoot = new UInt256(Crypto.Hash256(tx.Hash.ToArray())),
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS(),
                NextConsensus = UInt160.Zero,
                Witness = Witness.Empty,
            },
            Transactions = [tx]
        };

        BlocksAdd(snapshot, block.Hash, block);
    }
}
