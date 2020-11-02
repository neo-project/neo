using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using Neo.SmartContract.Native.Designate;
using Neo.VM;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class OracleResponse : TransactionAttribute
    {
        private const int MaxResultSize = 1024;

        public static readonly byte[] FixedScript;

        public ulong Id;
        public OracleResponseCode Code;
        public byte[] Result;

        public override TransactionAttributeType Type => TransactionAttributeType.OracleResponse;
        public override bool AllowMultiple => false;

        public override int Size => base.Size +
            sizeof(ulong) +                 //Id
            sizeof(OracleResponseCode) +    //ResponseCode
            Result.GetVarSize();            //Result

        static OracleResponse()
        {
            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitAppCall(NativeContract.Oracle.Hash, "finish");
            FixedScript = sb.ToArray();
        }

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Code = (OracleResponseCode)reader.ReadByte();
            if (!Enum.IsDefined(typeof(OracleResponseCode), Code))
                throw new FormatException();
            Result = reader.ReadVarBytes(MaxResultSize);
            if (Code != OracleResponseCode.Success && Result.Length > 0)
                throw new FormatException();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write((byte)Code);
            writer.WriteVarBytes(Result);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["id"] = Id;
            json["code"] = Code;
            json["result"] = Convert.ToBase64String(Result);
            return json;
        }

        public override bool Verify(StoreView snapshot, Transaction tx)
        {
            if (tx.Signers.Any(p => p.Scopes != WitnessScope.None)) return false;
            if (!tx.Script.AsSpan().SequenceEqual(FixedScript)) return false;
            OracleRequest request = NativeContract.Oracle.GetRequest(snapshot, Id);
            if (request is null) return false;
            if (tx.NetworkFee + tx.SystemFee != request.GasForResponse) return false;
            UInt160 oracleAccount = Blockchain.GetConsensusAddress(NativeContract.Designate.GetDesignatedByRole(snapshot, Role.Oracle, snapshot.Height));
            return tx.Signers.Any(p => p.Account.Equals(oracleAccount));
        }
    }
}
