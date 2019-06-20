using System;
using System.Collections;

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.X509;

using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Operators;

using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_CertificateService
    {
        CertificateService certificateService;

        // rootCA(selfCA) ----> subCA
        X509Certificate rootCA;
        X509Certificate subCA;

        [TestInitialize]
        public void TestSetup()
        {
            certificateService = new CertificateService();
            buildCertifacteChain();
        }


        private void buildCertifacteChain()
        {
            var rootKey = genKey();
            rootCA = buildsCertificate("Subject001", rootKey.Public, rootKey.Private);

            var subKey = genKey();
            subCA = buildsCertificate("Subject002", subKey.Public, rootKey.Private);
        }

        public AsymmetricCipherKeyPair genKey()
        {
            var kpgen = new RsaKeyPairGenerator();
            kpgen.Init(new KeyGenerationParameters(new SecureRandom(), 1024));
            var kp = kpgen.GenerateKeyPair();
            return kp;
        }


        private X509Certificate buildsCertificate(string subjectName,AsymmetricKeyParameter publicKey, AsymmetricKeyParameter signPrivateKey)
        {
            var gen = new X509V3CertificateGenerator();
            var CN = new X509Name("CN=" + subjectName);
            var SN = BigInteger.ProbablePrime(120, new Random());

            gen.SetSerialNumber(SN);
            gen.SetSubjectDN(CN);
            gen.SetIssuerDN(CN);
            gen.SetNotAfter(DateTime.Now.AddYears(1));
            gen.SetNotBefore(DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)));
            gen.SetPublicKey(publicKey);
            gen.AddExtension(
                X509Extensions.AuthorityKeyIdentifier.Id,
                false,
                new AuthorityKeyIdentifier(
                    SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey),
                    new GeneralNames(new GeneralName(CN)),
                    SN
                ));
            gen.AddExtension(
                X509Extensions.ExtendedKeyUsage.Id,
                false,
                new ExtendedKeyUsage(new ArrayList()
                {
                new DerObjectIdentifier("1.3.6.1.5.5.7.3.1")
                }));

            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA256WITHRSA", signPrivateKey);
            X509Certificate cert = gen.Generate(signatureFactory);
            return cert;
        }


        [TestMethod]
        public void Certificate_GetRawTbsCertificate()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_GetRawTbsCertificate(executionEngine);
            byte[] rawTbs = executionEngine.CurrentContext.EvaluationStack.Pop().GetByteArray();

            optSuccess.Should().BeTrue();
            rawTbs.Should().Equal(rootCA.GetTbsCertificate());
        }

        [TestMethod]
        public void Certificate_GetSignatureAlgorithm()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_GetSignatureAlgorithm(executionEngine);
            string signatureAlg = executionEngine.CurrentContext.EvaluationStack.Pop().GetString();

            optSuccess.Should().BeTrue();
            signatureAlg.Should().Equals(rootCA.SigAlgName);
            signatureAlg.Should().Equals("SHA256WITHRSA");
        }

        [TestMethod]
        public void Certificate_GetSignatureValue()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_GetSignatureValue(executionEngine);
            byte[] signatureValue = executionEngine.CurrentContext.EvaluationStack.Pop().GetByteArray();

            optSuccess.Should().BeTrue();
            signatureValue.Should().Equals(rootCA.GetSignature());
        }

        [TestMethod]
        public void Certificate_GetVersion()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_GetVersion(executionEngine);
            int version = (int) executionEngine.CurrentContext.EvaluationStack.Pop().GetBigInteger();

            optSuccess.Should().BeTrue();
            version.Should().Equals(rootCA.Version);
        }

        [TestMethod]
        public void Certificate_GetSerialNumber()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_GetSerialNumber(executionEngine);
            string serialNumber = executionEngine.CurrentContext.EvaluationStack.Pop().GetString();
            string serialNumberStr = rootCA.SerialNumber.ToByteArray().ToHexString();

            optSuccess.Should().BeTrue();
            serialNumber.Should().Equals(serialNumberStr);
        }

        [TestMethod]
        public void Certificate_GetIssuer()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_GetIssuer(executionEngine);
            string issuer = executionEngine.CurrentContext.EvaluationStack.Pop().GetString();

            optSuccess.Should().BeTrue();
            issuer.Should().Equals(rootCA.IssuerDN.ToString());
        }

        [TestMethod]
        public void Certificate_GetNotBefore()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_GetNotBefore(executionEngine);
            long notBefore = (long) executionEngine.CurrentContext.EvaluationStack.Pop().GetBigInteger();

            optSuccess.Should().BeTrue();
            notBefore.Should().Equals(new DateTimeOffset(rootCA.NotBefore).ToUnixTimeSeconds());
        }


        [TestMethod]
        public void Certificate_GetNotAfter()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_GetNotAfter(executionEngine);
            long notBefore = (long)executionEngine.CurrentContext.EvaluationStack.Pop().GetBigInteger();

            optSuccess.Should().BeTrue();
            notBefore.Should().Equals(new DateTimeOffset(rootCA.NotAfter).ToUnixTimeSeconds());
        }


        [TestMethod]
        public void Certificate_GetSubject()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_GetSubject(executionEngine);
            string subject = executionEngine.CurrentContext.EvaluationStack.Pop().GetString();

            optSuccess.Should().BeTrue();
            subject.Should().Equals(rootCA.SubjectDN.ToString());
        }


        [TestMethod]
        public void Certificate_Decode()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(rootCA.GetEncoded());

            bool optSuccess = certificateService.Certificate_Decode(executionEngine);
            InteropInterface _interface =  (InteropInterface) executionEngine.CurrentContext.EvaluationStack.Pop();
            X509Certificate cerficate  = _interface.GetInterface<X509Certificate>();

            optSuccess.Should().BeTrue();
            cerficate.GetTbsCertificate().Should().Equals(rootCA.GetTbsCertificate());
        }


        [TestMethod]
        public void CheckSignature_RootCA()
        {
            string sigAlgName =  rootCA.SigAlgName;
            byte[] signature = rootCA.GetSignature();
            byte[] signedData = rootCA.GetTbsCertificate();

            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(signedData);
            executionEngine.CurrentContext.EvaluationStack.Push(signature);
            executionEngine.CurrentContext.EvaluationStack.Push(sigAlgName);
            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_CheckSignature(executionEngine);
            bool checkSignature = executionEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

            optSuccess.Should().BeTrue();
            checkSignature.Should().BeTrue();
        }

        [TestMethod]
        public void CheckSignature_SubCA()
        {
            string sigAlgName = subCA.SigAlgName;
            byte[] signature = subCA.GetSignature();
            byte[] signedData = subCA.GetTbsCertificate();

            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(signedData);
            executionEngine.CurrentContext.EvaluationStack.Push(signature);
            executionEngine.CurrentContext.EvaluationStack.Push(sigAlgName);
            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_CheckSignature(executionEngine);
            bool checkSignature = executionEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

            optSuccess.Should().BeTrue();
            checkSignature.Should().BeTrue();
        }


        [TestMethod]
        public void CheckSignature_Failure()
        {
            string sigAlgName = rootCA.SigAlgName;
            byte[] signature = rootCA.GetSignature();
            byte[] signedData = rootCA.GetTbsCertificate();

            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            signedData[0] = 0x00;
            signedData[1] = 0x00;
            signedData[2] = 0x00;
            signedData[3] = 0x00;
            executionEngine.CurrentContext.EvaluationStack.Push(signedData);
            executionEngine.CurrentContext.EvaluationStack.Push(signature);
            executionEngine.CurrentContext.EvaluationStack.Push(sigAlgName);
            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

            bool optSuccess = certificateService.Certificate_CheckSignature(executionEngine);
            bool checkSignature = executionEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

            optSuccess.Should().BeTrue();
            checkSignature.Should().BeFalse();
        }
       
    }
}
