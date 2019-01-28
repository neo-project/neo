using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;

namespace Neo.Consensus
{
    public interface IConsensusContext : IDisposable, ISerializable
    {
        //public const uint Version = 0;
        ConsensusState State { get; set; }
        UInt256 PrevHash { get; }
        uint BlockIndex { get; }
        byte ViewNumber { get; }
        ECPoint[] Validators { get; }
        int MyIndex { get; }
        uint PrimaryIndex { get; }
        uint Timestamp { get; set; }
        ulong Nonce { get; set; }
        UInt160 NextConsensus { get; set; }
        UInt256[] TransactionHashes { get; set; }
        Dictionary<UInt256, Transaction> Transactions { get; set; }
        UInt256[] Preparations { get; set; }
        byte[][] Commits { get; set; }
        byte[] ExpectedView { get; set; }

        int M { get; }

        Header PrevHeader { get; }

        bool TransactionExists(UInt256 hash);
        bool VerifyTransaction(Transaction tx);

        Block CreateBlock();

        //void Dispose();

        uint GetPrimaryIndex(byte view_number);

        ConsensusPayload MakeChangeView();

        ConsensusPayload MakeCommit();

        Block MakeHeader();

        ConsensusPayload MakePrepareRequest();

        ConsensusPayload MakePrepareResponse(UInt256 preparation);

        void Reset(byte view_number);

        void Fill();

        bool VerifyRequest();
    }
}
