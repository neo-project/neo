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
    partial class RpcServer
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

            _ = IO.Helper.ToByteArray(ret).AsSerializableArray<Signer>();

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

        /// <summary>
        /// Invokes a smart contract with its scripthash based on the specified operation and parameters and returns the result.
        /// </summary>
        /// <remarks>
        /// This method is used to test your VM script as if they ran on the blockchain at that point in time.
        /// This RPC call does not affect the blockchain in any way.
        /// </remarks>
        /// <param name="scriptHash">Smart contract scripthash. Use big endian for Hash160, little endian for ByteArray.</param>
        /// <param name="operation">The operation name (string)</param>
        /// <param name="args">Optional. The parameters to be passed into the smart contract operation</param>
        /// <param name="signers">Optional. List of contract signature accounts.</param>
        /// <param name="useDiagnostic">Optional. Flag to enable diagnostic information.</param>
        /// <returns>A JToken containing the result of the invocation.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken InvokeFunction(string scriptHash, string operation, ContractParameter[] args = null, Signer[] signers = null, bool useDiagnostic = false)
        {
            UInt160 contractHash = Result.Ok_Or(() => UInt160.Parse(scriptHash), RpcError.InvalidParams);
            byte[] script;
            using (ScriptBuilder sb = new())
            {
                script = sb.EmitDynamicCall(contractHash, operation, args ?? Array.Empty<ContractParameter>()).ToArray();
            }
            return GetInvokeResult(script, signers, [], useDiagnostic);
        }

        /// <summary>
        /// Returns the result after passing a script through the VM.
        /// </summary>
        /// <remarks>
        /// This method is to test your VM script as if they ran on the blockchain at that point in time.
        /// This RPC call does not affect the blockchain in any way.
        /// You must install the plugin RpcServer before you can invoke the method.
        /// </remarks>
        /// <param name="scriptBase64">A script runnable in the VM, encoded as Base64. e.g. "AQIDBAUGBwgJCgsMDQ4PEA=="</param>
        /// <param name="signers">Optional. The list of contract signature accounts.</param>
        /// <param name="witnesses">Optional. The list of witnesses for the transaction.</param>
        /// <param name="useDiagnostic">Optional. Flag to enable diagnostic information.</param>
        /// <returns>A JToken containing the result of the invocation.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken InvokeScript(string scriptBase64, Signer[] signers = null, Witness[] witnesses = null, bool useDiagnostic = false)
        {
            byte[] script = Result.Ok_Or(() => Convert.FromBase64String(scriptBase64), RpcError.InvalidParams);
            return GetInvokeResult(script, signers, witnesses, useDiagnostic);
        }

        /// <summary>
        /// Gets the Iterator value from session and Iterator id returned by invokefunction or invokescript.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method queries Iterator type data and does not affect the blockchain data.
        /// You must install the plugin RpcServer before you can invoke the method.
        /// Before you can use the method, make sure that the SessionEnabled value in config.json of the plugin RpcServer is true,
        /// and you have obtained Iterator id and session by invoking invokefunction or invokescript.
        /// </para>
        /// <para>
        /// The validity of the session and iterator id is set by SessionExpirationTime in the config.json file of the RpcServer plug-in, in seconds.
        /// </para>
        /// </remarks>
        /// <param name="session">Cache id. It is session returned by invokefunction or invokescript. e.g. "c5b628b6-10d9-4cc5-b850-3cfc0b659fcf"</param>
        /// <param name="iteratorId">Iterator data id. It is the id of stack returned by invokefunction or invokescript. e.g. "593b02c6-138d-4945-846d-1e5974091daa"</param>
        /// <param name="count">The number of values returned. It cannot exceed the value of the MaxIteratorResultItems field in config.json of the RpcServer plug-in.</param>
        /// <returns>A JToken containing the iterator values.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken TraverseIterator(string session, string iteratorId, int count)
        {
            settings.SessionEnabled.True_Or(RpcError.SessionsDisabled);
            Guid sid = Result.Ok_Or(() => Guid.Parse(session), RpcError.InvalidParams.WithData($"Invalid session id"));
            Guid iid = Result.Ok_Or(() => Guid.Parse(iteratorId), RpcError.InvalidParams.WithData($"Invalid iterator id"));
            Result.True_Or(() => count <= settings.MaxIteratorResultItems, RpcError.InvalidParams.WithData($"Invalid iterator items count: {count}"));

            Session currentSession;
            lock (sessions)
            {
                currentSession = Result.Ok_Or(() => sessions[sid], RpcError.UnknownSession);
                currentSession.ResetExpiration();
            }
            IIterator iterator = Result.Ok_Or(() => currentSession.Iterators[iid], RpcError.UnknownIterator);
            JArray json = new();
            while (count-- > 0 && iterator.Next())
                json.Add(iterator.Value(null).ToJson());
            return json;
        }

        /// <summary>
        /// Terminates a session with the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID of the session to terminate. e.g. "00000000-0000-0000-0000-000000000000"</param>
        /// <returns>A JToken indicating whether the session was successfully terminated.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken TerminateSession(Guid guid)
        {
            settings.SessionEnabled.True_Or(RpcError.SessionsDisabled);

            Session session = null;
            bool result;
            lock (sessions)
            {
                result = Result.Ok_Or(() => sessions.Remove(guid, out session), RpcError.UnknownSession);
            }
            if (result) session.Dispose();
            return result;
        }

        /// <summary>
        /// Gets the unclaimed GAS for the specified address.
        /// </summary>
        /// <param name="account">The account to check for unclaimed GAS. e.g. "NQ5D43HX4QBXZ3XZ4QBXZ3XZ4QBXZ3XZ"</param>
        /// <returns>A JToken containing the unclaimed GAS amount and the address.</returns>
        [RpcMethodWithParams]
        protected internal virtual JToken GetUnclaimedGas(string account)
        {
            JObject json = new();
            var scriptHash = Result.Ok_Or(() => AddressToScriptHash(account, system.Settings.AddressVersion), RpcError.InvalidParams) ?? throw new ArgumentNullException("Result.Ok_Or(() => AddressToScriptHash(account, system.Settings.AddressVersion), RpcError.InvalidParams)");

            var snapshot = system.StoreView;
            json["unclaimed"] = NativeContract.NEO.UnclaimedGas(snapshot, scriptHash, NativeContract.Ledger.CurrentIndex(snapshot) + 1).ToString();
            json["address"] = scriptHash.ToAddress(system.Settings.AddressVersion);
            return json;
        }

        static string GetExceptionMessage(Exception exception)
        {
            return exception?.GetBaseException().Message;
        }
    }
}
