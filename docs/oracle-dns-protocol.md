# Oracle DNS Protocol

The Oracle plugin can serve DNS data securely by resolving `dns://` URLs through a DNS-over-HTTPS (DoH) gateway. This ensures oracle nodes read authoritative data directly from public resolvers (such as Cloudflare) without passing plaintext DNS queries over untrusted networks.

> **When should I use it?**  
> Any time you need on-chain access to TXT records (DKIM, SPF, DID documents, etc.), TLSA records, or X.509 material published via DNS.

## Enable and configure

1. Install or build the `OracleService` plugin and copy `OracleService.json` next to the plugin binary.
2. Add the `Dns` section (defaults shown):

```jsonc
{
  "PluginConfiguration": {
    // ...
    "Dns": {
      "EndPoint": "https://cloudflare-dns.com/dns-query",
      "Timeout": 5000
    }
  }
}
```

- `EndPoint` must point to a DoH resolver that understands the [application/dns-json](https://developers.cloudflare.com/api/operations/dns-over-https) format.
- `Timeout` is the maximum milliseconds the oracle will wait for a DoH response before returning `OracleResponseCode.Timeout`.

> You can run your own DoH gateway and point the oracle to it if you need custom trust anchors or strict egress controls.

## Constructing dns:// URLs

```
dns://<base-domain>/<path-selector>?selector=<label>&name=<override>&type=<rr-type>&format=<output>
```

| Parameter  | Description |
|------------|-------------|
| `base-domain` | Required host portion (e.g., `example.com`). |
| `path-selector` | Optional path segments (`/_acme-challenge.dkim`) used when no `selector=` query is supplied. |
| `selector` | Optional label joined with the base domain (`selector.example.com`). Useful for DKIM selectors. |
| `name` | Optional absolute DNS name. When set it overrides host, selector, and path components entirely. |
| `type` | Optional RR type (default `TXT`). Accepts standard mnemonics (`TXT`, `TLSA`, `CERT`, `A`, `AAAA`, …) or numeric values. |
| `format` | Optional output hint. Use `format=x509` to force TXT/CERT payloads to be parsed as DER certificates (the plugin fills the `Certificate` section). |

Only `base-domain` is required. The following are equivalent and resolve DKIM entry `_domainkey` selector `1alhai` under `icloud.com`:

```
dns://icloud.com?selector=1alhai._domainkey&type=TXT
dns://1alhai._domainkey.icloud.com?type=TXT
dns://icloud.com/1alhai._domainkey?type=TXT
```

## Response schema

Successful queries return UTF-8 JSON. Attributes correspond to the `ResultEnvelope` produced by the oracle:

```jsonc
{
  "Name": "1alhai._domainkey.icloud.com",
  "Type": "TXT",
  "Answers": [
    {
      "Name": "1alhai._domainkey.icloud.com",
      "Type": "TXT",
      "Ttl": 299,
      "Data": "\"k=rsa; p=...IDAQAB\""
    }
  ],
  "Certificate": {
    "Subject": "CN=example.com",
    "Issuer": "CN=Example Root",
    "Thumbprint": "ABCD1234...",
    "NotBefore": "2024-01-16T00:00:00Z",
    "NotAfter": "2025-01-16T00:00:00Z",
    "Der": "MIIC...",
    "PublicKeyAlgorithm": "RSA",
    "PublicKey": "MIIBIjANBg..."
  }
}
```

- `Answers` mirrors the DoH response but normalizes record types and names.
- `Certificate` is present only when `type=CERT` or `format=x509`. `Der` is the base64-encoded certificate, while `PublicKeyAlgorithm`/`PublicKey` expose the decoded subject public key info (base64-encoded raw key) so it can be stored or verified without parsing DER on-chain.
- If the DoH server responds with NXDOMAIN, the oracle returns `OracleResponseCode.NotFound`.
- Responses exceeding `OracleResponse.MaxResultSize` yield `OracleResponseCode.ResponseTooLarge`.

## Contract usage example

```csharp
public static void RequestAppleDkim()
{
    const string url = "dns://1alhai._domainkey.icloud.com?type=TXT";
    Oracle.Request(url, "", nameof(OnOracleCallback), Runtime.CallingScriptHash, 5_00000000);
}

public static void OnOracleCallback(string url, byte[] userData, int code, byte[] result)
{
    if (code != (int)OracleResponseCode.Success) throw new Exception("Oracle query failed");

    var envelope = (Neo.SmartContract.Framework.Services.Neo.Json.JsonObject)StdLib.JsonDeserialize(result);
    var answers = (Neo.SmartContract.Framework.Services.Neo.Json.JsonArray)envelope["Answers"];
    var txt = (Neo.SmartContract.Framework.Services.Neo.Json.JsonObject)answers[0];
    Storage.Put(Storage.CurrentContext, "dkim", txt["Data"].AsString());
}
```

Tips:

1. Budget enough `gasForResponse` to cover JSON payload size (TXT records are often kilobytes).
2. Validate TTL or fingerprint data before trusting it.
3. Combine oracle DNS data with existing filters (e.g., `Helper.JsonPath`/`OracleService.Filter`) if you only need a slice of the result.

## Manual testing

Use the same resolver the oracle will contact to inspect responses:

```bash
curl -s \
  -H 'accept: application/dns-json' \
  'https://cloudflare-dns.com/dns-query?name=1alhai._domainkey.icloud.com&type=TXT'
```

Compare the JSON payload with the data returned by your contract callback to ensure parity.
