// Copyright (C) 2015-2025 The Neo Project.
//
// RealisticBenchmarkBasedGasPricing.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    /// <summary>
    /// Realistic benchmark-based gas pricing system for Neo VM opcodes.
    /// Based on actual operation complexity rather than identical category timings.
    /// </summary>
    public static class RealisticBenchmarkBasedGasPricing
    {
        public const long BASE_GAS_UNIT = 1;
        public const double BASELINE_TIME_NS = 280.0; // NOP baseline

        /// <summary>
        /// Calculate gas cost for an opcode based on realistic timing analysis.
        /// </summary>
        public static long CalculateGasCost(OpCode opcode, params object[] parameters)
        {
            var gasCost = GetBaseGasCost(opcode);

            // Add parameter-dependent costs for variable operations
            if (IsSizeDependent(opcode) && parameters != null && parameters.Length > 0)
            {
                gasCost += CalculateParameterCost(opcode, parameters);
            }

            return Math.Max(gasCost, BASE_GAS_UNIT);
        }

        /// <summary>
        /// Get base gas cost for each opcode based on realistic timing analysis.
        /// </summary>
        private static long GetBaseGasCost(OpCode opcode)
        {
            return opcode switch
            {
                // Constants (0x00-0x20) - Fast register operations
                OpCode.PUSHINT8 => 1,
                OpCode.PUSHINT16 => 1,
                OpCode.PUSHINT32 => 2,
                OpCode.PUSHINT64 => 2,
                OpCode.PUSHINT128 => 3,
                OpCode.PUSHINT256 => 4,
                OpCode.PUSHT => 1,
                OpCode.PUSHF => 1,
                OpCode.PUSHNULL => 1,
                OpCode.PUSHM1 => 1,
                OpCode.PUSH0 => 1,
                OpCode.PUSH1 => 1,
                OpCode.PUSH2 => 1,
                OpCode.PUSH3 => 1,
                OpCode.PUSH4 => 1,
                OpCode.PUSH5 => 1,
                OpCode.PUSH6 => 1,
                OpCode.PUSH7 => 1,
                OpCode.PUSH8 => 1,
                OpCode.PUSH9 => 1,
                OpCode.PUSH10 => 1,
                OpCode.PUSH11 => 1,
                OpCode.PUSH12 => 1,
                OpCode.PUSH13 => 1,
                OpCode.PUSH14 => 1,
                OpCode.PUSH15 => 1,
                OpCode.PUSH16 => 1,

                // Flow Control (0x21-0x41) - Jump operations
                OpCode.NOP => 1,
                OpCode.JMP => 2,
                OpCode.JMP_L => 2,
                OpCode.JMPIF => 2,
                OpCode.JMPIF_L => 2,
                OpCode.JMPIFNOT => 2,
                OpCode.JMPIFNOT_L => 2,
                OpCode.JMPEQ => 2,
                OpCode.JMPEQ_L => 2,
                OpCode.JMPNE => 2,
                OpCode.JMPNE_L => 2,
                OpCode.JMPGT => 2,
                OpCode.JMPGT_L => 2,
                OpCode.JMPGE => 2,
                OpCode.JMPGE_L => 2,
                OpCode.JMPLT => 2,
                OpCode.JMPLT_L => 2,
                OpCode.JMPLE => 2,
                OpCode.JMPLE_L => 2,
                OpCode.CALL => 3,
                OpCode.CALL_L => 3,
                OpCode.CALLA => 3,
                OpCode.CALLT => 4,
                OpCode.ABORT => 3,
                OpCode.ASSERT => 2,
                OpCode.THROW => 3,
                OpCode.TRY => 2,
                OpCode.TRY_L => 2,
                OpCode.ENDTRY => 2,
                OpCode.ENDTRY_L => 2,
                OpCode.ENDFINALLY => 2,
                OpCode.RET => 1,

                // Stack Operations (0x43-0x55) - Stack manipulation
                OpCode.DEPTH => 2,
                OpCode.DROP => 2,
                OpCode.NIP => 2,
                OpCode.XDROP => 3,
                OpCode.CLEAR => 4,
                OpCode.DUP => 2,
                OpCode.OVER => 2,
                OpCode.PICK => 2,
                OpCode.TUCK => 2,
                OpCode.SWAP => 2,
                OpCode.ROT => 2,
                OpCode.ROLL => 3,
                OpCode.REVERSE3 => 2,
                OpCode.REVERSE4 => 2,
                OpCode.REVERSEN => 4,

                // Bitwise Logic (0x90-0x98) - Fast CPU operations
                OpCode.INVERT => 2,
                OpCode.AND => 2,
                OpCode.OR => 2,
                OpCode.XOR => 2,
                OpCode.EQUAL => 2,
                OpCode.NOTEQUAL => 2,

                // Arithmetic (0x99-0xBB) - Mathematical operations
                OpCode.SIGN => 2,
                OpCode.ABS => 2,
                OpCode.NEGATE => 2,
                OpCode.INC => 2,
                OpCode.DEC => 2,
                OpCode.ADD => 2,
                OpCode.SUB => 2,
                OpCode.MUL => 2,
                OpCode.DIV => 3,
                OpCode.MOD => 3,
                OpCode.POW => 4,
                OpCode.SQRT => 4,
                OpCode.MODPOW => 6,
                OpCode.SHL => 2,
                OpCode.SHR => 2,
                OpCode.NOT => 2,
                OpCode.BOOLAND => 2,
                OpCode.BOOLOR => 2,
                OpCode.NZ => 2,
                OpCode.NUMEQUAL => 3,
                OpCode.NUMNOTEQUAL => 3,
                OpCode.LT => 2,
                OpCode.LE => 2,
                OpCode.GT => 2,
                OpCode.GE => 2,
                OpCode.MIN => 2,
                OpCode.MAX => 2,
                OpCode.WITHIN => 3,

                // Compound Types (0xBE-0xD4) - Memory allocation
                OpCode.NEWARRAY0 => 2,
                OpCode.NEWSTRUCT0 => 2,
                OpCode.NEWMAP => 2,
                OpCode.SIZE => 2,
                OpCode.HASKEY => 4,
                OpCode.KEYS => 9,
                OpCode.VALUES => 9,
                OpCode.PICKITEM => 3,
                OpCode.APPEND => 3,
                OpCode.SETITEM => 3,
                OpCode.REVERSEITEMS => 8,
                OpCode.REMOVE => 3,
                OpCode.CLEARITEMS => 4,
                OpCode.POPITEM => 3,

                // Type Operations (0xD8-0xDB) - Type checking
                OpCode.ISNULL => 2,
                OpCode.ISTYPE => 2,
                OpCode.CONVERT => 5,

                // Data Operations (0x0C-0x0E, 0x7C-0x81) - Variable cost
                OpCode.PUSHDATA1 => 2,   // + size cost
                OpCode.PUSHDATA2 => 3,   // + size cost
                OpCode.PUSHDATA4 => 4,   // + size cost
                OpCode.NEWARRAY => 3,    // + size cost
                OpCode.NEWSTRUCT => 3,   // + size cost
                OpCode.PACK => 4,        // + item count cost
                OpCode.UNPACK => 5,      // + size cost
                OpCode.CAT => 6,         // + length cost
                OpCode.SUBSTR => 5,      // + length cost
                OpCode.LEFT => 5,        // + count cost
                OpCode.RIGHT => 5,       // + count cost

                // Default for unknown opcodes
                _ => 2
            };
        }

        /// <summary>
        /// Check if opcode cost depends on input parameters.
        /// </summary>
        private static bool IsSizeDependent(OpCode opcode)
        {
            return opcode switch
            {
                OpCode.PUSHDATA1 or OpCode.PUSHDATA2 or OpCode.PUSHDATA4 => true,
                OpCode.NEWARRAY or OpCode.NEWSTRUCT => true,
                OpCode.PACK or OpCode.UNPACK => true,
                OpCode.CAT or OpCode.SUBSTR or OpCode.LEFT or OpCode.RIGHT => true,
                OpCode.DIV or OpCode.MOD => true,
                OpCode.POW or OpCode.SQRT or OpCode.MODPOW => true,
                _ => false
            };
        }

        /// <summary>
        /// Calculate additional gas cost based on parameters.
        /// </summary>
        private static long CalculateParameterCost(OpCode opcode, object[] parameters)
        {
            if (parameters.Length == 0) return 0;

            return opcode switch
            {
                // Data push operations - cost based on data size
                OpCode.PUSHDATA1 => CalculateDataPushCost(parameters[0], 0.002),
                OpCode.PUSHDATA2 => CalculateDataPushCost(parameters[0], 0.001),
                OpCode.PUSHDATA4 => CalculateDataPushCost(parameters[0], 0.0003),

                // Array/Struct creation - cost based on size
                OpCode.NEWARRAY => CalculateSizeCost(parameters[0], 0.008),
                OpCode.NEWSTRUCT => CalculateSizeCost(parameters[0], 0.010),

                // Pack/Unpack operations - cost based on item count
                OpCode.PACK => CalculateItemCountCost(parameters[0], 0.030),
                OpCode.UNPACK => CalculateSizeCost(parameters[0], 0.040),

                // String operations - cost based on length
                OpCode.CAT => CalculateStringConcatCost(parameters),
                OpCode.SUBSTR => CalculateLengthCost(parameters.Length > 1 ? parameters[1] : parameters[0], 0.004),
                OpCode.LEFT => CalculateLengthCost(parameters[0], 0.004),
                OpCode.RIGHT => CalculateLengthCost(parameters[0], 0.004),

                // Mathematical operations with complexity scaling
                OpCode.DIV or OpCode.MOD => Math.Max(1, (long)Math.Log2(GetIntegerValue(parameters[1]))),
                OpCode.POW => Math.Max(1, (long)(64 + Math.Log2(GetIntegerValue(parameters[1])) * 0.5)),
                OpCode.SQRT => Math.Max(1, (long)(64 + Math.Log2(GetIntegerValue(parameters[0])) * 0.3)),
                OpCode.MODPOW => Math.Max(1, (long)(512 + Math.Log2(GetIntegerValue(parameters[1])) * 0.4)),

                _ => 0
            };
        }

        /// <summary>
        /// Calculate cost for data push operations.
        /// </summary>
        private static long CalculateDataPushCost(object data, double factor)
        {
            long dataSize = GetSizeValue(data);
            return (long)Math.Ceiling(dataSize * factor);
        }

        /// <summary>
        /// Calculate cost for size-dependent operations.
        /// </summary>
        private static long CalculateSizeCost(object size, double factor)
        {
            long sizeValue = GetIntegerValue(size);
            return (long)Math.Ceiling(sizeValue * factor);
        }

        /// <summary>
        /// Calculate cost for item count dependent operations.
        /// </summary>
        private static long CalculateItemCountCost(object count, double factor)
        {
            long countValue = GetIntegerValue(count);
            return (long)Math.Ceiling(countValue * factor);
        }

        /// <summary>
        /// Calculate cost for string concatenation.
        /// </summary>
        private static long CalculateStringConcatCost(object[] parameters)
        {
            if (parameters.Length < 2) return 0;

            long totalLength = GetSizeValue(parameters[0]) + GetSizeValue(parameters[1]);
            return (long)Math.Ceiling(totalLength * 0.003);
        }

        /// <summary>
        /// Calculate cost for length-dependent operations.
        /// </summary>
        private static long CalculateLengthCost(object length, double factor)
        {
            long lengthValue = GetIntegerValue(length);
            return (long)Math.Ceiling(lengthValue * factor);
        }

        /// <summary>
        /// Get integer value from parameter.
        /// </summary>
        private static long GetIntegerValue(object value)
        {
            if (value is int i) return Math.Max(1, i);
            if (value is long l) return Math.Max(1, l);
            if (value is uint ui) return Math.Max(1, (long)ui);
            if (value is ulong ul) return Math.Max(1, (long)Math.Min(ul, long.MaxValue));
            if (value is byte[] bytes) return Math.Max(1, bytes.Length);
            if (value is string str) return Math.Max(1, str.Length);

            return 1; // Default minimum value
        }

        /// <summary>
        /// Get size value from parameter (for arrays, strings, etc.).
        /// </summary>
        private static long GetSizeValue(object value)
        {
            if (value is byte[] bytes) return bytes.Length;
            if (value is string str) return str.Length;
            if (value is Array array) return array.Length;

            return GetIntegerValue(value);
        }

        /// <summary>
        /// Get complete gas price table for all opcodes.
        /// </summary>
        public static Dictionary<OpCode, long> GetCompleteGasPriceTable()
        {
            var gasPrices = new Dictionary<OpCode, long>();

            // Populate all opcodes with their base costs
            foreach (OpCode opcode in Enum.GetValues<OpCode>())
            {
                gasPrices[opcode] = GetBaseGasCost(opcode);
            }

            return gasPrices;
        }

        /// <summary>
        /// Get realistic performance ratio for an opcode category.
        /// </summary>
        public static double GetPerformanceRatio(OpCode opcode)
        {
            var baseCost = GetBaseGasCost(opcode);
            var timingNs = EstimateOpcodeTiming(opcode);
            return timingNs / BASELINE_TIME_NS;
        }

        /// <summary>
        /// Estimate realistic timing for an opcode in nanoseconds.
        /// </summary>
        private static double EstimateOpcodeTiming(OpCode opcode)
        {
            return opcode switch
            {
                // Constants - very fast
                OpCode.PUSHINT8 or OpCode.PUSHINT16 or OpCode.PUSHT or OpCode.PUSHF or OpCode.PUSHNULL or OpCode.PUSHM1 => 300.0,
                OpCode.PUSH0 or OpCode.PUSH1 or OpCode.PUSH2 or OpCode.PUSH3 or OpCode.PUSH4 or OpCode.PUSH5 => 300.0,
                OpCode.PUSHINT32 => 360.0,
                OpCode.PUSHINT64 => 380.0,
                OpCode.PUSHINT128 => 450.0,
                OpCode.PUSHINT256 => 520.0,

                // Flow control
                OpCode.NOP => 280.0,
                OpCode.JMP or OpCode.JMP_L => 420.0,
                OpCode.JMPIF or OpCode.JMPIFNOT or OpCode.JMPIF_L or OpCode.JMPIFNOT_L => 480.0,
                OpCode.JMPEQ or OpCode.JMPNE or OpCode.JMPGT or OpCode.JMPGE or OpCode.JMPLT or OpCode.JMPLE => 520.0,
                OpCode.CALL or OpCode.CALL_L or OpCode.CALLA => 850.0,
                OpCode.CALLT => 1200.0,
                OpCode.RET => 380.0,

                // Stack operations
                OpCode.DUP or OpCode.DROP => 340.0,
                OpCode.SWAP => 380.0,
                OpCode.OVER or OpCode.TUCK => 440.0,
                OpCode.PICK or OpCode.ROT => 520.0,
                OpCode.ROLL or OpCode.XDROP => 650.0,
                OpCode.CLEAR => 1200.0,

                // Bitwise operations - fast
                OpCode.INVERT => 380.0,
                OpCode.AND or OpCode.OR or OpCode.XOR => 420.0,
                OpCode.EQUAL or OpCode.NOTEQUAL => 450.0,

                // Arithmetic operations
                OpCode.INC or OpCode.DEC => 340.0,
                OpCode.ADD or OpCode.SUB => 420.0,
                OpCode.MUL => 480.0,
                OpCode.DIV or OpCode.MOD => 680.0,
                OpCode.POW => 1200.0,
                OpCode.SQRT => 950.0,
                OpCode.MODPOW => 1800.0,

                // Compound types
                OpCode.NEWARRAY0 or OpCode.NEWSTRUCT0 => 550.0,
                OpCode.NEWMAP => 620.0,
                OpCode.SIZE => 480.0,
                OpCode.PICKITEM => 750.0,
                OpCode.SETITEM => 900.0,
                OpCode.KEYS => 2500.0,
                OpCode.VALUES => 2800.0,

                // Default
                _ => 500.0
            };
        }
    }
}
