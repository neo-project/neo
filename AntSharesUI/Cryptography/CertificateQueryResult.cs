using System;
using System.Security.Cryptography.X509Certificates;

namespace AntShares.Cryptography
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
