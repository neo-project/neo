using AntShares.Algebra;
using AntShares.Core;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.Network;
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
        public Dictionary<Secp256r1Point, UInt256> Nonces;
        public Dictionary<Secp256r1Point, UInt256> NonceHashes;
        public Dictionary<Secp256r1Point, List<FiniteFieldPoint>> NoncePieces;
        public List<UInt256> TransactionHashes;

        private Secp256r1Point my_pubkey;

        private BlockConsensusContext(Secp256r1Point my_pubkey)
        {
            this.my_pubkey = my_pubkey;
            this.Nonces = new Dictionary<Secp256r1Point, UInt256>();
            this.NonceHashes = new Dictionary<Secp256r1Point, UInt256>();
            this.NoncePieces = new Dictionary<Secp256r1Point, List<FiniteFieldPoint>>();
            this.TransactionHashes = new List<UInt256>();
        }

        public static BlockConsensusContext Create(Secp256r1Point my_pubkey)
        {
            Random rand = new Random();
            byte[] nonce = new byte[32];
            rand.NextBytes(nonce);
            BlockConsensusContext context = new BlockConsensusContext(my_pubkey);
            context.PrevHash = Blockchain.Default.CurrentBlockHash;
            context.Miners = Blockchain.Default.GetMiners().ToArray();
            if (!context.Miners.Contains(my_pubkey)) return null;
            context.Nonces.Add(my_pubkey, new UInt256(nonce));
            context.NonceHashes.Add(my_pubkey, new UInt256(nonce.Sha256()));
            context.TransactionHashes.AddRange(LocalNode.GetMemoryPool().Select(p => p.Hash));
            return context;
        }

        public BlockConsensusRequest CreateRequest(MinerWallet wallet)
        {
            BlockConsensusRequest request = new BlockConsensusRequest
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
            return request;
        }
    }
}
