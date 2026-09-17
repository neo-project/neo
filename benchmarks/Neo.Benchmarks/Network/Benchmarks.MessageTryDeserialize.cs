// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks.MessageTryDeserialize.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Neo.Extensions;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System.Buffers.Binary;

namespace Neo.Benchmarks.Network
{
    sealed class DisableOptimizeCheckConfig : ManualConfig
    {
        public DisableOptimizeCheckConfig()
        {
            WithOptions(ConfigOptions.DisableOptimizationsValidator);
        }
    }

    /// <summary>
    /// Before: header parse copies a 3-byte slice plus extra arrays for 16/32-bit lengths.
    /// After: index the ByteString and decode little-endian lengths in place.
    /// Payload ToArray is unchanged in both.
    /// </summary>
    [Config(typeof(DisableOptimizeCheckConfig))]
    [MemoryDiagnoser]
    public class Benchmarks_MessageTryDeserialize
    {
        public enum FrameKind
        {
            Compact,
            Ping,
            UInt16Length,
            UInt32Length
        }

        [Params(FrameKind.Compact, FrameKind.Ping, FrameKind.UInt16Length, FrameKind.UInt32Length)]
        public FrameKind Kind { get; set; } = FrameKind.Compact;

        private ByteString _frame = ByteString.Empty;

        [GlobalSetup]
        public void Setup()
        {
            var compact = Message.Create(MessageCommand.GetAddr).ToArray();
            var ping = Message.Create(MessageCommand.Ping, PingPayload.Create(uint.MaxValue)).ToArray();
            var u16 = compact.Take(2).Concat([(byte)0xFD, compact[2], (byte)0x00]).Concat(compact.Skip(3)).ToArray();
            var u32 = compact.Take(2).Concat([(byte)0xFE, compact[2], (byte)0x00, (byte)0x00, (byte)0x00]).Concat(compact.Skip(3)).ToArray();

            _frame = Kind switch
            {
                FrameKind.Compact => ByteString.CopyFrom(compact),
                FrameKind.Ping => ByteString.CopyFrom(ping),
                FrameKind.UInt16Length => ByteString.CopyFrom(u16),
                FrameKind.UInt32Length => ByteString.CopyFrom(u32),
                _ => throw new ArgumentOutOfRangeException(nameof(Kind))
            };

            if (TryDeserializeBefore(_frame) != TryDeserializeAfter(_frame))
                throw new InvalidOperationException($"Before/After consumed length mismatch for {Kind}.");
        }

        [Benchmark(Baseline = true)]
        public int Before()
        {
            return TryDeserializeBefore(_frame);
        }

        [Benchmark]
        public int After()
        {
            return TryDeserializeAfter(_frame);
        }

        /// <summary>
        /// Header parse from master-n3 before this PR: Slice.ToArray for the 3-byte header
        /// and for 16/32/64-bit payload lengths.
        /// </summary>
        private static int TryDeserializeBefore(ByteString data)
        {
            if (data.Count < 3) return 0;

            var header = data.Slice(0, 3).ToArray();
            var flags = (MessageFlags)header[0];
            var command = (MessageCommand)header[1];
            ulong length = header[2];
            var payloadIndex = 3;

            if (length == 0xFD)
            {
                if (data.Count < 5) return 0;
                length = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(payloadIndex, 2).ToArray());
                payloadIndex += 2;
            }
            else if (length == 0xFE)
            {
                if (data.Count < 7) return 0;
                length = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(payloadIndex, 4).ToArray());
                payloadIndex += 4;
            }
            else if (length == 0xFF)
            {
                if (data.Count < 11) return 0;
                length = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(payloadIndex, 8).ToArray());
                payloadIndex += 8;
            }

            if (length > Message.PayloadMaxSize) throw new FormatException();
            if (data.Count < (int)length + payloadIndex) return 0;

            ReadOnlyMemory<byte> payload = length <= 0
                ? ReadOnlyMemory<byte>.Empty
                : data.Slice(payloadIndex, (int)length).ToArray();
            return payloadIndex + (int)length + payload.Length + (byte)flags + (byte)command;
        }

        /// <summary>
        /// Header parse from this PR: index the ByteString and decode lengths in place.
        /// </summary>
        private static int TryDeserializeAfter(ByteString data)
        {
            if (data.Count < 3) return 0;

            var flags = (MessageFlags)data[0];
            var command = (MessageCommand)data[1];
            ulong length = data[2];
            var payloadIndex = 3;

            if (length == 0xFD)
            {
                if (data.Count < 5) return 0;
                length = ReadLittleEndian(data, payloadIndex, 2);
                payloadIndex += 2;
            }
            else if (length == 0xFE)
            {
                if (data.Count < 7) return 0;
                length = ReadLittleEndian(data, payloadIndex, 4);
                payloadIndex += 4;
            }
            else if (length == 0xFF)
            {
                if (data.Count < 11) return 0;
                length = ReadLittleEndian(data, payloadIndex, 8);
                payloadIndex += 8;
            }

            if (length > Message.PayloadMaxSize) throw new FormatException();
            if (data.Count < (int)length + payloadIndex) return 0;

            ReadOnlyMemory<byte> payload = length <= 0
                ? ReadOnlyMemory<byte>.Empty
                : data.Slice(payloadIndex, (int)length).ToArray();
            return payloadIndex + (int)length + payload.Length + (byte)flags + (byte)command;
        }

        private static ulong ReadLittleEndian(ByteString data, int index, int size)
        {
            ulong value = 0;
            for (int i = 0; i < size; i++)
                value |= (ulong)data[index + i] << (8 * i);
            return value;
        }
    }
}

/*
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
Intel Core Ultra 7 255U 2.00GHz, 1 CPU, 14 logical and 12 physical cores
.NET SDK 10.0.400
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

| Method | Kind         | Mean     | Ratio | Allocated | Alloc Ratio | Winner |
|------- |------------- |---------:|------:|----------:|------------:|--------|
| Before | Compact      | 13.03 ns |  1.00 |      32 B |        1.00 |        |
| After  | Compact      |  4.60 ns |  0.35 |         - |        0.00 | After  |
| Before | Ping         | 43.35 ns |  1.00 |     184 B |        1.00 |        |
| After  | Ping         | 27.83 ns |  0.64 |     112 B |        0.61 | After  |
| Before | UInt16Length | 39.63 ns |  1.00 |     144 B |        1.00 |        |
| After  | UInt16Length |  9.03 ns |  0.23 |         - |        0.00 | After  |
| Before | UInt32Length | 39.28 ns |  1.00 |     144 B |        1.00 |        |
| After  | UInt32Length | 15.06 ns |  0.38 |         - |        0.00 | After  |

After is faster and allocates less on every frame. Compact / 0xFD / 0xFE empty-payload
headers allocate nothing after the change. Ping still copies the payload in both.
*/
