using AntShares.Core;
using AntShares.Cryptography.ECC;
using AntShares.Properties;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AntShares.Cryptography
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
            if (cert.PublicKey.Oid.Value != "1.2.840.10045.2.1")
                return new CertificateQueryResult { Type = CertificateQueryResultType.Missing };
            if (!pubkey.Equals(ECPoint.DecodePoint(cert.PublicKey.EncodedKeyValue.RawData, ECCurve.Secp256r1)))
                return new CertificateQueryResult { Type = CertificateQueryResultType.Missing };
            using (X509Chain chain = new X509Chain())
            {
                CertificateQueryResult result = new CertificateQueryResult { Certificate = cert };
                if (chain.Build(cert))
                {
                    result.Type = CertificateQueryResultType.Good;
                }
                else if (chain.ChainStatus.Length == 1 && chain.ChainStatus[0].Status == X509ChainStatusFlags.NotTimeValid)
                {
                    result.Type = CertificateQueryResultType.Expired;
                }
                else
                {
                    result.Type = CertificateQueryResultType.Invalid;
                }
                return result;
            }
        }
    }
}
