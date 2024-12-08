// Copyright (C) 2015-2024 The Neo Project.
//
// RpcServer.SmartContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Array = System.Array;

namespace Neo.Plugins.RpcServer
{
    partial class Server
    {
        private readonly Dictionary<Guid, Session> sessions = new();
        private Timer timer;

        private void Initialize_SmartContract()
        {
            if (settings.SessionEnabled)
                timer = new(OnTimer, null, settings.SessionExpirationTime, settings.SessionExpirationTime);
        }

        internal void Dispose_SmartContract()
        {
            timer?.Dispose();
            Session[] toBeDestroyed;
            lock (sessions)
            {
                toBeDestroyed = sessions.Values.ToArray();
                sessions.Clear();
            }
            foreach (Session session in toBeDestroyed)
                session.Dispose();
        }

        internal void OnTimer(object state)
        {
            List<(Guid Id, Session Session)> toBeDestroyed = new();
            lock (sessions)
            {
                foreach (var (id, session) in sessions)
                    if (DateTime.UtcNow >= session.StartTime + settings.SessionExpirationTime)
                        toBeDestroyed.Add((id, session));
                foreach (var (id, _) in toBeDestroyed)
                    sessions.Remove(id);
            }
            foreach (var (_, session) in toBeDestroyed)
                session.Dispose();
        }

        private JObject GetInvokeResult(byte[] script, Signer[] signers = null, Witness[] witnesses = null, bool useDiagnostic = false)
        {
            JObject json = new();
            Session session = new(system, script, signers, witnesses, settings.MaxGasInvoke, useDiagnostic ? new Diagnostic() : null);
            try
            {
                json["script"] = Convert.ToBase64String(script);
                json["state"] = session.Engine.State;
                // Gas consumed in the unit of datoshi, 1 GAS = 10^8 datoshi
                json["gasconsumed"] = session.Engine.FeeConsumed.ToString();
                json["exception"] = GetExceptionMessage(session.Engine.FaultException);
                json["notifications"] = new JArray(session.Engine.Notifications.Select(n =>
                {
                    var obj = new JObject();
                    obj["eventname"] = n.EventName;
                    obj["contract"] = n.ScriptHash.ToString();
                    obj["state"] = ToJson(n.State, session);
                    return obj;
                }));
                if (useDiagnostic)
                {
                    Diagnostic diagnostic = (Diagnostic)session.Engine.Diagnostic;
                    json["diagnostics"] = new JObject()
                    {
                        ["invokedcontracts"] = ToJson(diagnostic.InvocationTree.Root),
                        ["storagechanges"] = ToJson(session.Engine.SnapshotCache.GetChangeSet())
                    };
                }
                var stack = new JArray();
                foreach (var item in session.Engine.ResultStack)
                {
                    try
                    {
                        stack.Add(ToJson(item, session));
                    }
                    catch (Exception ex)
                    {
                        stack.Add("error: " + ex.Message);
                    }
                }
                json["stack"] = stack;
                if (session.Engine.State != VMState.FAULT)
                {
                    ProcessInvokeWithWallet(json, signers);
                }
            }
            catch
            {
                session.Dispose();
                throw;
            }
            if (session.Iterators.Count == 0 || !settings.SessionEnabled)
            {
                session.Dispose();
            }
            else
            {
                Guid id = Guid.NewGuid();
                json["session"] = id.ToString();
                lock (sessions)
                    sessions.Add(id, session);
            }
            return json;
        }

        private static JObject ToJson(TreeNode<UInt160> node)
        {
            JObject json = new();
            json["hash"] = node.Item.ToString();
            if (node.Children.Any())
            {
                json["call"] = new JArray(node.Children.Select(ToJson));
            }
            return json;
        }

        private static JArray ToJson(IEnumerable<DataCache.Trackable> changes)
        {
            JArray array = new();
            foreach (var entry in changes)
            {
                array.Add(new JObject
                {
                    ["state"] = entry.State.ToString(),
                    ["key"] = Convert.ToBase64String(entry.Key.ToArray()),
                    ["value"] = Convert.ToBase64String(entry.Item.Value.ToArray())
                });
            }
            return array;
        }

        private static JObject ToJson(StackItem item, Session session)
        {
            JObject json = item.ToJson();
            if (item is InteropInterface interopInterface && interopInterface.GetInterface<object>() is IIterator iterator)
            {
                Guid id = Guid.NewGuid();
                session.Iterators.Add(id, iterator);
                json["interface"] = nameof(IIterator);
                json["id"] = id.ToString();
            }
            return json;
        }

        private static Signer[] SignersFromJson(JArray _params, ProtocolSettings settings)
        {
            if (_params.Count > Transaction.MaxTransactionAttributes)
            {
                throw new RpcException(RpcError.InvalidParams.WithData("Max allowed witness exceeded."));
            }

            var ret = _params.Select(u => new Signer
            {
                Account = AddressToScriptHash(u["account"].AsString(), settings.AddressVersion),
                Scopes = (WitnessScope)Enum.Parse(typeof(WitnessScope), u["scopes"]?.AsString()),
                AllowedContracts = ((JArray)u["allowedcontracts"])?.Select(p => UInt160.Parse(p.AsString())).ToArray() ?? Array.Empty<UInt160>(),
                AllowedGroups = ((JArray)u["allowedgroups"])?.Select(p => ECPoint.Parse(p.AsString(), ECCurve.Secp256r1)).ToArray() ?? Array.Empty<ECPoint>(),
                Rules = ((JArray)u["rules"])?.Select(r => WitnessRule.FromJson((JObject)r)).ToArray() ?? Array.Empty<WitnessRule>(),
            }).ToArray();

            // Validate format

            _ = ret.ToByteArray().AsSerializableArray<Signer>();

            return ret;
        }

