using AntShares.Algebra;
using AntShares.Core;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.Threading;
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
        public readonly HashSet<UInt256> TransactionHashes = new HashSet<UInt256>();

        public readonly object SyncRoot = new object();
        private readonly Secp256r1Point my_pubkey;
        private readonly TimeoutAction action_request;
        private int index_me = -1;

        public bool Valid => index_me >= 0;

        public BlockConsensusContext(Secp256r1Point my_pubkey)
        {
            this.my_pubkey = my_pubkey;
            Reset();
            this.action_request = new TimeoutAction(CreateResponse, TimeSpan.FromSeconds(Blockchain.SecondsPerBlock), () => NoncePieces.Count > Miners.Length * (2.0 / 3.0), () => NoncePieces.Count == Miners.Length - 1);
        }

        public void AddRequest(BlockConsensusRequest request, MinerWallet wallet)
        {
            lock (SyncRoot)
            {
                if (request.PrevHash != PrevHash) return;
                if (request.Miner == my_pubkey) return;
                if (NoncePieces.ContainsKey(request.Miner)) return;
                byte[] aeskey = wallet.GetAesKey(request.Miner);
                byte[] piece = request.NoncePieces[my_pubkey];
                using (AesManaged aes = new AesManaged())
                using (ICryptoTransform decryptor = aes.CreateDecryptor(aeskey, request.IV))
                {
                    piece = decryptor.TransformFinalBlock(piece, 0, piece.Length);
                }
                Array.Clear(aeskey, 0, aeskey.Length);
                NoncePieces.Add(request.Miner, new List<FiniteFieldPoint>());
                NoncePieces[request.Miner].Add(FiniteFieldPoint.DeserializeFrom(piece));
                NonceHashes.Add(request.Miner, request.NonceHash);
                TransactionHashes.UnionWith(request.TransactionHashes);
                action_request.CheckPredicate();
            }
        }

        private BlockConsensusRequest _request = null;
        public BlockConsensusRequest CreateRequest(MinerWallet wallet)
        {
            lock (SyncRoot)
            {
                if (!Valid) throw new InvalidOperationException();
                if (_request == null)
                {
                    _request = new BlockConsensusRequest
                    {
                        PrevHash = PrevHash,
                        Miner = my_pubkey,
                        IV = new byte[16],
                        NonceHash = NonceHashes[my_pubkey],
                        TransactionHashes = TransactionHashes.ToArray()
                    };
                    Random rand = new Random();
                    rand.NextBytes(_request.IV);
                    SplitSecret secret = SecretSharing.Split(Nonces[my_pubkey].ToArray(), (Miners.Length - 1) / 2 + 1);
                    for (int i = 0; i < Miners.Length; i++)
                    {
                        if (Miners[i].Equals(my_pubkey)) continue;
                        byte[] aeskey = wallet.GetAesKey(Miners[i]);
                        byte[] piece = secret.GetShare(i + 1).ToArray();
                        using (AesManaged aes = new AesManaged())
                        using (ICryptoTransform encryptor = aes.CreateEncryptor(aeskey, _request.IV))
                        {
                            piece = encryptor.TransformFinalBlock(piece, 0, piece.Length);
                        }
                        Array.Clear(aeskey, 0, aeskey.Length);
                        _request.NoncePieces.Add(Miners[i], piece);
                    }
                }
                return _request;
            }
        }

        private void CreateResponse()
        {
            //TODO: 组合所有其他矿工的共识数据并广播
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
                Miners = Blockchain.Default.GetMiners();
                Nonces.Clear();
                Nonces.Add(my_pubkey, new UInt256(nonce));
                NonceHashes.Clear();
                NonceHashes.Add(my_pubkey, new UInt256(nonce.Sha256()));
                NoncePieces.Clear();
                TransactionHashes.Clear();
                TransactionHashes.UnionWith(Blockchain.Default.GetMemoryPool().Select(p => p.Hash));
                index_me = Array.IndexOf(Miners, my_pubkey);
                _request = null;
                if (action_request != null)
                    action_request.Reset();
            }
        }
    }
}
