// Copyright (C) 2015-2025 The Neo Project.
//
// OracleDnsProtocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Neo.Plugins.OracleService
{
    class OracleDnsProtocol : IOracleProtocol
    {
        private sealed class DohAnswer
        {
            public string Name { get; set; }
            public int Type { get; set; }
            public uint Ttl { get; set; }
            public string Data { get; set; }
        }

        private sealed class DohResponse
        {
            public int Status { get; set; }
            public DohAnswer[] Answer { get; set; }
        }

        private sealed class ResultAnswer
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public uint Ttl { get; set; }
            public string Data { get; set; }
        }

        private sealed class CertificateResult
        {
            public string Subject { get; set; }
            public string Issuer { get; set; }
            public string Thumbprint { get; set; }
            public DateTime NotBefore { get; set; }
            public DateTime NotAfter { get; set; }
            public string Der { get; set; }
            public CertificatePublicKey PublicKey { get; set; }
        }

        private sealed class CertificatePublicKey
        {
            public string Algorithm { get; set; }
            public string Encoded { get; set; }
            public string Modulus { get; set; }
            public string Exponent { get; set; }
            public string Curve { get; set; }
            public string X { get; set; }
            public string Y { get; set; }
        }

        private sealed class ResultEnvelope
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public ResultAnswer[] Answers { get; set; }
            public CertificateResult Certificate { get; set; }
        }

        private static readonly IReadOnlyDictionary<string, int> RecordTypeLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 1,
            ["NS"] = 2,
            ["CNAME"] = 5,
            ["SOA"] = 6,
            ["MX"] = 15,
            ["TXT"] = 16,
            ["AAAA"] = 28,
            ["SRV"] = 33,
            ["CERT"] = 37,
            ["DNSKEY"] = 48,
            ["TLSA"] = 52,
        };

        private static readonly IReadOnlyDictionary<int, string> ReverseRecordTypeLookup =
            RecordTypeLookup.ToDictionary(p => p.Value, p => p.Key);

        private readonly HttpClient client;
        private readonly JsonSerializerOptions serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly JsonSerializerOptions resultSerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        private readonly object syncRoot = new();
        private bool configured;
        private Uri endpoint;

        public OracleDnsProtocol(HttpMessageHandler handler = null)
        {
            client = handler is null ? new HttpClient() : new HttpClient(handler);
            CustomAttributeData attribute = Assembly.GetExecutingAssembly().CustomAttributes.First(p => p.AttributeType == typeof(AssemblyInformationalVersionAttribute));
            string version = (string)attribute.ConstructorArguments[0].Value;
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NeoOracleService", version));
        }

        public void Configure()
        {
            EnsureConfigured(force: true);
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public async Task<(OracleResponseCode, string)> ProcessAsync(Uri uri, CancellationToken cancellation)
        {
            EnsureConfigured();

            string queryName;
            NameValueCollection query;
            try
            {
                query = HttpUtility.ParseQueryString(uri.Query);
                queryName = BuildQueryName(uri, query);
            }
            catch (Exception ex)
            {
                return (OracleResponseCode.Error, ex.Message);
            }

            int recordType;
            string recordTypeLabel;
            try
            {
                recordType = ParseRecordType(query);
                recordTypeLabel = GetRecordTypeLabel(recordType);
            }
            catch (Exception ex)
            {
                return (OracleResponseCode.Error, ex.Message);
            }

            bool wantsCertificate = recordType == RecordTypeLookup["CERT"] || ShouldExtractCertificate(query);
            Utility.Log(nameof(OracleDnsProtocol), LogLevel.Debug, $"Request: {queryName} ({recordTypeLabel})");

            DohResponse dohResponse;
            try
            {
                dohResponse = await ResolveAsync(queryName, recordType, cancellation);
            }
            catch (TaskCanceledException)
            {
                return (OracleResponseCode.Timeout, null);
            }
            catch (Exception ex)
            {
                return (OracleResponseCode.Error, ex.Message);
            }

            if (dohResponse is null)
                return (OracleResponseCode.Error, "Invalid DNS response.");

            if (dohResponse.Status == 3)
                return (OracleResponseCode.NotFound, null);

            if (dohResponse.Status != 0)
                return (OracleResponseCode.Error, $"DNS error {dohResponse.Status}");

            if (dohResponse.Answer is null || dohResponse.Answer.Length == 0)
                return (OracleResponseCode.NotFound, null);

            ResultAnswer[] answers = dohResponse.Answer
                .Select(a => new ResultAnswer
                {
                    Name = a.Name?.TrimEnd('.'),
                    Type = GetRecordTypeLabel(a.Type),
                    Ttl = a.Ttl,
                    Data = a.Data
                })
                .ToArray();

            CertificateResult certificate = null;
            if (wantsCertificate && TryBuildCertificate(dohResponse.Answer, out certificate))
                Utility.Log(nameof(OracleDnsProtocol), LogLevel.Debug, $"Certificate extracted for {queryName}");

            ResultEnvelope envelope = new()
            {
                Name = queryName,
                Type = recordTypeLabel,
                Answers = answers,
                Certificate = certificate
            };

            string payload = JsonSerializer.Serialize(envelope, resultSerializerOptions);
            if (Encoding.UTF8.GetByteCount(payload) > OracleResponse.MaxResultSize)
                return (OracleResponseCode.ResponseTooLarge, null);

            return (OracleResponseCode.Success, payload);
        }

        private async Task<DohResponse> ResolveAsync(string name, int type, CancellationToken cancellation)
        {
            Uri requestUri = BuildRequestUri(name, type);
            using HttpResponseMessage response = await client.GetAsync(requestUri, cancellation);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"DoH endpoint returned {(int)response.StatusCode} ({response.StatusCode})");
            await using var stream = await response.Content.ReadAsStreamAsync(cancellation);
            return await JsonSerializer.DeserializeAsync<DohResponse>(stream, serializerOptions, cancellation);
        }

        private Uri BuildRequestUri(string name, int type)
        {
            UriBuilder builder = new(endpoint);
            NameValueCollection existingQuery = HttpUtility.ParseQueryString(builder.Query ?? string.Empty);
            existingQuery["name"] = name.TrimEnd('.');
            existingQuery["type"] = type.ToString(CultureInfo.InvariantCulture);
            builder.Query = existingQuery.ToString();
            return builder.Uri;
        }

        private void EnsureConfigured(bool force = false)
        {
            if (configured && !force)
                return;
            lock (syncRoot)
            {
                if (configured && !force)
                    return;
                var dnsSettings = OracleSettings.Default?.Dns ?? throw new InvalidOperationException("DNS settings are not loaded.");
                endpoint = dnsSettings.EndPoint;
                client.Timeout = dnsSettings.Timeout;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.ParseAdd("application/dns-json");
                configured = true;
            }
        }

        internal static string BuildQueryName(Uri uri, NameValueCollection queryParameters = null)
        {
            queryParameters ??= HttpUtility.ParseQueryString(uri.Query);
            string overriddenName = NormalizeLabel(queryParameters["name"]);
            if (!string.IsNullOrEmpty(overriddenName))
                return overriddenName.TrimEnd('.');

            string host = NormalizeLabel(uri.Host);
            if (string.IsNullOrEmpty(host))
                throw new FormatException("dns:// url must include a domain host");

            string selector = NormalizeLabel(queryParameters["selector"]);
            if (string.IsNullOrEmpty(selector))
            {
                string pathSelector = NormalizePathSelector(uri.AbsolutePath);
                selector = NormalizeLabel(pathSelector);
            }

            if (string.IsNullOrEmpty(selector))
                return host;

            return $"{selector}.{host}".Trim('.');
        }

        private static string NormalizeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return Uri.UnescapeDataString(value).Trim().Trim('.');
        }

        private static string NormalizePathSelector(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            string trimmed = path.Trim('/');
            if (string.IsNullOrEmpty(trimmed))
                return null;
            string decoded = Uri.UnescapeDataString(trimmed);
            return decoded.Replace('/', '.');
        }

        private static int ParseRecordType(NameValueCollection query)
        {
            string typeRaw = query["type"];
            if (string.IsNullOrWhiteSpace(typeRaw))
                return RecordTypeLookup["TXT"];
            typeRaw = typeRaw.Trim();
            if (int.TryParse(typeRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric))
                return numeric;
            if (RecordTypeLookup.TryGetValue(typeRaw, out int mapped))
                return mapped;
            throw new FormatException($"Unsupported DNS record type '{typeRaw}'");
        }

        private static string GetRecordTypeLabel(int type)
        {
            if (ReverseRecordTypeLookup.TryGetValue(type, out string label))
                return label;
            return type.ToString(CultureInfo.InvariantCulture);
        }

        private static bool ShouldExtractCertificate(NameValueCollection query)
        {
            string format = query["format"];
            if (string.IsNullOrWhiteSpace(format))
                return false;
            return format.Equals("x509", StringComparison.OrdinalIgnoreCase)
                || format.Equals("cert", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryBuildCertificate(IEnumerable<DohAnswer> answers, out CertificateResult certificate)
        {
            certificate = null;
            foreach (var answer in answers)
            {
                if (!CanContainCertificate(answer))
                    continue;

                if (!TryExtractCertificateBytes(answer.Data, out byte[] raw))
                    continue;

                try
                {
                    using X509Certificate2 cert = X509CertificateLoader.LoadCertificate(raw);
                    certificate = new CertificateResult
                    {
                        Subject = cert.Subject,
                        Issuer = cert.Issuer,
                        Thumbprint = cert.Thumbprint,
                        NotBefore = cert.NotBefore,
                        NotAfter = cert.NotAfter,
                        Der = Convert.ToBase64String(cert.Export(X509ContentType.Cert)),
                        PublicKey = BuildPublicKey(cert)
                    };
                    return true;
                }
                catch
                {
                    // Skip invalid certificate payloads
                }
            }
            return false;
        }

        private static CertificatePublicKey BuildPublicKey(X509Certificate2 cert)
        {
            CertificatePublicKey key = new()
            {
                Algorithm = cert.PublicKey.Oid?.FriendlyName ?? cert.PublicKey.Oid?.Value,
                Encoded = Convert.ToBase64String(cert.GetPublicKey())
            };

            try
            {
                using RSA rsa = cert.GetRSAPublicKey();
                if (rsa is not null)
                {
                    RSAParameters parameters = rsa.ExportParameters(false);
                    key.Modulus = Convert.ToHexString(parameters.Modulus);
                    key.Exponent = Convert.ToHexString(parameters.Exponent);
                    return key;
                }
            }
            catch
            {
                // ignore and fall through
            }

            try
            {
                using ECDsa ecdsa = cert.GetECDsaPublicKey();
                if (ecdsa is not null)
                {
                    ECParameters parameters = ecdsa.ExportParameters(false);
                    key.Curve = parameters.Curve.Oid?.FriendlyName ?? parameters.Curve.Oid?.Value;
                    key.X = Convert.ToHexString(parameters.Q.X);
                    key.Y = Convert.ToHexString(parameters.Q.Y);
                    return key;
                }
            }
            catch
            {
            }

            return key;
        }

        private static bool CanContainCertificate(DohAnswer answer)
        {
            if (answer is null || string.IsNullOrEmpty(answer.Data))
                return false;
            return answer.Type == RecordTypeLookup["CERT"] || answer.Type == RecordTypeLookup["TXT"];
        }

        private static bool TryExtractCertificateBytes(string payload, out byte[] raw)
        {
            raw = null;
            if (string.IsNullOrWhiteSpace(payload))
                return false;

            string cleaned = payload.Trim();
            int lastSpace = cleaned.LastIndexOf(' ');
            if (lastSpace > 0)
            {
                string tail = cleaned[(lastSpace + 1)..];
                if (TryDecodeBase64(tail, out raw))
                    return true;
            }

            string normalized = cleaned.Replace("\"", string.Empty, StringComparison.Ordinal)
                                       .Replace(" ", string.Empty, StringComparison.Ordinal);
            return TryDecodeBase64(normalized, out raw);
        }

        private static bool TryDecodeBase64(string input, out byte[] data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(input))
                return false;
            try
            {
                data = Convert.FromBase64String(input);
                return data.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
