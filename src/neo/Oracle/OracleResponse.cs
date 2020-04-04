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
    public class OracleResponse : IInteroperable, IVerifiable
    {
        private UInt160 _hash;

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
        /// Filter cost paid by Oracle and must be claimed to the user
        /// </summary>
        public long FilterCost { get; set; }

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

        public int Size => UInt160.Length + sizeof(byte) + Result.GetVarSize() + sizeof(long);

        public Witness[] Witnesses
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Create error result
        /// </summary>
        /// <param name="requestHash">Request Id</param>
        /// <param name="error">Error</param>
        /// <param name="filterCost">Gas cost</param>
        /// <returns>OracleResult</returns>
        public static OracleResponse CreateError(UInt160 requestHash, OracleResultError error, long filterCost = 0)
        {
            return new OracleResponse()
            {
                RequestHash = requestHash,
                Error = error,
                Result = new byte[0],
                FilterCost = filterCost
            };
        }

        /// <summary>
        /// Create result
        /// </summary>
        /// <param name="requestHash">Request Hash</param>
        /// <param name="result">Result</param>
        /// <param name="filterCost">Gas cost</param>
        /// <returns>OracleResult</returns>
        public static OracleResponse CreateResult(UInt160 requestHash, string result, long filterCost)
        {
            return CreateResult(requestHash, Encoding.UTF8.GetBytes(result), filterCost);
        }

        /// <summary>
        /// Create result
        /// </summary>
        /// <param name="requestHash">Request Id</param>
        /// <param name="result">Result</param>
        /// <param name="filterCost">Gas cost</param>
        /// <returns>OracleResult</returns>
        public static OracleResponse CreateResult(UInt160 requestHash, byte[] result, long filterCost)
        {
            return new OracleResponse()
            {
                RequestHash = requestHash,
                Error = OracleResultError.None,
                Result = result,
                FilterCost = filterCost
            };
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(RequestHash);
            writer.Write((byte)Error);
            if (Error == OracleResultError.None)
                writer.WriteVarBytes(Result);
            writer.Write(FilterCost);
        }

        public void Serialize(BinaryWriter writer)
        {
            SerializeUnsigned(writer);
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            RequestHash = reader.ReadSerializable<UInt160>();
            Error = (OracleResultError)reader.ReadByte();
            Result = Error == OracleResultError.None ? reader.ReadVarBytes(ushort.MaxValue) : new byte[0];
            FilterCost = reader.ReadInt64();
            if (FilterCost < 0) throw new FormatException(nameof(FilterCost));
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
                new ByteString(RequestHash.ToArray()),
                new Integer((byte)Error),
                new ByteString(Result)
            });
        }
    }
}
