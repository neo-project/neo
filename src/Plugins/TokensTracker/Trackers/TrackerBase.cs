// Copyright (C) 2015-2024 The Neo Project.
//
// TrackerBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.Plugins.Trackers
{
    record TransferRecord(UInt160 asset, UInt160 from, UInt160 to, byte[] tokenId, BigInteger amount);

    abstract class TrackerBase
    {
        protected bool _shouldTrackHistory;
        protected uint _maxResults;
        protected IStore _db;
        private ISnapshot _levelDbSnapshot;
        protected NeoSystem _neoSystem;
        public abstract string TrackName { get; }

        protected TrackerBase(IStore db, uint maxResult, bool shouldTrackHistory, NeoSystem neoSystem)
        {
            _db = db;
            _maxResults = maxResult;
            _shouldTrackHistory = shouldTrackHistory;
            _neoSystem = neoSystem;
        }

        public abstract void OnPersist(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList);

        public void ResetBatch()
        {
            _levelDbSnapshot?.Dispose();
            _levelDbSnapshot = _db.GetSnapshot();
        }

        public void Commit()
        {
            _levelDbSnapshot?.Commit();
        }

        public IEnumerable<(TKey key, TValue val)> QueryTransfers<TKey, TValue>(byte dbPrefix, UInt160 userScriptHash, ulong startTime, ulong endTime)
                where TKey : ISerializable, new()
                where TValue : class, ISerializable, new()
        {
            var prefix = new[] { dbPrefix }.Concat(userScriptHash.ToArray()).ToArray();
            byte[] startTimeBytes, endTimeBytes;
            if (BitConverter.IsLittleEndian)
            {
                startTimeBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(startTime));
                endTimeBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(endTime));
            }
            else
            {
                startTimeBytes = BitConverter.GetBytes(startTime);
                endTimeBytes = BitConverter.GetBytes(endTime);
            }
            var transferPairs = _db.FindRange<TKey, TValue>([.. prefix, .. startTimeBytes], [.. prefix, .. endTimeBytes]);
            return transferPairs;
        }

        protected static byte[] Key(byte prefix, ISerializable key)
        {
            byte[] buffer = new byte[key.Size + 1];
            using (MemoryStream ms = new(buffer, true))
            using (BinaryWriter writer = new(ms))
            {
                writer.Write(prefix);
                key.Serialize(writer);
            }
            return buffer;
        }

        protected void Put(byte prefix, ISerializable key, ISerializable value)
        {
            _levelDbSnapshot.Put(Key(prefix, key), value.ToArray());
        }

        protected void Delete(byte prefix, ISerializable key)
        {
            _levelDbSnapshot.Delete(Key(prefix, key));
        }

        protected TransferRecord GetTransferRecord(UInt160 asset, Array stateItems)
        {
            if (stateItems.Count < 3)
            {
                return null;
            }
            var fromItem = stateItems[0];
            var toItem = stateItems[1];
            var amountItem = stateItems[2];
            if (fromItem.NotNull() && fromItem is not ByteString)
                return null;
            if (toItem.NotNull() && toItem is not ByteString)
                return null;
            if (amountItem is not ByteString && amountItem is not Integer)
                return null;

            byte[] fromBytes = fromItem.IsNull ? null : fromItem.GetSpan().ToArray();
            if (fromBytes != null && fromBytes.Length != UInt160.Length)
                return null;
            byte[] toBytes = toItem.IsNull ? null : toItem.GetSpan().ToArray();
            if (toBytes != null && toBytes.Length != UInt160.Length)
                return null;
            if (fromBytes == null && toBytes == null)
                return null;

            var from = fromBytes == null ? UInt160.Zero : new UInt160(fromBytes);
            var to = toBytes == null ? UInt160.Zero : new UInt160(toBytes);
            return stateItems.Count switch
            {
                3 => new TransferRecord(asset, @from, to, null, amountItem.GetInteger()),
                4 when (stateItems[3] is ByteString tokenId) => new TransferRecord(asset, @from, to, tokenId.Memory.ToArray(), amountItem.GetInteger()),
                _ => null
            };
        }

        protected JObject ToJson(TokenTransferKey key, TokenTransfer value)
        {
            JObject transfer = new();
            transfer["timestamp"] = key.TimestampMS;
            transfer["assethash"] = key.AssetScriptHash.ToString();
            transfer["transferaddress"] = value.UserScriptHash == UInt160.Zero ? null : value.UserScriptHash.ToAddress(_neoSystem.Settings.AddressVersion);
            transfer["amount"] = value.Amount.ToString();
            transfer["blockindex"] = value.BlockIndex;
            transfer["transfernotifyindex"] = key.BlockXferNotificationIndex;
            transfer["txhash"] = value.TxHash.ToString();
            return transfer;
        }

        public UInt160 GetScriptHashFromParam(string addressOrScriptHash)
        {
            return addressOrScriptHash.Length < 40 ?
                addressOrScriptHash.ToScriptHash(_neoSystem.Settings.AddressVersion) : UInt160.Parse(addressOrScriptHash);
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            Utility.Log(TrackName, level, message);
        }
    }
}
