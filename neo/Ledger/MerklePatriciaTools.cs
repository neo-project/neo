using System.Linq;

namespace Neo.Ledger
{
    public static class MerklePatriciaTools
    {
        /// <summary>
        /// Converts a byte array to an hexadecimal string.
        /// </summary>
        /// <param name="hexchar">Byte array to be converted.</param>
        /// <returns>The converted string.</returns>
        public static string ByteToHexString(this byte[] hexchar, bool useSpace = true, bool forceTwoChars = true) =>
            string.Join(useSpace ? " " : "",
                hexchar.Select(x => x.ToString("x" + (forceTwoChars ? "2" : ""))).ToList());
    }
}