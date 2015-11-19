using AntShares.Algebra;
using AntShares.Cryptography.ECC;
using System.Collections.Generic;

namespace AntShares.Miner
{
    internal class BlockConsensusContext
    {
        public UInt256 PrevHash;
        public ECPoint[] Miners;
        public readonly Dictionary<ECPoint, UInt256> Nonces = new Dictionary<ECPoint, UInt256>();
        public readonly Dictionary<ECPoint, UInt256> NonceHashes = new Dictionary<ECPoint, UInt256>();
        public readonly Dictionary<ECPoint, List<FiniteFieldPoint>> NoncePieces = new Dictionary<ECPoint, List<FiniteFieldPoint>>();
        public readonly HashSet<UInt256> TransactionHashes = new HashSet<UInt256>();
    }
}
