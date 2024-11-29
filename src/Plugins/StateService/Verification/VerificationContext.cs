// Copyright (C) 2015-2024 The Neo Project.
//
// VerificationContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.StateService.Network;
using Neo.Plugins.StateService.Storage;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System.Collections.Concurrent;
using System.IO;

namespace Neo.Plugins.StateService.Verification
{
    class VerificationContext
    {
        private const uint MaxValidUntilBlockIncrement = 100;
        private StateRoot _root;
        private ExtensiblePayload _rootPayload;
        private ExtensiblePayload _votePayload;
        private readonly Wallet _wallet;
        private readonly KeyPair _keyPair;
        private readonly int _myIndex;
        private readonly uint _rootIndex;
        private readonly ECPoint[] _verifiers;
        private int M => _verifiers.Length - (_verifiers.Length - 1) / 3;
        private readonly ConcurrentDictionary<int, byte[]> _signatures = new();

        public int _retries;
        public bool IsValidator => _myIndex >= 0;
        public int MyIndex => _myIndex;
        public uint RootIndex => _rootIndex;
        public ECPoint[] Verifiers => _verifiers;
        public int Sender
        {
            get
            {
                int p = ((int)_rootIndex - _retries) % _verifiers.Length;
                return p >= 0 ? p : p + _verifiers.Length;
            }
        }
        public bool IsSender => _myIndex == Sender;
        public ICancelable _timer;
        public StateRoot StateRoot
        {
            get
            {
                if (_root is null)
                {
                    using var snapshot = StateStore.Singleton.GetSnapshot();
                    _root = snapshot.GetStateRoot(_rootIndex);
                }
                return _root;
            }
        }
        public ExtensiblePayload StateRootMessage => _rootPayload;
        public ExtensiblePayload VoteMessage
        {
            get
            {
                if (_votePayload is null)
                    _votePayload = CreateVoteMessage();
                return _votePayload;
            }
        }

        public VerificationContext(Wallet wallet, uint index)
        {
            _wallet = wallet;
            _retries = 0;
            _myIndex = -1;
            _rootIndex = index;
            _verifiers = NativeContract.RoleManagement.GetDesignatedByRole(StatePlugin._system.StoreView, Role.StateValidator, index);
            if (wallet is null) return;
            for (var i = 0; i < _verifiers.Length; i++)
            {
                var account = wallet.GetAccount(_verifiers[i]);
                if (account?.HasKey != true) continue;
                _myIndex = i;
                _keyPair = account.GetKey();
                break;
            }
        }

        private ExtensiblePayload CreateVoteMessage()
        {
            if (StateRoot is null) return null;
            if (!_signatures.TryGetValue(_myIndex, out var sig))
            {
                sig = StateRoot.Sign(_keyPair, StatePlugin._system.Settings.Network);
                _signatures[_myIndex] = sig;
            }
            return CreatePayload(MessageType.Vote, new Vote
            {
                RootIndex = _rootIndex,
                ValidatorIndex = _myIndex,
                Signature = sig
            }, VerificationService.MaxCachedVerificationProcessCount);
        }

        public bool AddSignature(int index, byte[] sig)
        {
            if (M <= _signatures.Count) return false;
            if (index < 0 || _verifiers.Length <= index) return false;
            if (_signatures.ContainsKey(index)) return false;
            Utility.Log(nameof(VerificationContext), LogLevel.Info, $"vote received, height={_rootIndex}, index={index}");
            var validator = _verifiers[index];
            var hashData = StateRoot?.GetSignData(StatePlugin._system.Settings.Network);
            if (hashData is null || !Crypto.VerifySignature(hashData, sig, validator))
            {
                Utility.Log(nameof(VerificationContext), LogLevel.Info, "incorrect vote, invalid signature");
                return false;
            }
            return _signatures.TryAdd(index, sig);
        }

        public bool CheckSignatures()
        {
            if (StateRoot is null) return false;
            if (_signatures.Count < M) return false;
            if (StateRoot.Witness is null)
            {
                var contract = Contract.CreateMultiSigContract(M, _verifiers);
                ContractParametersContext sc = new(StatePlugin._system.StoreView, StateRoot, StatePlugin._system.Settings.Network);
                for (int i = 0, j = 0; i < _verifiers.Length && j < M; i++)
                {
                    if (!_signatures.TryGetValue(i, out var sig)) continue;
                    sc.AddSignature(contract, _verifiers[i], sig);
                    j++;
                }
                if (!sc.Completed) return false;
                StateRoot.Witness = sc.GetWitnesses()[0];
            }
            if (IsSender)
                _rootPayload = CreatePayload(MessageType.StateRoot, StateRoot, MaxValidUntilBlockIncrement);
            return true;
        }

        private ExtensiblePayload CreatePayload(MessageType type, ISerializable payload, uint validBlockEndThreshold)
        {
            byte[] data;
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((byte)type);
                payload.Serialize(writer);
                writer.Flush();
                data = ms.ToArray();
            }
            var msg = new ExtensiblePayload
            {
                Category = StatePlugin.StatePayloadCategory,
                ValidBlockStart = StateRoot.Index,
                ValidBlockEnd = StateRoot.Index + validBlockEndThreshold,
                Sender = Contract.CreateSignatureRedeemScript(_verifiers[MyIndex]).ToScriptHash(),
                Data = data,
            };
            var sc = new ContractParametersContext(StatePlugin._system.StoreView, msg, StatePlugin._system.Settings.Network);
            _wallet.Sign(sc);
            msg.Witness = sc.GetWitnesses()[0];
            return msg;
        }
    }
}
