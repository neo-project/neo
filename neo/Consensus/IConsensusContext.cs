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
using Neo.Network.P2P;

namespace Neo.Consensus
{
    public interface IConsensusContext : IDisposable
    {
        //public const uint Version = 0;
        DateTime block_received_time {get; set;}
        ConsensusState State {get; set;}
        UInt256 PrevHash {get;}
        uint BlockIndex {get;}
        byte ViewNumber {get;}
        ECPoint[] Validators {get;}
        int MyIndex {get;}
        uint PrimaryIndex {get;}
        uint Timestamp {get; set;}
        ulong Nonce {get; set;}
        UInt160 NextConsensus {get; set;}
        UInt256[] TransactionHashes {get; set;}
        Dictionary<UInt256, Transaction> Transactions {get; set;}
        byte[][] Signatures {get; set;}
        byte[] ExpectedView {get; set;}

        int M {get;}

        void LocalNodeSendDirectly(ConsensusPayload _Inventory);

        void LocalNodeRelay(Block _Inventory);

        void LocalNodeTellMessage(Message message);

        void RestartTasks(UInt256[] hashes);

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

        DateTime GetUtcNow();
    }
}
