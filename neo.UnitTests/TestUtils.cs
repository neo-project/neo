using Neo.Core;
using Neo.Cryptography.ECC;

namespace Neo.UnitTests
{
    public static class TestUtils
    {
        public static byte[] GetByteArray(int length, byte firstByte)
        {            
            byte[] array = new byte[length];
            array[0] = firstByte;
            for (int i = 1; i < length; i++)
            {
                array[i] = 0x20;
            }
            return array;
        }

        public static readonly ECPoint[] StandbyValidators = new ECPoint[] { ECPoint.DecodePoint("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c".HexToBytes(), ECCurve.Secp256r1) };

        public static ClaimTransaction GetClaimTransaction()
        {
            return new ClaimTransaction
            {
                Claims = new CoinReference[0],
                Attributes = new TransactionAttribute[0],
                Inputs = new CoinReference[0],
                Outputs = new TransactionOutput[0],
                Scripts = new Witness[0]
            };
        }

        public static MinerTransaction GetMinerTransaction()
        {
            return new MinerTransaction
            {
                Nonce = 2083236893,
                Attributes = new TransactionAttribute[0],
                Inputs = new CoinReference[0],
                Outputs = new TransactionOutput[0],
                Scripts = new Witness[0]
            };
        }

        public static CoinReference GetCoinReference(UInt256 prevHash)
        {
            if (prevHash == null) prevHash = UInt256.Zero;
            return new CoinReference
            {
                PrevHash = prevHash,
                PrevIndex = 0
            };
        }

        public static void SetupTestBlockchain(UInt256 assetId)
        {
            Blockchain testBlockchain = new TestBlockchain(assetId);
            Blockchain.RegisterBlockchain(testBlockchain);
        }
    }    
}
