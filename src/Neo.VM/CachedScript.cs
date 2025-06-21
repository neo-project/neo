// Copyright (C) 2015-2025 The Neo Project.
//
// CachedScript.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.VM.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    /// <summary>
    /// Represents a script with pre-decoded instruction cache for improved performance.
    /// </summary>
    [DebuggerDisplay("Length={Length}")]
    public class CachedScript : Script
    {
        private readonly Instruction?[] _instructionCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedScript"/> class.
        /// </summary>
        /// <param name="script">The bytecodes of the script.</param>
        public CachedScript(ReadOnlyMemory<byte> script) : this(script, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedScript"/> class.
        /// </summary>
        /// <param name="script">The bytecodes of the script.</param>
        /// <param name="strictMode">
        /// Indicates whether strict mode is enabled.
        /// In strict mode, the script will be checked, but the loading speed will be slower.
        /// </param>
        /// <exception cref="BadScriptException">In strict mode, the script was found to contain bad instructions.</exception>
        public CachedScript(ReadOnlyMemory<byte> script, bool strictMode) : base(script, strictMode)
        {
            _instructionCache = new Instruction?[Length];
            
            // Pre-decode all instructions for scripts under reasonable size
            if (Length <= 10000) // Adjust threshold based on memory constraints
            {
                PreDecodeAllInstructions();
            }
        }

        private void PreDecodeAllInstructions()
        {
            try
            {
                for (int ip = 0; ip < Length;)
                {
                    var instruction = base.GetInstruction(ip);
                    _instructionCache[ip] = instruction;
                    ip += instruction.Size;
                }
            }
            catch
            {
                // If pre-decoding fails, fall back to lazy decoding
                System.Array.Clear(_instructionCache, 0, _instructionCache.Length);
            }
        }

        /// <summary>
        /// Get the <see cref="Instruction"/> at the specified position with caching.
        /// </summary>
        /// <param name="ip">The position to get the <see cref="Instruction"/>.</param>
        /// <returns>The <see cref="Instruction"/> at the specified position.</returns>
        /// <exception cref="ArgumentException">In strict mode, the <see cref="Instruction"/> was not found at the specified position.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new Instruction GetInstruction(int ip)
        {
            if (ip < 0 || ip >= Length)
                throw new ArgumentOutOfRangeException(nameof(ip));

            // Check cache first
            var cached = _instructionCache[ip];
            if (cached != null)
                return cached;

            // Decode and cache
            var instruction = base.GetInstruction(ip);
            _instructionCache[ip] = instruction;
            return instruction;
        }
    }
}