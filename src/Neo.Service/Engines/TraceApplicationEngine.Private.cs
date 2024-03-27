// Copyright (C) 2015-2024 The Neo Project.
//
// TraceApplicationEngine.Private.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text;

namespace Neo.Service.Engines
{
    internal partial class TraceApplicationEngine
    {
        private static readonly string s_nullString = "null";

        private readonly ConcurrentQueue<Instruction> _postExecutedInstructions = new();
        private readonly ConcurrentQueue<LogEventArgs> _debugLogEvents = new();
        private readonly StringBuilder _traceLog = new();
        private readonly ILogger? _logger;

        private int _vmAddress = 0;

        private void OnLog(object? sender, LogEventArgs e)
        {
            if (ReferenceEquals(sender, this)) return;
        }

        private void TraceLog(Slot? slot, string prefix)
        {
            if (slot is null) return;
            for (var i = 0; i < slot.Count; i++)
            {
                var stackItem = slot[i];
                var stackItemString = TraceLogFormat(stackItem);
                _traceLog.AppendLine($"{prefix[..2]}_{i:X6}: {stackItem.Type,-12} {stackItemString}");
            }
        }

        private void TraceLog(Instruction instruction)
        {
            switch (instruction.OpCode)
            {
                case OpCode.PUSHINT8:
                case OpCode.PUSHINT16:
                case OpCode.PUSHINT32:
                case OpCode.PUSHINT64:
                case OpCode.PUSHINT128:
                case OpCode.PUSHINT256:
                    {
                        var value = new BigInteger(instruction.Operand.Span);
                        _traceLog.AppendLine($"VM_{_vmAddress:X6}: {instruction.OpCode,-12} {value}");
                        break;
                    }
                case OpCode.CALL:
                    {

                        var value = BigInteger.Add(_vmAddress, new BigInteger(instruction.Operand.Span));
                        _traceLog.AppendLine($"VM_{_vmAddress:X6}: {instruction.OpCode,-12} VM:{value:X6}");
                        break;
                    }
                default:
                    {
                        var dataHexString = System.Convert.ToHexString(instruction.Operand.Span);
                        _traceLog.AppendLine($"VM_{_vmAddress:X6}: {instruction.OpCode,-12} {dataHexString}");
                    }
                    break;
            }
            _vmAddress += instruction.Size;
        }

        private static string TraceLogFormat(StackItem? stackItem) =>
            stackItem?.Type switch
            {
                StackItemType.Pointer => $"VMS_{((Pointer)stackItem).Position:X6}",
                StackItemType.Boolean => $"{stackItem.GetBoolean()}",
                StackItemType.Integer => $"{stackItem.GetInteger()}",
                StackItemType.ByteString => $"0x{System.Convert.ToHexString(stackItem.GetSpan())}",
                StackItemType.Array or StackItemType.Struct or StackItemType.Map => $"Size={((CompoundType)stackItem).Count}",
                StackItemType.Any or _ => s_nullString,
            };
    }
}
