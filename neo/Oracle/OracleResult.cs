using Neo.Cryptography;
using Neo.SmartContract;
using Neo.IO;
using Neo.VM;
using System.IO;
using System.Text;

namespace Neo.Oracle
{
    public class OracleResult : IInteroperable
    {
        private UInt160 _hash;

        /// <summary>
        /// Transaction Hash
        /// </summary>
        public UInt256 TransactionHash { get; }

        /// <summary>
        /// Request hash
        /// </summary>
        public UInt160 RequestHash { get; }

        /// <summary>
        /// Error
        /// </summary>
        public OracleResultError Error { get; }

        /// <summary>
        /// Result
        /// </summary>
        public byte[] Result { get; }

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

        /// <summary>
        /// Error constructor
        /// </summary>
        /// <param name="txHash">Tx Hash</param>
        /// <param name="requestHash">Request Id</param>
        /// <param name="error">Error</param>
        internal OracleResult(UInt256 txHash, UInt160 requestHash, OracleResultError error)
        {
            TransactionHash = txHash;
            RequestHash = requestHash;
            Error = error;
            Result = null;
        }

        /// <summary>
        /// Good result constructor
        /// </summary>
        /// <param name="txHash">Tx Hash</param>
        /// <param name="requestHash">Request Id</param>
        /// <param name="result">Result</param>
        internal OracleResult(UInt256 txHash, UInt160 requestHash, byte[] result)
        {
            Error = OracleResultError.None;
            TransactionHash = txHash;
            RequestHash = requestHash;
            Result = result;
        }

        /// <summary>
        /// Create error result
        /// </summary>
        /// <param name="txHash">Tx Hash</param>
        /// <param name="requestHash">Request Id</param>
        /// <returns>OracleResult</returns>
        public static OracleResult CreateError(UInt256 txHash, UInt160 requestHash, OracleResultError error)
        {
            return new OracleResult(txHash, requestHash, error);
        }

        /// <summary>
        /// Create result
        /// </summary>
        /// <param name="txHash">Tx Hash</param>
        /// <param name="requestHash">Request Hash</param>
        /// <param name="result">Result</param>
        /// <returns>OracleResult</returns>
        public static OracleResult CreateResult(UInt256 txHash, UInt160 requestHash, string result)
        {
            return new OracleResult(txHash, requestHash, Encoding.UTF8.GetBytes(result));
        }

        /// <summary>
        /// Create result
        /// </summary>
        /// <param name="txHash">Tx Hash</param>
        /// <param name="requestHash">Request Id</param>
        /// <param name="result">Result</param>
        /// <returns>OracleResult</returns>
        public static OracleResult CreateResult(UInt256 txHash, UInt160 requestHash, byte[] result)
        {
            return new OracleResult(txHash, requestHash, result);
        }

        /// <summary>
        /// Get hash data
        /// </summary>
        /// <returns>Hash data</returns>
        private byte[] GetHashData()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(TransactionHash);
                writer.Write(RequestHash);
                writer.Write((byte)Error);
                if (Result != null) writer.WriteVarBytes(Result);
                writer.Flush();

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Get Stack item for IInteroperable
        /// </summary>
        /// <returns>StackItem</returns>
        public StackItem ToStackItem()
        {
            return new VM.Types.Array(new StackItem[]
            {
                new VM.Types.Integer((byte)Error),
                Result == null ? StackItem.Null : new VM.Types.ByteArray(Result)
            });
        }
    }
}
