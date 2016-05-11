using AntShares.Cryptography;

namespace AntShares.Core.Scripts
{
    public static class Helper
    {
        /// <summary>
        /// 计算脚本的散列值，先使用sha256，然后再计算一次ripemd160
        /// </summary>
        /// <param name="script">要计算散列值的脚本</param>
        /// <returns>返回脚本的散列值</returns>
        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(script.Sha256().RIPEMD160());
        }
    }
}
