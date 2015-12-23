using System;
using System.Security.Cryptography.X509Certificates;

namespace AntShares.Cryptography.X509
{
    internal class CertificateQueryResult : IDisposable
    {
        public CertificateQueryResultType Type;
        public X509Certificate2 Certificate;

        public void Dispose()
        {
            if (Certificate != null)
            {
                Certificate.Dispose();
            }
        }
    }
}
