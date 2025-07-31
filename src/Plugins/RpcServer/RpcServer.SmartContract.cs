// Copyright (C) 2015-2025 The Neo Project.
//
// RpcServer.SmartContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
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
                    return new JObject()
                    {
                        ["eventname"] = n.EventName,
                        ["contract"] = n.ScriptHash.ToString(),
                        ["state"] = ToJson(n.State, session),
                    };
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

        protected static JObject ToJson(TreeNode<UInt160> node)
        {
            var json = new JObject() { ["hash"] = node.Item.ToString() };
            if (node.Children.Any())
            {
                json["call"] = new JArray(node.Children.Select(ToJson));
            }
            return json;
        }

        protected static JArray ToJson(IEnumerable<KeyValuePair<StorageKey, DataCache.Trackable>> changes)
        {
            var array = new JArray();
            foreach (var entry in changes)
            {
                array.Add(new JObject
                {
                    ["state"] = entry.Value.State.ToString(),
                    ["key"] = Convert.ToBase64String(entry.Key.ToArray()),
                    ["value"] = Convert.ToBase64String(entry.Value.Item.Value.ToArray())
                });
            }
            return array;
        }

        private static JObject ToJson(StackItem item, Session session)
        {
            var json = item.ToJson();
            if (item is InteropInterface interopInterface && interopInterface.GetInterface<object>() is IIterator iterator)
            {
                Guid id = Guid.NewGuid();
                session.Iterators.Add(id, iterator);
                json["interface"] = nameof(IIterator);
                json["id"] = id.ToString();
            }
            return json;
        }

        /// <summary>
        /// Invokes a function on a contract.
        /// <para>Request format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "invokefunction",
        ///   "params": [
        ///     "An UInt160 ScriptHash",  // the contract address
        ///     "operation",  // the operation to invoke
        ///     [{"type": "ContractParameterType", "value": "The parameter value"}],  // ContractParameter, the arguments
        ///     [{
        ///       // The part of the Signer
        ///       "account": "An UInt160 or Base58Check address", // The account of the signer, required
        ///       "scopes": "WitnessScope", // WitnessScope, required
        ///       "allowedcontracts": ["The contract hash(UInt160)"], // optional
        ///       "allowedgroups": ["PublicKey"], // ECPoint, i.e. ECC PublicKey, optional
        ///       "rules": [{"action": "WitnessRuleAction", "condition": {/*A json of WitnessCondition*/}}] // WitnessRule
        ///       // The part of the Witness, optional
        ///       "invocation": "A Base64-encoded string",
        ///       "verification": "A Base64-encoded string"
        ///     }], // A JSON array of signers and witnesses, optional
        ///     false // useDiagnostic, a bool value indicating whether to use diagnostic information, optional
        ///   ]
        /// }</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "script": "A Base64-encoded string",
        ///     "state": "A string of VMState",
        ///     "gasconsumed": "An integer number in string",
        ///     "exception": "The exception message",
        ///     "stack": [{"type": "The stack item type", "value": "The stack item value"}],
        ///     "notifications": [
        ///       {"eventname": "The event name", "contract": "The contract hash", "state": {"interface": "A string", "id": "The GUID string"}}
        ///     ], // The notifications, optional
        ///     "diagnostics": {
        ///       "invokedcontracts": {"hash": "The contract hash","call": [{"hash": "The contract hash"}]}, // The invoked contracts
        ///       "storagechanges": [
        ///         {
        ///           "state": "The TrackState string",
        ///           "key": "The Base64-encoded key",
        ///           "value": "The Base64-encoded value"
        ///         }
        ///         // ...
        ///       ] // The storage changes
        ///     }, // The diagnostics, optional, if useDiagnostic is true
        ///     "session": "A GUID string" // The session id, optional
        ///   }
        /// }</code>
        /// </summary>
        /// <param name="_params">An array containing the following elements:
        /// [0]: The script hash of the contract to invoke as a string.
        /// [1]: The operation to invoke as a string.
        /// [2]: The arguments to pass to the function as an array of ContractParameter. Optional.
        /// [3]: The JSON array of signers and witnesses<see cref="ParameterConverter.ToSignersAndWitnesses"/>. Optional.
        /// [4]: A boolean value indicating whether to use diagnostic information. Optional.
        /// </param>
        /// <returns>The result of the function invocation.</returns>
        /// <exception cref="RpcException">
        /// Thrown when the script hash is invalid, the contract is not found, or the verification fails.
        /// </exception>
        [RpcMethod]
        protected internal virtual JToken InvokeFunction(JArray _params)
        {
            var scriptHash = Result.Ok_Or(() => UInt160.Parse(_params[0].AsString()),
                RpcError.InvalidParams.WithData($"Invalid script hash `{_params[0]}`"));

            var operation = Result.Ok_Or(() => _params[1].AsString(), RpcError.InvalidParams);
            var args = _params.Count >= 3
                ? ((JArray)_params[2]).Select(p => ContractParameter.FromJson((JObject)p)).ToArray()
                : [];

            var (signers, witnesses) = _params.Count >= 4
                ? ((JArray)_params[3]).ToSignersAndWitnesses(system.Settings.AddressVersion)
                : (null, null);

            var useDiagnostic = _params.Count >= 5 && _params[4].GetBoolean();

            byte[] script;
            using (ScriptBuilder sb = new())
            {
                script = sb.EmitDynamicCall(scriptHash, operation, args).ToArray();
            }
            return GetInvokeResult(script, signers, witnesses, useDiagnostic);
        }

        /// <summary>
        /// Invokes a script.
        /// <para>Request format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "invokescript",
        ///   "params": [
        ///     "A Base64-encoded script", // the script to invoke
        ///     [{
        ///       // The part of the Signer
        ///       "account": "An UInt160 or Base58Check address", // The account of the signer, required
        ///       "scopes": "WitnessScope", // WitnessScope, required
        ///       "allowedcontracts": ["The contract hash(UInt160)"], // optional
        ///       "allowedgroups": ["PublicKey"], // ECPoint, i.e. ECC PublicKey, optional
        ///       "rules": [{"action": "WitnessRuleAction", "condition": {/* A json of WitnessCondition */ }}], // WitnessRule
        ///       // The part of the Witness, optional
        ///       "invocation": "A Base64-encoded string",
        ///       "verification": "A Base64-encoded string"
        ///     }], // A JSON array of signers and witnesses, optional
        ///     false // useDiagnostic, a bool value indicating whether to use diagnostic information, optional
        ///   ]
        /// }</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "script": "A Base64-encoded script",
        ///     "state": "A string of VMState", // see VMState
        ///     "gasconsumed": "An integer number in string", // The gas consumed
        ///     "exception": "The exception message", // The exception message
        ///     "stack": [
        ///       {"type": "The stack item type", "value": "The stack item value"} // A stack item in the stack
        ///       // ...
        ///     ],
        ///     "notifications": [
        ///       {"eventname": "The event name", // The name of the event
        ///        "contract": "The contract hash", // The hash of the contract
        ///        "state": {"interface": "A string", "id": "The GUID string"} // The state of the event
        ///       }
        ///     ], // The notifications, optional
        ///     "diagnostics": {
        ///       "invokedcontracts": {"hash": "The contract hash","call": [{"hash": "The contract hash"}]}, // The invoked contracts
        ///       "storagechanges": [
        ///         {
        ///           "state": "The TrackState string",
        ///           "key": "The Base64-encoded key",
        ///           "value": "The Base64-encoded value"
        ///         }
        ///         // ...
        ///       ] // The storage changes
        ///     }, // The diagnostics, optional, if useDiagnostic is true
        ///     "session": "A GUID string" // The session id, optional
        ///   }
        /// }</code>
        /// </summary>
        /// <param name="_params">An array containing the following elements:
        /// [0]: The script as a Base64-encoded string.
        /// [1]: The JSON array of signers and witnesses<see cref="ParameterConverter.ToSignersAndWitnesses"/>. Optional.
        /// [3]: A boolean value indicating whether to use diagnostic information. Optional.
        /// </param>
        /// <returns>The result of the script invocation.</returns>
        /// <exception cref="RpcException">
        /// Thrown when the script is invalid, the verification fails, or the script hash is invalid.
        /// </exception>
        [RpcMethod]
        protected internal virtual JToken InvokeScript(JArray _params)
        {
            var script = Result.Ok_Or(() => Convert.FromBase64String(_params[0].AsString()), RpcError.InvalidParams);
            var (signers, witnesses) = _params.Count >= 2
                ? ((JArray)_params[1]).ToSignersAndWitnesses(system.Settings.AddressVersion)
                : (null, null);
            var useDiagnostic = _params.Count >= 3 && _params[2].GetBoolean();
            return GetInvokeResult(script, signers, witnesses, useDiagnostic);
        }

        /// <summary>
        /// <para>Request format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "traverseiterator",
        ///   "params": [
        ///     "A GUID string(The session id)",
        ///     "A GUID string(The iterator id)",
        ///     100, // An integer number(The number of items to traverse)
        ///   ]
        /// }</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": [{"type": "The stack item type", "value": "The stack item value"}]
        /// }</code>
        /// </summary>
        /// <param name="_params"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Terminates a session.
        /// <para>Request format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "terminatesession",
        ///   "params": ["A GUID string(The session id)"]
        /// }</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": true // true if the session is terminated successfully, otherwise false
        /// }</code>
        /// </summary>
        /// <param name="_params">A 1-element array containing the session id as a GUID string.</param>
        /// <returns>True if the session is terminated successfully, otherwise false.</returns>
        /// <exception cref="RpcException">Thrown when the session id is invalid.</exception>
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

        /// <summary>
        /// Gets the unclaimed gas of an address.
        /// <para>Request format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "method": "getunclaimedgas",
        ///   "params": ["An UInt160 or Base58Check address"]
        /// }</code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {"unclaimed": "An integer in string", "address": "The Base58Check address"}
        /// }</code>
        /// </summary>
        /// <param name="_params">An array containing the following elements:
        /// [0]: The address as a UInt160 or Base58Check address.
        /// </param>
        /// <returns>A JSON object containing the unclaimed gas and the address.</returns>
        /// <exception cref="RpcException">
        /// Thrown when the address is invalid.
        /// </exception>
        [RpcMethod]
        protected internal virtual JToken GetUnclaimedGas(JArray _params)
        {
            var address = Result.Ok_Or(() => _params[0].AsString(),
                RpcError.InvalidParams.WithData($"Invalid address `{_params[0]}`"));
            var json = new JObject();
            var scriptHash = Result.Ok_Or(
                () => address.AddressToScriptHash(system.Settings.AddressVersion),
                RpcError.InvalidParams.WithData($"Invalid address `{address}`"));

            var snapshot = system.StoreView;
            json["unclaimed"] = NativeContract.NEO.UnclaimedGas(snapshot, scriptHash, NativeContract.Ledger.CurrentIndex(snapshot) + 1).ToString();
            json["address"] = scriptHash.ToAddress(system.Settings.AddressVersion);
            return json;
        }

        static string GetExceptionMessage(Exception exception)
        {
            if (exception == null) return null;

            // First unwrap any TargetInvocationException
            var unwrappedException = UnwrapException(exception);

            // Then get the base exception message
            return unwrappedException.GetBaseException().Message;
        }
    }
}
