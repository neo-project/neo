using Neo.Cryptography;

namespace Neo.SmartContract.Native
{
    public sealed class NameService : NonfungibleToken<NameService.NameState>
    {
        public override int Id => -6;
        public override string Symbol => "NNS";

        internal NameService()
        {
        }

        protected override byte[] GetKey(byte[] tokenId)
        {
            return Crypto.Hash160(tokenId);
        }

        public class NameState : NFTState
        {
            public override byte[] Id => Utility.StrictUTF8.GetBytes(Name);
        }
    }
}
