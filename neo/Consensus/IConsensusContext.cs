using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Consensus
{
    public interface IConsensusContext : IDisposable
    {
        //public const uint Version = 0;
        ConsensusState State {get; set;}
        UInt256 PrevHash {get; set;}
        uint BlockIndex {get; set;}
        byte ViewNumber {get; set;}
        ECPoint[] Validators {get;}
        int MyIndex {get; set;}
        uint PrimaryIndex {get; set;}
        uint Timestamp {get; set;}
        ulong Nonce {get; set;}
        UInt160 NextConsensus {get; set;}
        UInt256[] TransactionHashes {get; set;}
        Dictionary<UInt256, Transaction> Transactions {get; set;}
        byte[][] Signatures {get; set;}
        byte[] ExpectedView {get; set;}

        int M {get;}

        uint SnapshotHeight {get;}

        Header SnapshotHeader {get;}

        bool RejectTx(Transaction tx, bool verify);

        void ChangeView(byte view_number);

        Block CreateBlock();

        //void Dispose();

        uint GetPrimaryIndex(byte view_number);

        ConsensusPayload MakeChangeView();

        Block MakeHeader();

        void SignHeader();

        ConsensusPayload MakePrepareRequest();

        ConsensusPayload MakePrepareResponse(byte[] signature);

        void Reset();

        void Fill();

        bool VerifyRequest();
    }
}
