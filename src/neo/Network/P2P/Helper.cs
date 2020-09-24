using Neo.Cryptography;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Network.P2P
{
    public static class Helper
    {
        public static byte[] GetHashData(this IWitnessed verifiable)
        {
            return Models.Extensions.GetHashData(verifiable, ProtocolSettings.Default.Magic);
        }
    }
}
