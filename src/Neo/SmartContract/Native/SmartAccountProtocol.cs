// Copyright (C) 2015-2026 The Neo Project.
//
// SmartAccountProtocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// State-independent identity, signing, and nonce encoding for native SmartAccount v1.
    /// This codec does not register or activate a native contract.
    /// </summary>
    internal static class SmartAccountProtocol
    {
        internal const byte Version = 2;
        internal const byte IdentityVersion = 1;
        internal const byte AuthorizationVersion = 2;
        internal const int SaltSize = 32;
        internal const int MethodBytesMax = 128;
        internal const int ArgumentCountMax = 64;
        internal const int ArgumentSizeMax = 4096;
        internal const int ArgumentDepthMax = 8;
        internal const int SignatureBytesMax = 1024;
        private const int ChannelSize = 24;
        private const byte AccountPrefix = 0x10;
        private const byte NoncePrefix = 0x20;
        private const int UnsignedOperationSizeMax = 2 + 2 + UInt160.Length + 2 + MethodBytesMax
            + ArgumentSizeMax + 2 * (2 + Integer.MaxSize) + 2;

        private static readonly byte[] ServiceHashBytes = Helper.GetContractHash(UInt160.Zero, 0, "AccountManagement").ToArray();
        internal static UInt160 ServiceHash => new(ServiceHashBytes);
        private static readonly BigInteger IntegerLimit = BigInteger.One << 255;
        private static readonly BigInteger ChannelLimit = BigInteger.One << 191;
        internal static readonly BigInteger ExhaustedSequence = BigInteger.One << 64;

        internal static UInt160 GetAccountId(uint network, UInt160 custody, ReadOnlySpan<byte> salt)
        {
            RequireIdentity(custody);
            if (salt.Length != SaltSize)
                throw new FormatException("The account salt must contain exactly 32 bytes.");
            byte[] domain = CreateDomain("NeoSmartAccount"u8, IdentityVersion, network, custody);
            byte[] material = new byte[domain.Length + salt.Length];
            domain.CopyTo(material, 0);
            salt.CopyTo(material.AsSpan(domain.Length));
            UInt160 result = material.ToScriptHash();
            RequireIdentity(result);
            return result;
        }

        internal static byte[] CreateVerificationScript(UInt160 accountId)
        {
            RequireIdentity(accountId);
            using ScriptBuilder builder = new();
            builder.EmitDynamicCall(ServiceHash, "verify", CallFlags.ReadOnly, accountId);
            return builder.ToArray();
        }

        internal static UInt160 GetAccountAddress(UInt160 accountId) => CreateVerificationScript(accountId).ToScriptHash();

        internal static byte[] GetAuthorizationDomain(uint network, UInt160 accountId, ulong authorityEpoch, ulong configurationNonce)
        {
            RequireIdentity(accountId);
            byte[] domain = CreateDomain("NeoSmartAccount/UserOperation"u8, AuthorizationVersion, network, accountId);
            byte[] result = new byte[domain.Length + 2 * sizeof(ulong)];
            domain.CopyTo(result, 0);
            BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(domain.Length), authorityEpoch);
            BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(domain.Length + sizeof(ulong)), configurationNonce);
            return result;
        }

        private static byte[] CreateDomain(ReadOnlySpan<byte> prefix, byte version, uint network, UInt160 identity)
        {
            byte[] result = new byte[prefix.Length + 1 + sizeof(uint) + 2 * UInt160.Length];
            prefix.CopyTo(result);
            int offset = prefix.Length;
            result[offset++] = version;
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), network);
            offset += sizeof(uint);
            ServiceHash.ToArray().CopyTo(result, offset);
            identity.ToArray().CopyTo(result, offset + UInt160.Length);
            return result;
        }

        internal static byte[] GetOperationDigest(uint network, UInt160 accountId, StackItem operation, ulong authorityEpoch, ulong configurationNonce)
        {
            byte[] domain = GetAuthorizationDomain(network, accountId, authorityEpoch, configurationNonce);
            byte[] serialized = SerializeUnsignedOperation(operation);
            byte[] message = new byte[domain.Length + serialized.Length];
            domain.CopyTo(message, 0);
            serialized.CopyTo(message, domain.Length);
            return SHA256.HashData(message);
        }

        internal static byte[] SerializeUnsignedOperation(StackItem operation)
        {
            if (operation.Type != StackItemType.Array || operation is not Array op || op.Count != 6)
                throw new FormatException("A UserOperation must be an Array with exactly six fields.");
            if (op[0] is not ByteString target || target.Size != UInt160.Length || new UInt160(target.GetSpan()) == UInt160.Zero)
                throw new FormatException("The operation target must be a nonzero 20-byte ByteString.");
            if (op[1] is not ByteString method || method.Size is 0 or > MethodBytesMax)
                throw new FormatException("The operation method must contain 1 through 128 UTF-8 bytes.");
            try
            {
                _ = Utility.StrictUTF8.GetCharCount(method.GetSpan());
            }
            catch (DecoderFallbackException exception)
            {
                throw new FormatException("The operation method must use strict UTF-8.", exception);
            }
            if (op[2].Type != StackItemType.Array || op[2] is not Array args || args.Count > ArgumentCountMax)
                throw new FormatException("Operation arguments must be an Array containing at most 64 values.");
            if (op[3] is not Integer nonce || op[4] is not Integer deadline)
                throw new FormatException("The nonce and deadline must have the Integer type.");
            RequireUnsignedInteger(nonce.GetInteger());
            RequireUnsignedInteger(deadline.GetInteger());
            if (op[5] is not ByteString signature || signature.Size > SignatureBytesMax)
                throw new FormatException("The signature must be a ByteString containing at most 1024 bytes.");

            int size = 0;
            HashSet<Array> seen = new(ReferenceEqualityComparer.Instance);
            ValidateArgument(args, 0, seen, ref size);
            Array unsigned = new([op[0], op[1], args, nonce, deadline, ByteString.Empty]);
            // Every serialized value occupies at least one byte. The size bound therefore
            // also bounds the worklist and item count, independently of VM host defaults.
            return BinarySerializer.Serialize(unsigned, UnsignedOperationSizeMax, UnsignedOperationSizeMax);
        }

        private static void ValidateArgument(StackItem item, int depth, HashSet<Array> seen, ref int size)
        {
            switch (item)
            {
                case Null:
                    Charge(1, ref size);
                    break;
                case Boolean:
                    Charge(2, ref size);
                    break;
                case Integer:
                case ByteString:
                    int length = item.GetSpan().Length;
                    Charge(1L + VarIntSize(length) + length, ref size);
                    break;
                case Array array:
                    if (depth > ArgumentDepthMax || !seen.Add(array))
                        throw new FormatException("Argument containers must have bounded depth and unique references.");
                    Charge(1 + VarIntSize(array.Count), ref size);
                    // Even Null occupies a byte. Reject excessive fan-out before traversal.
                    if (array.Count > ArgumentSizeMax - size)
                        throw new FormatException("Serialized arguments exceed 4096 bytes.");
                    foreach (StackItem child in array)
                        ValidateArgument(child, depth + 1, seen, ref size);
                    break;
                default:
                    throw new FormatException("The argument type is not supported by SmartAccount v1.");
            }
        }

        private static int VarIntSize(int value) => value < 0xfd ? 1 : value <= ushort.MaxValue ? 3 : 5;

        private static void Charge(long bytes, ref int size)
        {
            if (bytes > ArgumentSizeMax - size)
                throw new FormatException("Serialized arguments exceed 4096 bytes.");
            size += (int)bytes;
        }

        internal static (BigInteger Channel, ulong Sequence) GetNonceParts(BigInteger nonce)
        {
            RequireUnsignedInteger(nonce);
            return (nonce >> 64, (ulong)(nonce & ulong.MaxValue));
        }

        internal static BigInteger ConsumeNonce(BigInteger nonce, BigInteger nextSequence)
        {
            var (_, sequence) = GetNonceParts(nonce);
            if (nextSequence < 0 || nextSequence > ExhaustedSequence)
                throw new FormatException("The stored nonce cursor is outside the sequence or exhaustion domain.");
            if (nextSequence == ExhaustedSequence || sequence != nextSequence)
                throw new InvalidOperationException("The nonce channel is exhausted or the sequence is not current.");
            return nextSequence + 1;
        }

        internal static byte[] GetAccountKey(UInt160 accountId)
        {
            RequireIdentity(accountId);
            byte[] result = new byte[1 + UInt160.Length];
            result[0] = AccountPrefix;
            accountId.ToArray().CopyTo(result, 1);
            return result;
        }

        internal static byte[] GetNonceKey(UInt160 accountId, BigInteger channel)
        {
            RequireIdentity(accountId);
            if (channel < 0 || channel >= ChannelLimit)
                throw new FormatException("The nonce channel must be a non-negative 191-bit value.");
            byte[] result = new byte[1 + UInt160.Length + ChannelSize];
            result[0] = NoncePrefix;
            accountId.ToArray().CopyTo(result, 1);
            byte[] encoded = channel.ToByteArray(isUnsigned: true, isBigEndian: true);
            encoded.CopyTo(result, result.Length - encoded.Length);
            return result;
        }

        private static void RequireUnsignedInteger(BigInteger value)
        {
            if (value < 0 || value >= IntegerLimit)
                throw new FormatException("The value must be a non-negative signed 256-bit Integer.");
        }

        private static void RequireIdentity(UInt160 identity)
        {
            if (identity == UInt160.Zero)
                throw new FormatException("A zero account or custody identity is not permitted.");
        }
    }
}
