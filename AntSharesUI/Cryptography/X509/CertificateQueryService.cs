using AntShares.Core;
using AntShares.Cryptography.ECC;
using AntShares.Properties;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AntShares.Cryptography.X509
{
    internal static class CertificateQueryService
    {
        public static CertificateQueryResult Query(ECPoint pubkey)
        {
            if (pubkey.Equals(Blockchain.AntShare.Issuer) || pubkey.Equals(Blockchain.AntCoin.Issuer))
                return new CertificateQueryResult { Type = CertificateQueryResultType.System };
            Directory.CreateDirectory(Settings.Default.CertCachePath);
            string path = Path.Combine(Settings.Default.CertCachePath, $"{pubkey}.cer");
            if (!File.Exists(path))
            {
                //TODO: 本地缓存中找不到证书的情况下，去公共服务器上查询
            }
            if (!File.Exists(path))
                return new CertificateQueryResult { Type = CertificateQueryResultType.Missing };
            X509Certificate2 cert;
            try
            {
                cert = new X509Certificate2(path);
            }
            catch (CryptographicException)
            {
                return new CertificateQueryResult { Type = CertificateQueryResultType.Missing };
            }
            //TODO: 取到证书后，验证证书是否合法等
            return new CertificateQueryResult
            {
                Certificate = cert,
                Type = CertificateQueryResultType.Good
            };
        }
    }
}
