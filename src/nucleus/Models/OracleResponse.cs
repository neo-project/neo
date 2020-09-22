namespace Neo.Models
{
    public class OracleResponse : TransactionAttribute
    {
        public ulong Id;
        public OracleResponseCode Code;
        public byte[] Result;

        public override bool AllowMultiple => false;
    }
}
