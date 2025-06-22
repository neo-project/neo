// Copyright (C) 2015-2025 The Neo Project.
//
// FastInteger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    /// <summary>
    /// Optimized integer implementation with fast paths for int64 operations.
    /// Uses native long arithmetic when possible, falls back to BigInteger for large values.
    /// Includes caching for common values to improve performance.
    /// </summary>
    [DebuggerDisplay("Type={GetType().Name}, Value={GetDebugValue()}")]
    public sealed class FastInteger : PrimitiveType
    {
        public const int MaxSize = 32;

        // Cache for common integer values
        private static readonly FastInteger[] _cache = new FastInteger[513]; // -256 to 256
        private const int CacheOffset = 256;

        // Static common values
        public static readonly FastInteger Zero;
        public static readonly FastInteger One;
        public static readonly FastInteger MinusOne;

        static FastInteger()
        {
            // Initialize cache with common values
            for (int i = 0; i < _cache.Length; i++)
            {
                int value = i - CacheOffset;
                _cache[i] = new FastInteger(value, false);
            }

            Zero = _cache[CacheOffset];
            One = _cache[CacheOffset + 1];
            MinusOne = _cache[CacheOffset - 1];
        }

        private readonly bool _isBig;
        private readonly long _longValue;
        private readonly BigInteger _bigValue;
        private readonly int _size;

        // Fast path constructor for long values
        private FastInteger(long value, bool checkCache = true)
        {
            if (checkCache && value >= -CacheOffset && value <= CacheOffset)
            {
                // This should not happen during normal operation since we use Create()
                throw new InvalidOperationException("Use Create() for cached values");
            }

            _isBig = false;
            _longValue = value;
            _bigValue = default;

            if (value == 0)
            {
                _size = 0;
            }
            else
            {
                // Calculate size for long value
                _size = GetByteCountForLong(value);
            }
        }

        // BigInteger constructor
        private FastInteger(BigInteger value)
        {
            _isBig = true;
            _longValue = 0;
            _bigValue = value;

            if (value.IsZero)
            {
                _size = 0;
            }
            else
            {
                _size = value.GetByteCount();
                if (_size > MaxSize)
                    throw new ArgumentException($"Cannot create {nameof(FastInteger)}, MaxSize exceeded: {_size}/{MaxSize}");
            }
        }

        public override ReadOnlyMemory<byte> Memory
        {
            get
            {
                if (_size == 0) return ReadOnlyMemory<byte>.Empty;

                if (_isBig)
                {
                    return _bigValue.ToByteArray();
                }
                else
                {
                    // Convert long to byte array
                    return new BigInteger(_longValue).ToByteArray();
                }
            }
        }

        public override int Size => _size;
        public override StackItemType Type => StackItemType.Integer;

        /// <summary>
        /// Create a FastInteger from a long value, using cache for common values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastInteger Create(long value)
        {
            if (value >= -CacheOffset && value <= CacheOffset)
            {
                return _cache[value + CacheOffset];
            }
            return new FastInteger(value, false);
        }

        /// <summary>
        /// Create a FastInteger from a BigInteger value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastInteger Create(BigInteger value)
        {
            // Try to fit in long range for optimization
            if (value >= long.MinValue && value <= long.MaxValue)
            {
                return Create((long)value);
            }
            return new FastInteger(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool GetBoolean()
        {
            return _isBig ? !_bigValue.IsZero : _longValue != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override BigInteger GetInteger()
        {
            return _isBig ? _bigValue : new BigInteger(_longValue);
        }

        /// <summary>
        /// Get the long value if it fits in long range, otherwise throw.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetLong()
        {
            if (_isBig)
            {
                if (_bigValue >= long.MinValue && _bigValue <= long.MaxValue)
                    return (long)_bigValue;
                throw new OverflowException("Value too large for long");
            }
            return _longValue;
        }

        /// <summary>
        /// Try to get the long value if it fits in long range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetLong(out long value)
        {
            if (_isBig)
            {
                if (_bigValue >= long.MinValue && _bigValue <= long.MaxValue)
                {
                    value = (long)_bigValue;
                    return true;
                }
                value = 0;
                return false;
            }
            value = _longValue;
            return true;
        }

        /// <summary>
        /// Get int value for array indexing, with bounds checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetInt32()
        {
            if (_isBig)
            {
                if (_bigValue >= int.MinValue && _bigValue <= int.MaxValue)
                    return (int)_bigValue;
                throw new OverflowException("Value too large for int32");
            }

            if (_longValue >= int.MinValue && _longValue <= int.MaxValue)
                return (int)_longValue;
            throw new OverflowException("Value too large for int32");
        }

        public override bool Equals(StackItem? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is not FastInteger fi) return false;

            if (_isBig && fi._isBig)
                return _bigValue == fi._bigValue;
            if (!_isBig && !fi._isBig)
                return _longValue == fi._longValue;

            // Mixed comparison
            if (_isBig)
                return _bigValue == fi._longValue;
            else
                return _longValue == fi._bigValue;
        }

        public override int GetHashCode()
        {
            return _isBig ? _bigValue.GetHashCode() : _longValue.GetHashCode();
        }

        // Arithmetic operations with fast paths

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastInteger Add(FastInteger left, FastInteger right)
        {
            if (!left._isBig && !right._isBig)
            {
                // Fast path: both are long values
                try
                {
                    long result = checked(left._longValue + right._longValue);
                    return Create(result);
                }
                catch (OverflowException)
                {
                    // Fall back to BigInteger
                    return Create(new BigInteger(left._longValue) + new BigInteger(right._longValue));
                }
            }

            // Slow path: at least one is BigInteger
            return Create(left.GetInteger() + right.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastInteger Subtract(FastInteger left, FastInteger right)
        {
            if (!left._isBig && !right._isBig)
            {
                // Fast path: both are long values
                try
                {
                    long result = checked(left._longValue - right._longValue);
                    return Create(result);
                }
                catch (OverflowException)
                {
                    // Fall back to BigInteger
                    return Create(new BigInteger(left._longValue) - new BigInteger(right._longValue));
                }
            }

            // Slow path: at least one is BigInteger
            return Create(left.GetInteger() - right.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastInteger Multiply(FastInteger left, FastInteger right)
        {
            if (!left._isBig && !right._isBig)
            {
                // Fast path: both are long values
                try
                {
                    long result = checked(left._longValue * right._longValue);
                    return Create(result);
                }
                catch (OverflowException)
                {
                    // Fall back to BigInteger
                    return Create(new BigInteger(left._longValue) * new BigInteger(right._longValue));
                }
            }

            // Slow path: at least one is BigInteger
            return Create(left.GetInteger() * right.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastInteger Divide(FastInteger left, FastInteger right)
        {
            if (!left._isBig && !right._isBig)
            {
                // Fast path: both are long values
                if (right._longValue == 0)
                    throw new DivideByZeroException();

                long result = left._longValue / right._longValue;
                return Create(result);
            }

            // Slow path: at least one is BigInteger
            return Create(left.GetInteger() / right.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastInteger Modulo(FastInteger left, FastInteger right)
        {
            if (!left._isBig && !right._isBig)
            {
                // Fast path: both are long values
                if (right._longValue == 0)
                    throw new DivideByZeroException();

                long result = left._longValue % right._longValue;
                return Create(result);
            }

            // Slow path: at least one is BigInteger
            return Create(left.GetInteger() % right.GetInteger());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastInteger Negate(FastInteger value)
        {
            if (!value._isBig)
            {
                // Fast path: long value
                if (value._longValue == long.MinValue)
                {
                    // -long.MinValue overflows to BigInteger
                    return Create(-new BigInteger(value._longValue));
                }
                return Create(-value._longValue);
            }

            // Slow path: BigInteger
            return Create(-value._bigValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastInteger Increment(FastInteger value)
        {
            if (!value._isBig)
            {
                // Fast path: long value
                if (value._longValue == long.MaxValue)
                {
                    // Increment overflows to BigInteger
                    return Create(new BigInteger(value._longValue) + 1);
                }
                return Create(value._longValue + 1);
            }

            // Slow path: BigInteger
            return Create(value._bigValue + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastInteger Decrement(FastInteger value)
        {
            if (!value._isBig)
            {
                // Fast path: long value
                if (value._longValue == long.MinValue)
                {
                    // Decrement overflows to BigInteger
                    return Create(new BigInteger(value._longValue) - 1);
                }
                return Create(value._longValue - 1);
            }

            // Slow path: BigInteger
            return Create(value._bigValue - 1);
        }

        // Comparison operations

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(FastInteger left, FastInteger right)
        {
            if (!left._isBig && !right._isBig)
                return left._longValue < right._longValue;
            return left.GetInteger() < right.GetInteger();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(FastInteger left, FastInteger right)
        {
            if (!left._isBig && !right._isBig)
                return left._longValue <= right._longValue;
            return left.GetInteger() <= right.GetInteger();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(FastInteger left, FastInteger right)
        {
            if (!left._isBig && !right._isBig)
                return left._longValue > right._longValue;
            return left.GetInteger() > right.GetInteger();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(FastInteger left, FastInteger right)
        {
            if (!left._isBig && !right._isBig)
                return left._longValue >= right._longValue;
            return left.GetInteger() >= right.GetInteger();
        }

        // Implicit conversions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastInteger(sbyte value) => Create(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastInteger(byte value) => Create(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastInteger(short value) => Create(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastInteger(ushort value) => Create(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastInteger(int value) => Create(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastInteger(uint value) => Create(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastInteger(long value) => Create(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastInteger(ulong value) => Create(value <= long.MaxValue ? (long)value : new BigInteger(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastInteger(BigInteger value) => Create(value);

        public override string ToString()
        {
            return _isBig ? _bigValue.ToString() : _longValue.ToString();
        }

        private string GetDebugValue()
        {
            return ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetByteCountForLong(long value)
        {
            if (value == 0) return 0;

            // Simple byte count calculation for long values
            if (value >= -128 && value <= 127) return 1;
            if (value >= -32768 && value <= 32767) return 2;
            if (value >= -2147483648 && value <= 2147483647) return 4;
            return 8;
        }
    }
}
