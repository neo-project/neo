using Neo.Cryptography;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using System.Text;

namespace Neo.Oracle
{
    public class OracleResult : IInteroperable, IVerifiable
    {
        private UInt160 _hash;

        /// <summary>
        /// Transaction Hash
        /// </summary>
        public UInt256 TransactionHash { get; set; }

        /// <summary>
        /// Request hash
        /// </summary>
        public UInt160 RequestHash { get; set; }

        /// <summary>
        /// Error
        /// </summary>
        public OracleResultError Error { get; set; }

        /// <summary>
        /// Result
        /// </summary>
        public byte[] Result { get; set; }

        /// <summary>
        /// Hash
        /// </summary>
        public UInt160 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt160(Crypto.Hash160(this.GetHashData()));
                }

                return _hash;
            }
        }

        public int Size => UInt256.Length + UInt160.Length + sizeof(byte) + Result.GetVarSize();

        public Witness[] Witnesses
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Create error result
        /// </summary>
        /// <param name="txHash">Tx Hash</param>
        /// <param name="requestHash">Request Id</param>
        /// <returns>OracleResult</returns>
        public static OracleResult CreateError(UInt256 txHash, UInt160 requestHash, OracleResultError error)
        {
            return new OracleResult()
            {
                TransactionHash = txHash,
                RequestHash = requestHash,
                Error = error,
                Result = new byte[0],
            };
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
            return CreateResult(txHash, requestHash, Encoding.UTF8.GetBytes(result));
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
            return new OracleResult()
            {
                TransactionHash = txHash,
                RequestHash = requestHash,
                Error = OracleResultError.None,
                Result = result,
            };
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(TransactionHash);
            writer.Write(RequestHash);
            writer.Write((byte)Error);
            if (Error != OracleResultError.None)
                writer.WriteVarBytes(Result);
        }

        public void Serialize(BinaryWriter writer)
        {
            SerializeUnsigned(writer);
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            TransactionHash = reader.ReadSerializable<UInt256>();
            RequestHash = reader.ReadSerializable<UInt160>();
            Error = (OracleResultError)reader.ReadByte();
            Result = Error != OracleResultError.None ? reader.ReadVarBytes(ushort.MaxValue) : new byte[0];
        }

        public void Deserialize(BinaryReader reader)
        {
            DeserializeUnsigned(reader);
        }

        public UInt160[] GetScriptHashesForVerifying(StoreView snapshot)
        {
            return new UInt160[] { new UInt160(Crypto.Hash160(this.GetHashData())) };
        }

        /// <summary>
        /// Get Stack item for IInteroperable
        /// </summary>
        /// <returns>StackItem</returns>
        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new VM.Types.Array(referenceCounter, new StackItem[]
            {
                new Integer((byte)Error),
                new ByteArray(Result)
            });
        }
    }
}
