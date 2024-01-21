// Copyright (C) 2015-2024 The Neo Project.
//
// Blockchain.Fallback.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.IO;
using System.Text;
namespace Neo.Ledger
{
#if FALLBACK
    public delegate void BlockFallbackHandler(NeoSystem system, uint height);

    partial class Blockchain
    {
        public static event BlockFallbackHandler BlockFallback;

        private static void SaveFallback(SnapshotCache snapshot, uint blockId)
        {
            var changeSet = snapshot.GetChangeSet();
            using var memoryStream = new MemoryStream();
            foreach (var item in changeSet)
            {
                byte operation = item.State switch
                {
                    TrackState.Deleted => 0,
                    TrackState.Added => 1,
                    TrackState.Changed => 2,
                    _ => throw new InvalidOperationException("Invalid fallback operation")
                };

                var encoded = ChangeEncode(
                    blockId,
                    operation,
                    item.Key.Key.ToArray(),
                    operation != 1 ? snapshot.TryGet(item.Key, true).Value.ToArray() : Array.Empty<byte>()
                );
                memoryStream.Write(BitConverter.GetBytes(encoded.Length), 0, 4);
                memoryStream.Write(encoded, 0, encoded.Length);
            }
            snapshot.Add(new StorageKey(Encoding.UTF8.GetBytes($"fallback{blockId}")), new StorageItem(memoryStream.ToArray()));
        }

        public static void OnBlockFallback(NeoSystem system, uint height)
        {
            BlockFallback?.Invoke(system, height);
            var snapshot = system.GetSnapshot();
            var currentIndex = NativeContract.Ledger.CurrentIndex(snapshot);
            if (currentIndex <= height) return;

            for (var i = currentIndex; i > height; i--)
            {
                using var snapshotFallback = system.GetSnapshot();
                var item = snapshotFallback.TryGet(new StorageKey(Encoding.UTF8.GetBytes($"fallback{i}"))).Value.ToArray();
                using (var memoryStream = new MemoryStream(item))
                using (var reader = new BinaryReader(memoryStream))
                {
                    while (memoryStream.Position < memoryStream.Length)
                    {
                        int length = reader.ReadInt32();
                        if (length < 0 || length > memoryStream.Length - memoryStream.Position)
                        {
                            throw new InvalidDataException("Invalid length value.");
                        }
                        byte[] encodedItem = reader.ReadBytes(length);
                        var decoded = ChangeDecode(encodedItem);
                        var key = new StorageKey(decoded.key);
                        var value = new StorageItem(decoded.value);
                        try
                        {
                            switch (decoded.operation)
                            {
                                case 0:
                                    snapshot.Add(key, value);
                                    break;
                                case 1:
                                    snapshot.Delete(key);
                                    break;
                                case 2:
                                    snapshot.GetAndChange(key).FromReplica(value);
                                    break;
                                default:
                                    throw new InvalidOperationException("Invalid fallback operation");
                            }
                        }
                        catch (Exception e)
                        {
                            // Ignore exception while fallback
                        }
                    }
                    // snapshotFallback.Delete(new StorageKey(Encoding.UTF8.GetBytes($"fallback{i}")));
                    snapshotFallback.Commit();
                    Console.WriteLine("Fallback to block {0}", i);
                }
            }
        }

        private static byte[] ChangeEncode(uint blockId, byte operation, byte[] key, byte[] value)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Block ID (4 bytes)
                memoryStream.Write(BitConverter.GetBytes(blockId), 0, 4);
                // (1 byte)
                memoryStream.WriteByte(operation);
                // Key (2 bytes)
                memoryStream.Write(BitConverter.GetBytes((ushort)key.Length), 0, 2);
                //Key
                memoryStream.Write(key, 0, key.Length);
                //Value
                memoryStream.Write(value, 0, value.Length);
                return memoryStream.ToArray();
            }
        }

        public static (uint blockId, byte operation, byte[] key, byte[] value) ChangeDecode(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            using (var reader = new BinaryReader(memoryStream))
            {
                // Block ID
                uint blockId = reader.ReadUInt32();
                byte operation = reader.ReadByte();
                ushort keyLength = reader.ReadUInt16();
                byte[] key = reader.ReadBytes(keyLength);
                byte[] value = reader.ReadBytes((int)(memoryStream.Length - memoryStream.Position));
                return (blockId, operation, key, value);
            }
        }
    }
#endif
}
