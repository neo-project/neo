using System;
using System.IO;

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.X509;

using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_CertificateService
    {
        CertificateService certificateService;

        // rootCA ----> level2CA ---> level3CA --> level4CA
        X509Certificate rootCA;
        X509Certificate level2CA;
        X509Certificate level3CA;
        X509Certificate level4CA;
        X509Certificate selfSignCA;


        [TestInitialize]
        public void TestSetup()
        {
            certificateService = new CertificateService();

            rootCA = LoadX509("./cert/root_ca.crt");
            level2CA = LoadX509("./cert/level2_ca.crt");
            level3CA = LoadX509("./cert/level3_ca.crt");
            level4CA = LoadX509("./cert/level4_ca.crt");
            selfSignCA = rootCA;  
        }



        [TestMethod]
        public void Certificate_GetRawTbsCertificate()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_GetRawTbsCertificate(executionEngine);
            byte[] rawTbs = executionEngine.CurrentContext.EvaluationStack.Pop().GetByteArray();

            optSuccess.Should().BeTrue();
            rawTbs.Should().Equal(level2CA.GetTbsCertificate());
        }

        [TestMethod]
        public void Certificate_GetSignatureAlgorithm()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_GetSignatureAlgorithm(executionEngine);
            string signatureAlg = executionEngine.CurrentContext.EvaluationStack.Pop().GetString();

            optSuccess.Should().BeTrue();
            signatureAlg.Should().Equals(level2CA.SigAlgName);
        }

        [TestMethod]
        public void Certificate_GetSignatureValue()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_GetSignatureValue(executionEngine);
            byte[] signatureValue = executionEngine.CurrentContext.EvaluationStack.Pop().GetByteArray();

            optSuccess.Should().BeTrue();
            signatureValue.Should().Equals(level2CA.GetSignature());
        }

        [TestMethod]
        public void Certificate_GetVersion()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_GetVersion(executionEngine);
            int version = (int) executionEngine.CurrentContext.EvaluationStack.Pop().GetBigInteger();

            optSuccess.Should().BeTrue();
            version.Should().Equals(level2CA.Version);
        }

        [TestMethod]
        public void Certificate_GetSerialNumber()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_GetSerialNumber(executionEngine);
            string serialNumber = executionEngine.CurrentContext.EvaluationStack.Pop().GetString();
            string serialNumberStr = level2CA.SerialNumber.ToByteArray().ToHexString();

            optSuccess.Should().BeTrue();
            serialNumber.Should().Equals(serialNumberStr);
        }

        [TestMethod]
        public void Certificate_GetIssuer()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_GetIssuer(executionEngine);
            string issuer = executionEngine.CurrentContext.EvaluationStack.Pop().GetString();

            optSuccess.Should().BeTrue();
            issuer.Should().Equals(level2CA.IssuerDN.ToString());
        }

        [TestMethod]
        public void Certificate_GetNotBefore()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_GetNotBefore(executionEngine);
            long notBefore = (long) executionEngine.CurrentContext.EvaluationStack.Pop().GetBigInteger();

            optSuccess.Should().BeTrue();
            notBefore.Should().Equals(new DateTimeOffset(level2CA.NotBefore).ToUnixTimeSeconds());
        }


        [TestMethod]
        public void Certificate_GetNotAfter()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_GetNotAfter(executionEngine);
            long notBefore = (long)executionEngine.CurrentContext.EvaluationStack.Pop().GetBigInteger();

            optSuccess.Should().BeTrue();
            notBefore.Should().Equals(new DateTimeOffset(level2CA.NotAfter).ToUnixTimeSeconds());
        }


        [TestMethod]
        public void Certificate_GetSubject()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_GetSubject(executionEngine);
            string subject = executionEngine.CurrentContext.EvaluationStack.Pop().GetString();

            optSuccess.Should().BeTrue();
            subject.Should().Equals(level2CA.SubjectDN.ToString());
        }


        [TestMethod]
        public void Certificate_Decode()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(level2CA.GetEncoded());

            bool optSuccess = certificateService.Certificate_Decode(executionEngine);
            InteropInterface _interface =  (InteropInterface) executionEngine.CurrentContext.EvaluationStack.Pop();
            X509Certificate cerficate  = _interface.GetInterface<X509Certificate>();

            optSuccess.Should().BeTrue();
            cerficate.GetTbsCertificate().Should().Equals(level2CA.GetTbsCertificate());
        }


        [TestMethod]
        public void CheckSignature()
        {
            string sigAlgName =  level3CA.SigAlgName;
            byte[] signature = level3CA.GetSignature();
            byte[] signedData = level3CA.GetTbsCertificate();

            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(signedData);
            executionEngine.CurrentContext.EvaluationStack.Push(signature);
            executionEngine.CurrentContext.EvaluationStack.Push(sigAlgName);
            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_CheckSignature(executionEngine);
            bool checkSignature = executionEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

            optSuccess.Should().BeTrue();
            checkSignature.Should().BeTrue();
        } 

        [TestMethod]
        public void CheckSignature_Failure()
        {
            string sigAlgName = level3CA.SigAlgName;
            byte[] signature = level3CA.GetSignature();
            byte[] signedData = level3CA.GetTbsCertificate();

            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            signedData[0] = 0x00;
            signedData[1] = 0x00;
            signedData[2] = 0x00;
            signedData[3] = 0x00;
            executionEngine.CurrentContext.EvaluationStack.Push(signedData);
            executionEngine.CurrentContext.EvaluationStack.Push(signature);
            executionEngine.CurrentContext.EvaluationStack.Push(sigAlgName);
            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_CheckSignature(executionEngine);
            bool checkSignature = executionEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

            optSuccess.Should().BeTrue();
            checkSignature.Should().BeFalse();
        }

        [TestMethod]
        public void CheckSignatureFrom_level2CAAndRootCA()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));
            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));

            bool optSuccess = certificateService.Certificate_CheckSignatureFrom(executionEngine);
            bool checkSignature = executionEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

            optSuccess.Should().BeTrue();
            checkSignature.Should().BeTrue();
        }


        [TestMethod]
        public void CheckSignatureFrom_level3CAAndLevel2CA()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level2CA));
            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level3CA));

            bool optSuccess = certificateService.Certificate_CheckSignatureFrom(executionEngine);
            bool checkSignature = executionEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

            optSuccess.Should().BeTrue();
            checkSignature.Should().BeTrue();
        }


        [TestMethod]
        public void CheckSignatureFrom_level4CAAndLevel3CA()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level3CA));
            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level4CA));

            bool optSuccess = certificateService.Certificate_CheckSignatureFrom(executionEngine);
            bool checkSignature = executionEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

            optSuccess.Should().BeTrue();
            checkSignature.Should().BeTrue();
        }

        [TestMethod]
        public void CheckSignatureFrom_level3CAAndRootCA()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));
            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(level3CA));

            bool optSuccess = certificateService.Certificate_CheckSignatureFrom(executionEngine);
            bool checkSignature = executionEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

            optSuccess.Should().BeTrue();
            checkSignature.Should().BeFalse();
        }


       [TestMethod]
       public void CheckSignatureFrom_SelfSignCA()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(selfSignCA));
            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(selfSignCA));

            bool optSuccess = certificateService.Certificate_CheckSignatureFrom(executionEngine);
            bool checkSignature = executionEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

            optSuccess.Should().BeTrue();
            checkSignature.Should().BeTrue();
        }

        [TestMethod]
        public void CheckSignatureFrom_Loss_CA()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            certificateService.Certificate_CheckSignatureFrom(executionEngine).Should().BeFalse();
            executionEngine.CurrentContext.EvaluationStack.Count.Should().Be(0);

            executionEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));
            certificateService.Certificate_CheckSignatureFrom(executionEngine).Should().BeFalse();
            executionEngine.CurrentContext.EvaluationStack.Count.Should().Be(0);
        }


        [TestMethod]
        public void CheckSignatureFrom_Param_Isnot_CA()
        {
            ExecutionEngine executionEngine = new ExecutionEngine(null, null, null, null);
            executionEngine.LoadScript(new byte[] { }, 0);

            executionEngine.CurrentContext.EvaluationStack.Push(true);
            certificateService.Certificate_CheckSignatureFrom(executionEngine).Should().BeFalse();
            executionEngine.CurrentContext.EvaluationStack.Count.Should().Be(0);
        }


        private static X509Certificate LoadX509(string path)
        {
            using (Stream stream = new FileStream(path, FileMode.Open))
            {
                X509CertificateParser x509CertificateParser = new X509CertificateParser();
                return x509CertificateParser.ReadCertificate(stream);
            }
        }
    }
}
