using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using Neo.Persistence;

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
        byte[][] PreparationWitnessInvocationScripts { get; set; }
        uint[] PreparationTimestamps { get; set; }
        byte[][] Commits { get; set; }
        byte[] ExpectedView { get; set; }
        byte[][] ChangeViewWitnessInvocationScripts { get; set; }
        uint[] ChangeViewTimestamps { get; set; }
        byte[] OriginalChangeViewNumbers { get; set; }

        int F { get; }
        int M { get; }

        Header PrevHeader { get; }

        bool TransactionExists(UInt256 hash);
        bool VerifyTransaction(Transaction tx);

        Block CreateBlock();

        //void Dispose();

        uint GetPrimaryIndex(byte viewNumber);

        ConsensusPayload MakeChangeView();

        ConsensusPayload MakeCommit();

        Block MakeHeader();

        ConsensusPayload MakePrepareRequest();

        ConsensusPayload MakeRecoveryMessage();

        ConsensusPayload MakePrepareResponse(UInt256 preparation);

        void Reset(byte viewNumber, Snapshot newSnapshot=null);

        void Fill();

        bool VerifyRequest();
    }
}
