using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;
using System.Text;

namespace Neo.Oracle
{
    public class OracleResponse : ISerializable
    {
        private UInt160 _hash;
        private bool _alreadyPayed;

        /// <summary>
        /// Request hash
        /// </summary>
        public UInt160 RequestHash { get; set; }

        /// <summary>
        /// Result
        /// </summary>
        public byte[] Result { get; set; }

        /// <summary>
        /// Error
        /// </summary>
        public bool Error => Result == null;

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
                    _hash = new UInt160(Crypto.Hash160(this.ToArray()));
                }

                return _hash;
            }
        }

        public int Size => UInt160.Length + sizeof(byte) + Result.GetVarSize() + sizeof(long);

        /// <summary>
        /// Filter cost that it could be consumed only once
        /// </summary>
        public long FilterCostOnce()
        {
            if (_alreadyPayed) return 0;

            _alreadyPayed = true;
            return FilterCost;
        }

        /// <summary>
        /// Reset the _alreadyPayed flag
        /// </summary>
        public void ResetFilterCostOnce()
        {
            _alreadyPayed = false;
        }

        /// <summary>
        /// Create error result
        /// </summary>
        /// <param name="requestHash">Request Id</param>
        /// <param name="filterCost">Gas cost</param>
        /// <returns>OracleResult</returns>
        public static OracleResponse CreateError(UInt160 requestHash, long filterCost = 0)
        {
            return CreateResult(requestHash, (byte[])null, filterCost);
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
                Result = result,
                FilterCost = filterCost
            };
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(RequestHash);
            writer.Write(FilterCost);

            if (Result != null)
            {
                writer.Write((byte)0x01);
                writer.WriteVarBytes(Result);
            }
            else
            {
                // Error result

                writer.Write((byte)0x00);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            RequestHash = reader.ReadSerializable<UInt160>();
            FilterCost = reader.ReadInt64();
            if (FilterCost < 0) throw new FormatException(nameof(FilterCost));

            if (reader.ReadByte() == 0x01)
            {
                Result = reader.ReadVarBytes(ushort.MaxValue);
            }
            else
            {
                // Error result

                Result = null;
            }
        }
    }
}