        private static Witness[] WitnessesFromJson(JArray _params)
        {
            if (_params.Count > Transaction.MaxTransactionAttributes)
            {
                throw new RpcException(RpcError.InvalidParams.WithData("Max allowed witness exceeded."));
            }

            return _params.Select(u => new
            {
                Invocation = u["invocation"]?.AsString(),
                Verification = u["verification"]?.AsString()
            }).Where(x => x.Invocation != null || x.Verification != null).Select(x => new Witness()
            {
                InvocationScript = Convert.FromBase64String(x.Invocation ?? string.Empty),
                VerificationScript = Convert.FromBase64String(x.Verification ?? string.Empty)
            }).ToArray();
        }

        [RpcMethod]
        protected internal virtual JToken InvokeFunction(JArray _params)
        {
            UInt160 script_hash = Result.Ok_Or(() => UInt160.Parse(_params[0].AsString()), RpcError.InvalidParams.WithData($"Invalid script hash {nameof(script_hash)}"));
            string operation = Result.Ok_Or(() => _params[1].AsString(), RpcError.InvalidParams);
            ContractParameter[] args = _params.Count >= 3 ? ((JArray)_params[2]).Select(p => ContractParameter.FromJson((JObject)p)).ToArray() : System.Array.Empty<ContractParameter>();
            Signer[] signers = _params.Count >= 4 ? SignersFromJson((JArray)_params[3], system.Settings) : null;
            Witness[] witnesses = _params.Count >= 4 ? WitnessesFromJson((JArray)_params[3]) : null;
            bool useDiagnostic = _params.Count >= 5 && _params[4].GetBoolean();

            byte[] script;
            using (ScriptBuilder sb = new())
            {
                script = sb.EmitDynamicCall(script_hash, operation, args).ToArray();
            }
            return GetInvokeResult(script, signers, witnesses, useDiagnostic);
        }

        [RpcMethod]
        protected internal virtual JToken InvokeScript(JArray _params)
        {
            byte[] script = Result.Ok_Or(() => Convert.FromBase64String(_params[0].AsString()), RpcError.InvalidParams);
            Signer[] signers = _params.Count >= 2 ? SignersFromJson((JArray)_params[1], system.Settings) : null;
            Witness[] witnesses = _params.Count >= 2 ? WitnessesFromJson((JArray)_params[1]) : null;
            bool useDiagnostic = _params.Count >= 3 && _params[2].GetBoolean();
            return GetInvokeResult(script, signers, witnesses, useDiagnostic);
        }

        [RpcMethod]
        protected internal virtual JToken TraverseIterator(JArray _params)
        {
            settings.SessionEnabled.True_Or(RpcError.SessionsDisabled);
            Guid sid = Result.Ok_Or(() => Guid.Parse(_params[0].GetString()), RpcError.InvalidParams.WithData($"Invalid session id {nameof(sid)}"));
            Guid iid = Result.Ok_Or(() => Guid.Parse(_params[1].GetString()), RpcError.InvalidParams.WithData($"Invliad iterator id {nameof(iid)}"));
            int count = _params[2].GetInt32();
            Result.True_Or(() => count <= settings.MaxIteratorResultItems, RpcError.InvalidParams.WithData($"Invalid iterator items count {nameof(count)}"));
            Session session;
            lock (sessions)
            {
                session = Result.Ok_Or(() => sessions[sid], RpcError.UnknownSession);
                session.ResetExpiration();
            }
            IIterator iterator = Result.Ok_Or(() => session.Iterators[iid], RpcError.UnknownIterator);
            JArray json = new();
            while (count-- > 0 && iterator.Next())
                json.Add(iterator.Value(null).ToJson());
            return json;
        }

        [RpcMethod]
        protected internal virtual JToken TerminateSession(JArray _params)
        {
            settings.SessionEnabled.True_Or(RpcError.SessionsDisabled);
            Guid sid = Result.Ok_Or(() => Guid.Parse(_params[0].GetString()), RpcError.InvalidParams.WithData("Invalid session id"));

            Session session = null;
            bool result;
            lock (sessions)
            {
                result = Result.Ok_Or(() => sessions.Remove(sid, out session), RpcError.UnknownSession);
            }
            if (result) session.Dispose();
            return result;
        }

        [RpcMethod]
        protected internal virtual JToken GetUnclaimedGas(JArray _params)
        {
            string address = Result.Ok_Or(() => _params[0].AsString(), RpcError.InvalidParams.WithData($"Invalid address {nameof(address)}"));
            JObject json = new();
            UInt160 script_hash = Result.Ok_Or(() => AddressToScriptHash(address, system.Settings.AddressVersion), RpcError.InvalidParams);

            var snapshot = system.StoreView;
            json["unclaimed"] = NativeContract.NEO.UnclaimedGas(snapshot, script_hash, NativeContract.Ledger.CurrentIndex(snapshot) + 1).ToString();
            json["address"] = script_hash.ToAddress(system.Settings.AddressVersion);
            return json;
        }

        static string GetExceptionMessage(Exception exception)
        {
            return exception?.GetBaseException().Message;
        }
    }
}
