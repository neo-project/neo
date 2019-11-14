using Neo.Cryptography;

namespace Neo.Oracle
{
    public abstract class OracleRequest
    {
        private UInt160 _hash;

        /// <summary>
        /// Hash
        /// </summary>
        public UInt160 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt160(Crypto.Default.Hash160(GetHashData()));
                }

                return _hash;
            }
        }


        protected abstract byte[] GetHashData();
    }
}
