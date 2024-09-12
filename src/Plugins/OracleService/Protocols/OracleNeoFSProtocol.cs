// Copyright (C) 2015-2024 The Neo Project.
//
// OracleNeoFSProtocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.FileStorage.API.Client;
using Neo.FileStorage.API.Cryptography;
using Neo.FileStorage.API.Refs;
using Neo.Network.P2P.Payloads;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Object = Neo.FileStorage.API.Object.Object;
using Range = Neo.FileStorage.API.Object.Range;

namespace Neo.Plugins.OracleService
{
    class OracleNeoFSProtocol : IOracleProtocol
    {
        private readonly System.Security.Cryptography.ECDsa privateKey;

        public OracleNeoFSProtocol(Wallet wallet, ECPoint[] oracles)
        {
            byte[] key = oracles.Select(p => wallet.GetAccount(p)).Where(p => p is not null && p.HasKey && !p.Lock).FirstOrDefault().GetKey().PrivateKey;
            privateKey = key.LoadPrivateKey();
        }

        public void Configure()
        {
        }

        public void Dispose()
        {
            privateKey.Dispose();
        }

        public async Task<(OracleResponseCode, string)> ProcessAsync(Uri uri, CancellationToken cancellation)
        {
            Utility.Log(nameof(OracleNeoFSProtocol), LogLevel.Debug, $"Request: {uri.AbsoluteUri}");
            try
            {
                (OracleResponseCode code, string data) = await GetAsync(uri, Settings.Default.NeoFS.EndPoint, cancellation);
                Utility.Log(nameof(OracleNeoFSProtocol), LogLevel.Debug, $"NeoFS result, code: {code}, data: {data}");
                return (code, data);
            }
            catch (Exception e)
            {
                Utility.Log(nameof(OracleNeoFSProtocol), LogLevel.Debug, $"NeoFS result: error,{e.Message}");
                return (OracleResponseCode.Error, null);
            }
        }


        /// <summary>
        /// GetAsync returns neofs object from the provided url.
        /// If Command is not provided, full object is requested.
        /// </summary>
        /// <param name="uri">URI scheme is "neofs:ContainerID/ObjectID/Command/offset|length".</param>
        /// <param name="host">Client host.</param>
        /// <param name="cancellation">Cancellation token object.</param>
        /// <returns>Returns neofs object.</returns>
        private async Task<(OracleResponseCode, string)> GetAsync(Uri uri, string host, CancellationToken cancellation)
        {
            string[] ps = uri.AbsolutePath.Split("/");
            if (ps.Length < 2) throw new FormatException("Invalid neofs url");
            ContainerID containerID = ContainerID.FromString(ps[0]);
            ObjectID objectID = ObjectID.FromString(ps[1]);
            Address objectAddr = new()
            {
                ContainerId = containerID,
                ObjectId = objectID
            };
            using Client client = new(privateKey, host);
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            tokenSource.CancelAfter(Settings.Default.NeoFS.Timeout);
            if (ps.Length == 2)
                return GetPayload(client, objectAddr, tokenSource.Token);
            return ps[2] switch
            {
                "range" => await GetRangeAsync(client, objectAddr, ps.Skip(3).ToArray(), tokenSource.Token),
                "header" => (OracleResponseCode.Success, await GetHeaderAsync(client, objectAddr, tokenSource.Token)),
                "hash" => (OracleResponseCode.Success, await GetHashAsync(client, objectAddr, ps.Skip(3).ToArray(), tokenSource.Token)),
                _ => throw new Exception("invalid command")
            };
        }

        private static (OracleResponseCode, string) GetPayload(Client client, Address addr, CancellationToken cancellation)
        {
            var objReader = client.GetObjectInit(addr, options: new CallOptions { Ttl = 2 }, context: cancellation);
            var obj = objReader.ReadHeader();
            if (obj.PayloadSize > OracleResponseAttribute.MaxResultSize)
                return (OracleResponseCode.ResponseTooLarge, "");
            var payload = new byte[obj.PayloadSize];
            int offset = 0;
            while (true)
            {
                if ((ulong)offset > obj.PayloadSize) return (OracleResponseCode.ResponseTooLarge, "");
                (byte[] chunk, bool ok) = objReader.ReadChunk();
                if (!ok) break;
                Array.Copy(chunk, 0, payload, offset, chunk.Length);
                offset += chunk.Length;
            }
            return (OracleResponseCode.Success, Utility.StrictUTF8.GetString(payload));
        }

        private static async Task<(OracleResponseCode, string)> GetRangeAsync(Client client, Address addr, string[] ps, CancellationToken cancellation)
        {
            if (ps.Length == 0) throw new FormatException("missing object range (expected 'Offset|Length')");
            Range range = ParseRange(ps[0]);
            if (range.Length > OracleResponseAttribute.MaxResultSize) return (OracleResponseCode.ResponseTooLarge, "");
            var res = await client.GetObjectPayloadRangeData(addr, range, options: new CallOptions { Ttl = 2 }, context: cancellation);
            return (OracleResponseCode.Success, Utility.StrictUTF8.GetString(res));
        }

        private static async Task<string> GetHeaderAsync(Client client, Address addr, CancellationToken cancellation)
        {
            var obj = await client.GetObjectHeader(addr, options: new CallOptions { Ttl = 2 }, context: cancellation);
            return obj.ToString();
        }

        private static async Task<string> GetHashAsync(Client client, Address addr, string[] ps, CancellationToken cancellation)
        {
            if (ps.Length == 0 || ps[0] == "")
            {
                Object obj = await client.GetObjectHeader(addr, options: new CallOptions { Ttl = 2 }, context: cancellation);
                return $"\"{new UInt256(obj.PayloadChecksum.Sum.ToByteArray())}\"";
            }
            Range range = ParseRange(ps[0]);
            List<byte[]> hashes = await client.GetObjectPayloadRangeHash(addr, new List<Range>() { range }, ChecksumType.Sha256, Array.Empty<byte>(), new CallOptions { Ttl = 2 }, cancellation);
            if (hashes.Count == 0) throw new Exception("empty response, object range is invalid (expected 'Offset|Length')");
            return $"\"{new UInt256(hashes[0])}\"";
        }

        private static Range ParseRange(string s)
        {
            string url = HttpUtility.UrlDecode(s);
            int sepIndex = url.IndexOf("|");
            if (sepIndex < 0) throw new Exception("object range is invalid (expected 'Offset|Length')");
            ulong offset = ulong.Parse(url[..sepIndex]);
            ulong length = ulong.Parse(url[(sepIndex + 1)..]);
            return new Range() { Offset = offset, Length = length };
        }
    }
}
