using AntShares.Algebra;
using AntShares.Core;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace AntShares.Miner
{
    internal class BlockConsensusContext
    {
        public UInt256 PrevHash;
        public Secp256r1Point[] Miners;
        public readonly Dictionary<Secp256r1Point, UInt256> Nonces = new Dictionary<Secp256r1Point, UInt256>();
        public readonly Dictionary<Secp256r1Point, UInt256> NonceHashes = new Dictionary<Secp256r1Point, UInt256>();
        public readonly Dictionary<Secp256r1Point, List<FiniteFieldPoint>> NoncePieces = new Dictionary<Secp256r1Point, List<FiniteFieldPoint>>();
        public readonly List<UInt256> TransactionHashes = new List<UInt256>();

        public readonly object SyncRoot = new object();
        private readonly Secp256r1Point my_pubkey;

        private BlockConsensusRequest request;
        private int index_me = -1;

        public bool Valid => index_me >= 0;

        public BlockConsensusContext(Secp256r1Point my_pubkey)
        {
            this.my_pubkey = my_pubkey;
            Reset();
        }

        public BlockConsensusRequest CreateRequest(MinerWallet wallet)
        {
            lock (SyncRoot)
            {
                if (!Valid) throw new InvalidOperationException();
                if (request == null)
                {
                    request = new BlockConsensusRequest
                    {
                        PrevHash = PrevHash,
                        NonceHash = NonceHashes[my_pubkey],
                        TransactionHashes = TransactionHashes.ToArray()
                    };
                    SplitSecret secret = SecretSharing.Split(Nonces[my_pubkey].ToArray(), (Miners.Length - 1) / 2 + 1);
                    for (int i = 0; i < Miners.Length; i++)
                    {
                        if (Miners[i].Equals(my_pubkey)) continue;
                        byte[] aeskey = wallet.GetAesKey(Miners[i]);
                        byte[] iv = aeskey.Take(16).ToArray();
                        byte[] piece = secret.GetShare(i + 1).ToArray();
                        using (AesManaged aes = new AesManaged())
                        using (ICryptoTransform encryptor = aes.CreateEncryptor(aeskey, iv))
                        {
                            piece = encryptor.TransformFinalBlock(piece, 0, piece.Length);
                        }
                        Array.Clear(aeskey, 0, aeskey.Length);
                        Array.Clear(iv, 0, iv.Length);
                        request.NoncePieces.Add(Miners[i], piece);
                    }
                }
                return request;
            }
        }

        public void Reset()
        {
            if (PrevHash == Blockchain.Default.CurrentBlockHash)
                return;
            Random rand = new Random();
            byte[] nonce = new byte[32];
            rand.NextBytes(nonce);
            lock (SyncRoot)
            {
                PrevHash = Blockchain.Default.CurrentBlockHash;
                Miners = Blockchain.Default.GetMiners().ToArray();
                Nonces.Clear();
                Nonces.Add(my_pubkey, new UInt256(nonce));
                NonceHashes.Clear();
                NonceHashes.Add(my_pubkey, new UInt256(nonce.Sha256()));
                NoncePieces.Clear();
                TransactionHashes.Clear();
                TransactionHashes.AddRange(Blockchain.Default.GetMemoryPool().Select(p => p.Hash));
                request = null;
                index_me = Array.IndexOf(Miners, my_pubkey);
            }
        }
    }
}
