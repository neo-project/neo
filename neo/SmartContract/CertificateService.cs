using System;
using System.Text;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    /// <summary>
    /// X509Certificate API
    /// </summary>
    public class CertificateService
    {

        /// <summary>
        /// Get the raw data for the entire X.509 certificate as an array of bytes.
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate</remarks>
        /// <remarks>Evaluation stack output: Certificate.Tbs</remarks>
        public bool Certificate_GetRawTbsCertificate(ExecutionEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate x509)) return false;
            engine.CurrentContext.EvaluationStack.Push(x509.GetTbsCertificate());
            return true;
        }



        /// <summary>
        /// Get the signature algorithm of certificate in the current evaluation stack
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate</remarks>
        /// <remarks>Evaluation stack output: Certificate.SigAlgName</remarks>
        public bool Certificate_GetSignatureAlgorithm(ExecutionEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate x509)) return false;
            engine.CurrentContext.EvaluationStack.Push(x509.SigAlgName);
            return true;
        }


        /// <summary>
        /// Get the signature of certificate in the current evaluation stack
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate</remarks>
        /// <remarks>Evaluation stack output: Certificate.Signature</remarks>
        public bool Certificate_GetSignatureValue(ExecutionEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate x509)) return false;
            engine.CurrentContext.EvaluationStack.Push(x509.GetSignature());
            return true;
        }


        /// <summary>
        /// Get the version of certificate in the current evaluation stack
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate</remarks>
        /// <remarks>Evaluation stack output: Certificate.Version</remarks>
        public bool Certificate_GetVersion(ExecutionEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate x509)) return false;
            engine.CurrentContext.EvaluationStack.Push(x509.Version);
            return true;
        }


        /// <summary>
        /// Gets the serial number of a certificate as a hexadecimal string
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate</remarks>
        /// <remarks>Evaluation stack output: Certificate.SerialNumber</remarks>
        public bool Certificate_GetSerialNumber(ExecutionEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate x509)) return false;
            string serialNumber = x509.SerialNumber.ToByteArray().ToHexString();
            engine.CurrentContext.EvaluationStack.Push(serialNumber);
            return true;
        }


        /// <summary>
        /// Get the issuer of certificate in the current evaluation stack
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate</remarks>
        /// <remarks>Evaluation stack output: Certificate.Issuer</remarks>
        public bool Certificate_GetIssuer(ExecutionEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate x509)) return false;
            engine.CurrentContext.EvaluationStack.Push(x509.IssuerDN.ToString());
            return true;
        }


        /// <summary>
        /// Get the notBefore of certificate in the current evaluation stack
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate</remarks>
        /// <remarks>Evaluation stack output: Certificate.NotBefore</remarks>
        public bool Certificate_GetNotBefore(ExecutionEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate x509)) return false;
            long notBefore = new DateTimeOffset(x509.NotBefore).ToUnixTimeSeconds();
            engine.CurrentContext.EvaluationStack.Push(notBefore);
            return true;
        }


        /// <summary>
        /// Get the notAfter of certificate in the current evaluation stack
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate</remarks>
        /// <remarks>Evaluation stack output: Certificate.NotAfter</remarks>
        public bool Certificate_GetNotAfter(ExecutionEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate x509)) return false;
            long notAfter = new DateTimeOffset(x509.NotAfter).ToUnixTimeSeconds();
            engine.CurrentContext.EvaluationStack.Push(notAfter);
            return true;
        }


        /// <summary>
        /// Get the subject of certificate in the current evaluation stack
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate</remarks>
        /// <remarks>Evaluation stack output: Certificate.Subject</remarks>
        public bool Certificate_GetSubject(ExecutionEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate x509)) return false;
            engine.CurrentContext.EvaluationStack.Push(x509.SubjectDN.ToString());
            return true;
        }


        /// <summary>
        /// Get the notAfter of certificate in the current evaluation stack
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: encodedCertValue</remarks>
        /// <remarks>Evaluation stack output: Certificate</remarks>
        public bool Certificate_Decode(ExecutionEngine engine)
        {
            byte[] encodedCertValue = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            X509CertificateParser x509CertificateParser = new X509CertificateParser();
            X509Certificate x509 = x509CertificateParser.ReadCertificate(encodedCertValue);
            if (x509 == null) return false;

            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(x509));
            return true;
        }


        /// <summary>
        /// Verify the signature of certificate
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate, Algorihtm, SignatureValue, SignedData </remarks>
        /// <remarks>Evaluation stack output: true/false</remarks>
        public bool Certificate_CheckSignature(ExecutionEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate signerCertificate)) return false;

            string algorithm = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            byte[] signatureValue = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            byte[] signedData = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();

            if (signatureValue == null || signatureValue.Length == 0) return false;
            if (signedData == null || signedData.Length == 0) return false;

            try
            {
                bool valid = CheckSignature(signerCertificate, algorithm, signatureValue, signedData);
                engine.CurrentContext.EvaluationStack.Push(valid);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        /// Verify the signature with the certificate publicKey
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="signatureAlg"></param>
        /// <param name="signature"></param>
        /// <param name="signed"></param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.Exception"></exception>
        private bool CheckSignature(X509Certificate certificate, string signatureAlg, byte[] signature, byte[] signed)
        {
            ISigner signer = SignerUtilities.GetSigner(signatureAlg);
            AsymmetricKeyParameter publicKeyParameter = certificate.GetPublicKey();
            signer.Init(false, publicKeyParameter);
            signer.BlockUpdate(signed, 0, signed.Length);

            return signer.VerifySignature(signature);
        }


        private bool popX509Certificate(ExecutionEngine engine, out X509Certificate x509)
        {
            x509 = null;
            if (engine.CurrentContext.EvaluationStack.Count == 0) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)) return false;

            x509 = _interface.GetInterface<X509Certificate>();
            if (x509 == null) return false;

            return true;
        }

    }
}
