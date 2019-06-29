using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_InteropService_NEO_Certificate
    {

        // rootCA(selfCA) ----> subCA
        X509Certificate rootCA;
        X509Certificate subCA;

        [TestInitialize]
        public void TestSetup()
        {
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


        private X509Certificate buildsCertificate(string subjectName, AsymmetricKeyParameter publicKey, AsymmetricKeyParameter signPrivateKey)
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
                X509Extensions.KeyUsage.Id,
                true,
                new KeyUsage(3)
                );
            gen.AddExtension(
                X509Extensions.AuthorityKeyIdentifier.Id,
                true,
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
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetRawTbsCertificate);
                byte[] rawTbs = applicationEngine.CurrentContext.EvaluationStack.Pop().GetByteArray();

                optSuccess.Should().BeTrue();
                rawTbs.ShouldBeEquivalentTo(rootCA.GetTbsCertificate());
            }
        }

        [TestMethod]
        public void Certificate_GetSignatureAlgorithm()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetSignatureAlgorithm);
                string signatureAlg = applicationEngine.CurrentContext.EvaluationStack.Pop().GetString();

                optSuccess.Should().BeTrue();
                signatureAlg.ShouldBeEquivalentTo(rootCA.SigAlgName);
            }
        }

        [TestMethod]
        public void Certificate_GetSignatureValue()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetSignatureValue);
                byte[] signatureValue = applicationEngine.CurrentContext.EvaluationStack.Pop().GetByteArray();

                optSuccess.Should().BeTrue();
                signatureValue.ShouldBeEquivalentTo(rootCA.GetSignature());
            }
        }

        [TestMethod]
        public void Certificate_GetVersion()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetVersion);
                int version = (int)applicationEngine.CurrentContext.EvaluationStack.Pop().GetBigInteger();

                optSuccess.Should().BeTrue();
                version.ShouldBeEquivalentTo(rootCA.Version);
            }
           
        }

        [TestMethod]
        public void Certificate_GetSerialNumber()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetSerialNumber);
                string serialNumber = applicationEngine.CurrentContext.EvaluationStack.Pop().GetString();
                string serialNumberStr = rootCA.SerialNumber.ToByteArray().ToHexString();

                optSuccess.Should().BeTrue();
                serialNumber.ShouldBeEquivalentTo(serialNumberStr);
            }
        }

        [TestMethod]
        public void Certificate_GetIssuer()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetIssuer);
                string issuer = applicationEngine.CurrentContext.EvaluationStack.Pop().GetString();

                optSuccess.Should().BeTrue();
                issuer.ShouldBeEquivalentTo(rootCA.IssuerDN.ToString());
            }
        }

        [TestMethod]
        public void Certificate_GetNotBefore()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetNotBefore);
                long notBefore = (long)applicationEngine.CurrentContext.EvaluationStack.Pop().GetBigInteger();

                optSuccess.Should().BeTrue();
                notBefore.ShouldBeEquivalentTo(new DateTimeOffset(rootCA.NotBefore).ToUnixTimeSeconds());
            }
            
        }


        [TestMethod]
        public void Certificate_GetNotAfter()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetNotAfter);
                long notBefore = (long)applicationEngine.CurrentContext.EvaluationStack.Pop().GetBigInteger();

                optSuccess.Should().BeTrue();
                notBefore.ShouldBeEquivalentTo(new DateTimeOffset(rootCA.NotAfter).ToUnixTimeSeconds());
            }
        }


        [TestMethod]
        public void Certificate_GetSubject()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetSubject);
                string subject = applicationEngine.CurrentContext.EvaluationStack.Pop().GetString();

                optSuccess.Should().BeTrue();
                subject.ShouldBeEquivalentTo(rootCA.SubjectDN.ToString());
            }
        }


        [TestMethod]
        public void Certificate_Decode()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                applicationEngine.CurrentContext.EvaluationStack.Push(rootCA.GetEncoded());

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_Decode);
                InteropInterface _interface = (InteropInterface)applicationEngine.CurrentContext.EvaluationStack.Pop();
                X509Certificate cerficate = _interface.GetInterface<X509Certificate>();

                optSuccess.Should().BeTrue();
                cerficate.GetTbsCertificate().ShouldBeEquivalentTo(rootCA.GetTbsCertificate());
            }
        }

        [TestMethod]
        public void Certificate_GetBasicConstraints()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);

                string authorityKeyId = X509Extensions.AuthorityKeyIdentifier.Id;
                applicationEngine.CurrentContext.EvaluationStack.Push(authorityKeyId);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetBasicConstraints);
                System.Numerics.BigInteger path = applicationEngine.CurrentContext.EvaluationStack.Pop().GetBigInteger();

                optSuccess.Should().BeTrue();
                path.Should().Be(-1);
            }
        }


        [TestMethod]
        public void Certificate_GetKeyUsage()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);
                string keyUsage = X509Extensions.KeyUsage.Id;
                applicationEngine.CurrentContext.EvaluationStack.Push(keyUsage);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetKeyUsage);
                VM.Types.Array array = (VM.Types.Array)applicationEngine.CurrentContext.EvaluationStack.Pop();

                optSuccess.Should().BeTrue();
                array.Count.Should().Be(9);
            }
        }


        [TestMethod]
        public void Certificate_GetExtensionValue()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);

                string authorityKeyId = X509Extensions.AuthorityKeyIdentifier.Id;
                applicationEngine.CurrentContext.EvaluationStack.Push(authorityKeyId);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetExtensionValue);
                Map map = (Map)applicationEngine.CurrentContext.EvaluationStack.Pop();

                optSuccess.Should().BeTrue();

                map.ContainsKey("Id").Should().BeTrue();
                map.ContainsKey("Critical").Should().BeTrue();
                map.ContainsKey("Value").Should().BeTrue();

                map.TryGetValue("Id", out StackItem id);
                id.GetString().ShouldBeEquivalentTo(authorityKeyId);

                map.TryGetValue("Critical", out StackItem critical);
                critical.GetBoolean().Should().BeTrue();

                map.TryGetValue("Value", out StackItem value);
                byte[] rootValue = rootCA.GetExtensionValue(new DerObjectIdentifier(authorityKeyId)).GetOctets();
                value.GetByteArray().ShouldBeEquivalentTo(rootValue);
            }
        }


        [TestMethod]
        public void Certificate_GetExtensionValue_NotExit_Id()
        {
            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);

                string authorityKeyId = "2.19.2.2";
                applicationEngine.CurrentContext.EvaluationStack.Push(authorityKeyId);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_GetExtensionValue);
                optSuccess.Should().BeFalse();
            }
        }


        [TestMethod]
        public void CheckSignature_RootCA()
        {
            string sigAlgName = rootCA.SigAlgName;
            byte[] signature = rootCA.GetSignature();
            byte[] signedData = rootCA.GetTbsCertificate();

            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);

                applicationEngine.CurrentContext.EvaluationStack.Push(signedData);
                applicationEngine.CurrentContext.EvaluationStack.Push(signature);
                applicationEngine.CurrentContext.EvaluationStack.Push(sigAlgName);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_CheckSignature);
                bool checkSignature = applicationEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

                optSuccess.Should().BeTrue();
                checkSignature.Should().BeTrue();
            }
        }

        [TestMethod]
        public void CheckSignature_SubCA()
        {
            string sigAlgName = subCA.SigAlgName;
            byte[] signature = subCA.GetSignature();
            byte[] signedData = subCA.GetTbsCertificate();

            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);

                applicationEngine.CurrentContext.EvaluationStack.Push(signedData);
                applicationEngine.CurrentContext.EvaluationStack.Push(signature);
                applicationEngine.CurrentContext.EvaluationStack.Push(sigAlgName);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                 bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_CheckSignature);
                bool checkSignature = applicationEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

                optSuccess.Should().BeTrue();
                checkSignature.Should().BeTrue();
            }
        }


        [TestMethod]
        public void CheckSignature_Failure()
        {
            string sigAlgName = rootCA.SigAlgName;
            byte[] signature = rootCA.GetSignature();
            byte[] signedData = rootCA.GetTbsCertificate();

            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                applicationEngine.LoadScript(new byte[] { }, 0);

                signedData[0] = 0x00;
                signedData[1] = 0x00;
                signedData[2] = 0x00;
                signedData[3] = 0x00;
                applicationEngine.CurrentContext.EvaluationStack.Push(signedData);
                applicationEngine.CurrentContext.EvaluationStack.Push(signature);
                applicationEngine.CurrentContext.EvaluationStack.Push(sigAlgName);
                applicationEngine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(rootCA));

                bool optSuccess = InteropService.Invoke(applicationEngine, InteropService.Neo_Certificate_CheckSignature);
                bool checkSignature = applicationEngine.CurrentContext.EvaluationStack.Pop().GetBoolean();

                optSuccess.Should().BeTrue();
                checkSignature.Should().BeFalse();
            }
        }

    }
}
