using Neo.VM;
using Neo.VM.Types;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Text;

namespace Neo.SmartContract
{
    static partial class InteropService
    {
        // Add  X509Certificate API
        #region certificate
        public static readonly uint Neo_Certificate_GetRawTbsCertificate = Register("Neo.Certificate.GetRawTbsCertificate", Certificate_GetRawTbsCertificate, 0_00000400);
        public static readonly uint Neo_Certificate_GetSignatureAlgorithm = Register("Neo.Certificate.GetSignatureAlgorithm", Certificate_GetSignatureAlgorithm, 0_00000400);
        public static readonly uint Neo_Certificate_GetSignatureValue = Register("Neo.Certificate.GetSignatureValue", Certificate_GetSignatureValue, 0_00000400);
        public static readonly uint Neo_Certificate_GetVersion = Register("Neo.Certificate.GetVersion", Certificate_GetVersion, 0_00000400);
        public static readonly uint Neo_Certificate_GetSerialNumber = Register("Neo.Certificate.GetSerialNumber", Certificate_GetSerialNumber, 0_00000400);
        public static readonly uint Neo_Certificate_GetIssuer = Register("Neo.Certificate.GetIssuer", Certificate_GetIssuer, 0_00000400);
        public static readonly uint Neo_Certificate_GetNotBefore = Register("Neo.Certificate.GetNotBefore", Certificate_GetNotBefore, 0_00000400);
        public static readonly uint Neo_Certificate_GetNotAfter = Register("Neo.Certificate.GetNotAfter", Certificate_GetNotAfter, 0_00000400);
        public static readonly uint Neo_Certificate_GetSubject = Register("Neo.Certificate.GetSubject", Certificate_GetSubject, 0_00000400);
        public static readonly uint Neo_Certificate_Decode = Register("Neo.Certificate.Decode", Certificate_Decode, 0_00500000);    //The fee equals to  System.Runtime.Deserialize
        public static readonly uint Neo_Certificate_GetBasicConstraints = Register("Neo.Certificate.GetBasicConstraints", Certificate_GetBasicConstraints, 0_00000400);
        public static readonly uint Neo_Certificate_GetKeyUsage = Register("Neo.Certificate.GetKeyUsage", Certificate_GetKeyUsage, 0_00000400);
        public static readonly uint Neo_Certificate_GetExtensionValue = Register("Neo.Certificate.GetExtensionValue", Certificate_GetExtensionValue, 0_00000400);
        public static readonly uint Neo_Certificate_CheckSignature = Register("Neo.Certificate.CheckSignature", Certificate_CheckSignature, 0_00030000); //The fee equals to System.Runtime.CheckWitness
        #endregion


        /// <summary>
        /// Get the raw data for the entire X.509 certificate as an array of bytes.
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate</remarks>
        /// <remarks>Evaluation stack output: Certificate.Tbs</remarks>
        private static bool Certificate_GetRawTbsCertificate(ApplicationEngine engine)
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
        private static bool Certificate_GetSignatureAlgorithm(ApplicationEngine engine)
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
        private static bool Certificate_GetSignatureValue(ApplicationEngine engine)
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
        private static bool Certificate_GetVersion(ApplicationEngine engine)
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
        private static bool Certificate_GetSerialNumber(ApplicationEngine engine)
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
        private static bool Certificate_GetIssuer(ApplicationEngine engine)
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
        private static bool Certificate_GetNotBefore(ApplicationEngine engine)
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
        private static bool Certificate_GetNotAfter(ApplicationEngine engine)
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
        private static bool Certificate_GetSubject(ApplicationEngine engine)
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
        private static bool Certificate_Decode(ApplicationEngine engine)
        {
            byte[] encodedCertValue = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            X509CertificateParser x509CertificateParser = new X509CertificateParser();
            X509Certificate x509 = x509CertificateParser.ReadCertificate(encodedCertValue);
            if (x509 == null) return false;

            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(x509));
            return true;
        }


        /// <summary>
        /// Get the basicConstraints of certificate in the current evaluation stack
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: X509Certificate</remarks>
        /// <remarks>Evaluation stack output: basicConstraints(int)</remarks>
        private static bool Certificate_GetBasicConstraints(ApplicationEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate certificate)) return false;

            int path = certificate.GetBasicConstraints();
            engine.CurrentContext.EvaluationStack.Push(path);
            return true;
        }


        /// <summary>
        /// Get the keyUsage of certificate in the current evaluation stack
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: X509Certificate</remarks>
        /// <remarks>Evaluation stack output: bool array</remarks>
        private static bool Certificate_GetKeyUsage(ApplicationEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate certificate)) return false;

            bool[] keyUsage = certificate.GetKeyUsage();
            VM.Types.Array array = new VM.Types.Array();
            for (int i = 0; keyUsage != null && i < keyUsage.Length; i++)
            {
                array.Add(keyUsage[i]);
            }
            engine.CurrentContext.EvaluationStack.Push(array);
            return true;
        }


        /// <summary>
        /// Get extension value by id
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate, extension id </remarks>
        /// <remarks>Evaluation stack output: a map contains `Id`, `Critical` and `Value` field</remarks>
        private static bool Certificate_GetExtensionValue(ApplicationEngine engine)
        {
            if (!popX509Certificate(engine, out X509Certificate certificate)) return false;

            string oid = Encoding.UTF8.GetString(engine.CurrentContext.EvaluationStack.Pop().GetByteArray());
            Asn1OctetString oidValue = certificate.GetExtensionValue(new DerObjectIdentifier(oid));
            if (oidValue == null)
            {
                return false;
            }
            byte[] value = oidValue.GetOctets();
            bool critical = certificate.GetCriticalExtensionOids().Contains(oid);

            Map map = new Map();
            map.Add("Id", oid);
            map.Add("Critical", critical);
            map.Add("Value", value);

            engine.CurrentContext.EvaluationStack.Push(map);
            return true;
        }




        /// <summary>
        /// Verify the signature of certificate
        /// </summary>
        /// <param name="engine"></param>
        /// <remarks>Evaluation stack input: Certificate, Algorihtm, SignatureValue, SignedData </remarks>
        /// <remarks>Evaluation stack output: true/false</remarks>
        private static bool Certificate_CheckSignature(ApplicationEngine engine)
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
        private static bool CheckSignature(X509Certificate certificate, string signatureAlg, byte[] signature, byte[] signed)
        {
            ISigner signer = SignerUtilities.GetSigner(signatureAlg);
            AsymmetricKeyParameter publicKeyParameter = certificate.GetPublicKey();
            signer.Init(false, publicKeyParameter);
            signer.BlockUpdate(signed, 0, signed.Length);

            return signer.VerifySignature(signature);
        }


        private static bool popX509Certificate(ApplicationEngine engine, out X509Certificate x509)
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
