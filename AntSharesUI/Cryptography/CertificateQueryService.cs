using AntShares.Cryptography.ECC;
using AntShares.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AntShares.Cryptography
{
    internal static class CertificateQueryService
    {
        private static WebClient web = new WebClient();
        private static Dictionary<ECPoint, CertificateQueryResult> results = new Dictionary<ECPoint, CertificateQueryResult>();

        static CertificateQueryService()
        {
            Directory.CreateDirectory(Settings.Default.CertCachePath);
            web.DownloadFileCompleted += Web_DownloadFileCompleted;
        }

        private static void Web_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            ECPoint pubkey = (ECPoint)e.UserState;
            lock (results)
            {
                if (e.Cancelled || e.Error != null)
                    results[pubkey].Type = CertificateQueryResultType.Missing;
                else
                    UpdateResultFromFile(pubkey);
            }
        }

        public static CertificateQueryResult Query(ECPoint pubkey, string url)
        {
            lock (results)
            {
                if (results.ContainsKey(pubkey)) return results[pubkey];
                results[pubkey] = new CertificateQueryResult { Type = CertificateQueryResultType.Querying };
            }
            string path = Path.Combine(Settings.Default.CertCachePath, $"{pubkey}.cer");
            if (File.Exists(path))
            {
                lock (results)
                {
                    UpdateResultFromFile(pubkey);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(url))
                    url = $"http://cert.onchain.com/antshares/{pubkey}.cer";
                web.DownloadFileAsync(new Uri(url), path, pubkey);
            }
            return results[pubkey];

            //if (!File.Exists(path))
            //    return new CertificateQueryResult { Type = CertificateQueryResultType.Missing };
        }

        private static void UpdateResultFromFile(ECPoint pubkey)
        {
            X509Certificate2 cert;
            try
            {
                cert = new X509Certificate2(Path.Combine(Settings.Default.CertCachePath, $"{pubkey}.cer"));
            }
            catch (CryptographicException)
            {
                results[pubkey].Type = CertificateQueryResultType.Missing;
                return;
            }
            if (cert.PublicKey.Oid.Value != "1.2.840.10045.2.1")
            {
                results[pubkey].Type = CertificateQueryResultType.Missing;
                return;
            }
            if (!pubkey.Equals(ECPoint.DecodePoint(cert.PublicKey.EncodedKeyValue.RawData, ECCurve.Secp256r1)))
            {
                results[pubkey].Type = CertificateQueryResultType.Missing;
                return;
            }
            using (X509Chain chain = new X509Chain())
            {
                results[pubkey].Certificate = cert;
                if (chain.Build(cert))
                {
                    results[pubkey].Type = CertificateQueryResultType.Good;
                }
                else if (chain.ChainStatus.Length == 1 && chain.ChainStatus[0].Status == X509ChainStatusFlags.NotTimeValid)
                {
                    results[pubkey].Type = CertificateQueryResultType.Expired;
                }
                else
                {
                    results[pubkey].Type = CertificateQueryResultType.Invalid;
                }
            }
        }
    }
}
