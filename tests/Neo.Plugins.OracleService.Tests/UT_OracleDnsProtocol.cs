// Copyright (C) 2015-2025 The Neo Project.
//
// UT_OracleDnsProtocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Neo.Plugins.OracleService.Tests
{
    [TestClass]
    public class UT_OracleDnsProtocol
    {
        [TestInitialize]
        public void Setup()
        {
            LoadSettings();
        }

        [TestMethod]
        public void BuildQueryName_UsesSelectorFromQuery()
        {
            var uri = new Uri("dns://example.com?selector=oracle");
            string name = OracleDnsProtocol.BuildQueryName(uri);
            Assert.AreEqual("oracle.example.com", name);
        }

        [TestMethod]
        public void BuildQueryName_UsesPathFallback()
        {
            var uri = new Uri("dns://example.com/_acme-challenge.dkim");
            string name = OracleDnsProtocol.BuildQueryName(uri);
            Assert.AreEqual("_acme-challenge.dkim.example.com", name);
        }

        [TestMethod]
        public async Task ProcessAsync_ReturnsCertificateFromTxtRecord()
        {
            string base64Cert = GenerateCertificateBase64("CN=example.com");
            var dohResponse = new
            {
                Status = 0,
                Answer = new[]
                {
                    new { name = "oracle.example.com.", type = 16, ttl = 60, data = $"\"{base64Cert}\"" }
                }
            };
            string json = JsonSerializer.Serialize(dohResponse);
            var handler = new StubHandler(request =>
            {
                Assert.IsTrue(request.RequestUri.Query.Contains("name=oracle.example.com", StringComparison.Ordinal));
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/dns-json")
                };
                return response;
            });
            using var protocol = new OracleDnsProtocol(handler);
            (OracleResponseCode code, string payload) = await protocol.ProcessAsync(new Uri("dns://example.com?selector=oracle&format=x509"), CancellationToken.None);
            Assert.AreEqual(OracleResponseCode.Success, code);
            Assert.IsNotNull(payload);
            using JsonDocument doc = JsonDocument.Parse(payload);
            Assert.AreEqual("oracle.example.com", doc.RootElement.GetProperty("Name").GetString());
            var certElement = doc.RootElement.GetProperty("Certificate");
            Assert.AreEqual(base64Cert, certElement.GetProperty("Der").GetString());
            using var parsedCert = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(base64Cert));
            var pkElement = certElement.GetProperty("PublicKey");
            string expectedPublicKey = Convert.ToBase64String(parsedCert.GetPublicKey());
            string expectedAlgorithm = parsedCert.PublicKey.Oid?.FriendlyName ?? parsedCert.PublicKey.Oid?.Value;
            Assert.AreEqual(expectedPublicKey, pkElement.GetProperty("Encoded").GetString());
            Assert.AreEqual(expectedAlgorithm, pkElement.GetProperty("Algorithm").GetString());
            using RSA rsa = parsedCert.GetRSAPublicKey();
            Assert.IsNotNull(rsa);
            RSAParameters parameters = rsa.ExportParameters(false);
            Assert.AreEqual(Convert.ToHexString(parameters.Modulus), pkElement.GetProperty("Modulus").GetString());
            Assert.AreEqual(Convert.ToHexString(parameters.Exponent), pkElement.GetProperty("Exponent").GetString());
        }

        [TestMethod]
        public async Task ProcessAsync_ReturnsEcPublicKeyFields()
        {
            string base64Cert = GenerateEcCertificateBase64("CN=example-ec.com");
            var dohResponse = new
            {
                Status = 0,
                Answer = new[]
                {
                    new { name = "ec.example.com.", type = 16, ttl = 60, data = $"\"{base64Cert}\"" }
                }
            };
            string json = JsonSerializer.Serialize(dohResponse);
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/dns-json")
            });
            using var protocol = new OracleDnsProtocol(handler);
            (OracleResponseCode code, string payload) = await protocol.ProcessAsync(new Uri("dns://ec.example.com?format=x509"), CancellationToken.None);
            Assert.AreEqual(OracleResponseCode.Success, code);
            using JsonDocument doc = JsonDocument.Parse(payload);
            var pkElement = doc.RootElement.GetProperty("Certificate").GetProperty("PublicKey");
            using var parsedCert = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(base64Cert));
            using ECDsa ecdsa = parsedCert.GetECDsaPublicKey();
            Assert.IsNotNull(ecdsa);
            ECParameters parameters = ecdsa.ExportParameters(false);
            string expectedCurve = parameters.Curve.Oid?.FriendlyName ?? parameters.Curve.Oid?.Value;
            Assert.AreEqual(expectedCurve, pkElement.GetProperty("Curve").GetString());
            Assert.AreEqual(Convert.ToHexString(parameters.Q.X), pkElement.GetProperty("X").GetString());
            Assert.AreEqual(Convert.ToHexString(parameters.Q.Y), pkElement.GetProperty("Y").GetString());
        }

        [TestMethod]
        public async Task ProcessAsync_ReturnsNotFoundForNxDomain()
        {
            const string response = "{\"Status\":3}";
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/dns-json")
            });
            using var protocol = new OracleDnsProtocol(handler);
            (OracleResponseCode code, string payload) = await protocol.ProcessAsync(new Uri("dns://example.com"), CancellationToken.None);
            Assert.AreEqual(OracleResponseCode.NotFound, code);
            Assert.IsNull(payload);
        }

        [TestMethod]
        public async Task ProcessAsync_ParsesCloudflareTxtExample()
        {
            const string dohResponse = """
{
  "Status": 0,
  "TC": false,
  "RD": true,
  "RA": true,
  "AD": true,
  "CD": false,
  "Question": [
    { "name": "1alhai._domainkey.icloud.com.", "type": 16 }
  ],
  "Answer": [
    {
      "name": "1alhai._domainkey.icloud.com.",
      "type": 16,
      "TTL": 299,
      "data": "\"k=rsa; p=MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAp1+6V9wVDqveufqdpypuXn7Z1xXHrp236UMtO4Zwzp1KimG1HjMATkUMlzUxr87hcPLZ9eczsQnUnxE27XGr0C+MEY0S8NxVkg4CSkiUbSSjMBDuNIQP5CKEM5Qn2ATqNnS/xPbbGr3HdWu3UwG+329xNXO/SuKD5d/mswHxZ34rnOG0r8QwMCKaRZ3eLaxhUJW6QcgO5Kb/6VQwWi4KFOeFHrgb3R04QLbTjaCj1eO0MJdHj7FVGHvXZHzVvzJeY9q24apqYh6gMPkTFogyXv3gZH/BqhGlymM4T/6QAEyy6AdZkGouVp21Hb+Jseb3CidRubc4QZAlWTMwVzKhI6+wIDAQAB\""
    }
  ],
  "Comment": "Mocked Cloudflare DoH response"
}
""";
            const string expectedTxt = "\"k=rsa; p=MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAp1+6V9wVDqveufqdpypuXn7Z1xXHrp236UMtO4Zwzp1KimG1HjMATkUMlzUxr87hcPLZ9eczsQnUnxE27XGr0C+MEY0S8NxVkg4CSkiUbSSjMBDuNIQP5CKEM5Qn2ATqNnS/xPbbGr3HdWu3UwG+329xNXO/SuKD5d/mswHxZ34rnOG0r8QwMCKaRZ3eLaxhUJW6QcgO5Kb/6VQwWi4KFOeFHrgb3R04QLbTjaCj1eO0MJdHj7FVGHvXZHzVvzJeY9q24apqYh6gMPkTFogyXv3gZH/BqhGlymM4T/6QAEyy6AdZkGouVp21Hb+Jseb3CidRubc4QZAlWTMwVzKhI6+wIDAQAB\"";
            var handler = new StubHandler(request =>
            {
                Assert.IsTrue(request.Headers.Accept.Any(h => h.MediaType == "application/dns-json"));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(dohResponse, Encoding.UTF8, "application/dns-json")
                };
            });
            using var protocol = new OracleDnsProtocol(handler);
            (OracleResponseCode code, string payload) = await protocol.ProcessAsync(new Uri("dns://1alhai._domainkey.icloud.com?type=TXT"), CancellationToken.None);
            Assert.AreEqual(OracleResponseCode.Success, code);
            using JsonDocument doc = JsonDocument.Parse(payload);
            Assert.AreEqual("1alhai._domainkey.icloud.com", doc.RootElement.GetProperty("Name").GetString());
            var answers = doc.RootElement.GetProperty("Answers");
            Assert.AreEqual(1, answers.GetArrayLength());
            Assert.AreEqual("TXT", answers[0].GetProperty("Type").GetString());
            Assert.AreEqual(expectedTxt, answers[0].GetProperty("Data").GetString());
        }

        private static void LoadSettings()
        {
            var values = new Dictionary<string, string>
            {
                ["PluginConfiguration:Network"] = "5195086",
                ["PluginConfiguration:Nodes:0"] = "http://127.0.0.1:20332",
                ["PluginConfiguration:AllowedContentTypes:0"] = "application/json",
                ["PluginConfiguration:Https:Timeout"] = "5000",
                ["PluginConfiguration:NeoFS:EndPoint"] = "http://127.0.0.1:8080",
                ["PluginConfiguration:NeoFS:Timeout"] = "15000",
                ["PluginConfiguration:Dns:EndPoint"] = "https://unit.test/dns-query",
                ["PluginConfiguration:Dns:Timeout"] = "3000"
            };
            IConfigurationSection section = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build()
                .GetSection("PluginConfiguration");
            OracleSettings.Load(section);
        }

        private static string GenerateCertificateBase64(string subject)
        {
            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            return Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
        }

        private static string GenerateEcCertificateBase64(string subject)
        {
            using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest(subject, ecdsa, HashAlgorithmName.SHA256);
            using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            return Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                this.responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(responder(request));
            }
        }
    }
}
