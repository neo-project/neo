using Neo.Cryptography;

namespace Neo.Oracle
{
    public abstract class OracleRequest
    {
        private UInt160 _hash;

        /// <summary>
        /// Type
        /// </summary>
        public abstract OracleRequestType Type { get; }

        /// <summary>
        /// Hash
        /// </summary>
        public UInt160 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt160(Crypto.Hash160(GetHashData()));
                }

                return _hash;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type</param>
        protected OracleRequest(OracleRequestType type) { Type = type; }

        /// <summary>
        /// This method serialize the parts of the class that should be taken into account for compute the Hash
        /// </summary>
        /// <returns>Serialized data</returns>
        protected abstract byte[] GetHashData();
    }
}
