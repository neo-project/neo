using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Ledger.MPT
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

        /// <summary>
        /// Get the index-th bit of a number.
        /// </summary>
        /// <param name="number">Number from where to get the bit.</param>
        /// <param name="index">Index of the bit.</param>
        /// <returns>The bit</returns>
        public static bool GetBit(this int number, int index) => (number >> index) % 2 == 1;

        /// <summary>
        /// Encodes nibbles into bytes.
        /// </summary>
        /// <param name="hexarray">Nibbles to be encoded.</param>
        /// <param name="isLeaf">Indicates if is a leaf or an extension node.</param>
        /// <returns>An array of bytes</returns>
        public static byte[] CompactEncode(this byte[] hexarray, bool isLeaf = false)
        {
            var first = (byte) ((isLeaf ? 2 : 0) + hexarray.Length % 2);
            var hexarrayList = new List<byte>(hexarray);
            if (first % 2 == 0)
            {
                hexarrayList.Insert(0, first);
                hexarrayList.Insert(1, 0);
            }
            else
            {
                hexarrayList.Insert(0, first);
            }

            var resp = new List<byte>();
            var hexarrayListCount = hexarrayList.Count;
            for (var i = 0; i < hexarrayListCount; i += 2)
            {
                resp.Add((byte) (16 * hexarrayList[i] + hexarrayList[i + 1]));
            }

            return resp.ToArray();
        }

        /// <summary>
        /// Decodes an array of bytes to a array of nibbles.
        /// </summary>
        /// <param name="hexarray">The array to be decoded.</param>
        /// <returns>An array of nibbles.</returns>
        public static byte[] CompactDecode(this byte[] hexarray)
        {
            var resp = new List<byte>(hexarray.Length * 2);
            if (hexarray[0] / 16 % 2 == 1)
            {
                resp.Add((byte) (hexarray[0] % 16));
            }

            for (var i = 1; i < hexarray.Length; i++)
            {
                resp.Add((byte) (hexarray[i] / 16));
                resp.Add((byte) (hexarray[i] % 16));
            }

            return resp.ToArray();
        }

        /// <summary>
        /// Converts byte array to StorageItem
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <returns>The generated StorageItem.</returns>
        public static StorageItem ToStorageItem(this byte[] data)
        {
            using (var memoryStream = new MemoryStream())
            using (var binWriter = new BinaryWriter(memoryStream))
            {
                binWriter.Write(data);
                using (var br = new BinaryReader(binWriter.BaseStream))
                {
                    br.BaseStream.Position = 0;
                    var item = new StorageItem();
                    item.Deserialize(br);
                    return item;
                }
            }
        }
    }
}