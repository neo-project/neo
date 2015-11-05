using AntShares.Cryptography;

namespace AntShares.Core.Scripts
{
    public static class Helper
    {
        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(script.Sha256().RIPEMD160());
        }
    }
}
