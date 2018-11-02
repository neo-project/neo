using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Ledger;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using Neo.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests
{

    // Testing signature of transactions and blocks

    [TestClass]
    public class UT_SignTransactionsBlocks
    {
        Block uut;  // no transaction
        Block uut2; // includes transaction
        NEP6SimpleWallet uut_wallet;

        [TestInitialize]
        public void TestSetup()
        {
            ulong nonce = 42;
            List<Transaction> transactions = new List<Transaction>();
            MinerTransaction tx = new MinerTransaction
            {
                Nonce = (uint)(nonce % (uint.MaxValue + 1ul)),
                Attributes = new TransactionAttribute[0],
                Inputs = new CoinReference[0],
                Outputs = new TransactionOutput[0],
                Witnesses = new Witness[0]
            };
            JObject jObj = tx.ToJson();
            jObj.Should().NotBeNull();
            jObj["txid"].AsString().Should().Be("0xe42ca5744eda6de2e1a2bdc2ed98fa7b967b13cd3aa2605c95fff37261f07ef6");
            transactions.Insert(0, tx);
            UInt256[] TransactionHashes = transactions.Select(p => p.Hash).ToArray();

            uut = new Block
            {
                Version = 0,
                PrevHash = new UInt256(Enumerable.Repeat((byte)0x01, 32).ToArray()),
                MerkleRoot = MerkleTree.ComputeRoot(TransactionHashes),
                Timestamp = 9,
                Index = 3,
                ConsensusData = nonce,
                NextConsensus = new UInt160(Enumerable.Repeat((byte)0x02, 20).ToArray()),
                Transactions = new Transaction[0] // DOES NOT INCLUDE TRANSACTIONS
            };
            uut.MerkleRoot.ToString().Should().Be("0xe42ca5744eda6de2e1a2bdc2ed98fa7b967b13cd3aa2605c95fff37261f07ef6");

            uut2 = new Block
            {
                Version = 0,
                PrevHash = new UInt256(Enumerable.Repeat((byte)0x01, 32).ToArray()),
                MerkleRoot = MerkleTree.ComputeRoot(TransactionHashes),
                Timestamp = 9,
                Index = 3,
                ConsensusData = nonce,
                NextConsensus = new UInt160(Enumerable.Repeat((byte)0x02, 20).ToArray()),
                Transactions = new Transaction[]{tx}  // INCLUDES TRANSACTION
            };

            string sjson = @"{""name"":""wallet1"",""version"":""1.0"",""scrypt"":{""n"":16384,""r"":8,""p"":8},""accounts"":[{""address"":""AKkkumHbBipZ46UMZJoFynJMXzSRnBvKcs"",""label"":null,""isDefault"":false,""lock"":false,""key"":""6PYLmjBYJ4wQTCEfqvnznGJwZeW9pfUcV5m5oreHxqryUgqKpTRAFt9L8Y"",""contract"":{""script"":""2102b3622bf4017bdfe317c58aed5f4c753f206b7db896046fa7d774bbc4bf7f8dc2ac"",""parameters"":[{""name"":""parameter0"",""type"":""Signature""}],""deployed"":false},""extra"":null},{""address"":""AZ81H31DMWzbSnFDLFkzh9vHwaDLayV7fU"",""label"":null,""isDefault"":false,""lock"":false,""key"":""6PYLmjBYJ4wQTCEfqvnznGJwZeW9pfUcV5m5oreHxqryUgqKpTRAFt9L8Y"",""contract"":{""script"":""532102103a7f7dd016558597f7960d27c516a4394fd968b9e65155eb4b013e4040406e2102a7bc55fe8684e0119768d104ba30795bdcc86619e864add26156723ed185cd622102b3622bf4017bdfe317c58aed5f4c753f206b7db896046fa7d774bbc4bf7f8dc22103d90c07df63e690ce77912e10ab51acc944b66860237b608c4f8f8309e71ee69954ae"",""parameters"":[{""name"":""parameter0"",""type"":""Signature""},{""name"":""parameter1"",""type"":""Signature""},{""name"":""parameter2"",""type"":""Signature""}],""deployed"":false},""extra"":null}],""extra"":null}";
            uut_wallet = new NEP6SimpleWallet(sjson);
            string password = "one";
            uut_wallet.Unlock(password);
        }

        [TestMethod]
        public void GetBlockHeaderNoTransaction()
        {
            // this block do not transactions
            uut.Transactions.Length.Should().Be(0);
            byte[] BlockHeaderHashData = uut.GetHashData();
            string hex = BitConverter.ToString(BlockHeaderHashData);
            hex.Should().Be("00-00-00-00-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-F6-7E-F0-61-72-F3-FF-95-5C-60-A2-3A-CD-13-7B-96-7B-FA-98-ED-C2-BD-A2-E1-E2-6D-DA-4E-74-A5-2C-E4-09-00-00-00-03-00-00-00-2A-00-00-00-00-00-00-00-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02");
        }

        [TestMethod]
        public void GetBlockHeaderWithTransaction()
        {
            // this block includes transaction, but HashData is the same (only header)
            uut2.Transactions.Length.Should().Be(1);
            byte[] BlockHeaderHashData = uut2.GetHashData();
            string hex = BitConverter.ToString(BlockHeaderHashData);
            hex.Should().Be("00-00-00-00-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-01-F6-7E-F0-61-72-F3-FF-95-5C-60-A2-3A-CD-13-7B-96-7B-FA-98-ED-C2-BD-A2-E1-E2-6D-DA-4E-74-A5-2C-E4-09-00-00-00-03-00-00-00-2A-00-00-00-00-00-00-00-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02-02");
        }

        [TestMethod]
        public void SignVerifyBlockHeader()
        {
            byte[] BlockHeaderHashData = uut.GetHashData();

            byte[] scripthash1 = new byte[]{0x2b,0xaa,0x76,0xad,0x53,0x4b,0x88,0x6c,0xb8,0x7c,0x6b,0x37,0x20,0xa3,0x49,0x43,0xd9,0x00,0x0f,0xa9};
            uut_wallet.GetAccount(new UInt160(scripthash1)).Address.Should().Be("AKkkumHbBipZ46UMZJoFynJMXzSRnBvKcs");

            WalletAccount account = uut_wallet.GetAccount(new UInt160(scripthash1));
            KeyPair key = account.GetKey();
            key.ToString().Should().Be("02b3622bf4017bdfe317c58aed5f4c753f206b7db896046fa7d774bbc4bf7f8dc2");
            byte[] priv = key.PrivateKey;
            byte[] pub  = key.PublicKey.EncodePoint(false).Skip(1).ToArray();

            string hexpriv = BitConverter.ToString(priv);
            string hexpub  = BitConverter.ToString(pub);
            hexpriv.Should().Be("34-7B-C2-BD-9E-B7-B9-F4-1A-21-7A-26-DC-5A-3D-2A-3C-25-EC-E1-C8-BF-F1-D5-A1-46-AA-F4-15-6E-34-36");
            hexpub.Should().Be("B3-62-2B-F4-01-7B-DF-E3-17-C5-8A-ED-5F-4C-75-3F-20-6B-7D-B8-96-04-6F-A7-D7-74-BB-C4-BF-7F-8D-C2-AF-9C-7B-29-75-9D-F7-F3-D9-20-52-A5-B9-BC-54-5B-CD-31-C6-A7-A3-46-3E-90-C7-68-A6-C3-E4-5B-10-36");

            byte[] sig1 = uut.Sign(key); // each signature is different... so cannot check against known value
            Crypto.Default.VerifySignature(BlockHeaderHashData, sig1, key.PublicKey.EncodePoint(false)).Should().Be(true);
        }
    }
}
