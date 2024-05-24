// Copyright (C) 2015-2024 The Neo Project.
//
// NativePointer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LevelDB.NativePointer
{
    // note: sizeof(Ptr<>) == sizeof(IntPtr) allows you to create Ptr<Ptr<>> of arbitrary depth and "it just works"
    // IntPtr severely lacks appropriate arithmetic operators; up-promotions to ulong used instead.
    public struct Ptr<T>(IntPtr addr)
        where T : struct
    {
        private IntPtr _addr = addr;

        // cannot use 'sizeof' operator on generic type parameters
        public static readonly uint SizeofT = (uint)Marshal.SizeOf(typeof(T));
        private static readonly IDeref<T> s_deref = GetDeref();

        private static IDeref<T> GetDeref()
        {
            if (typeof(T) == typeof(int))
                return (IDeref<T>)new IntDeref();

            // TODO: other concrete implementations of IDeref.
            // (can't be made generic; will not type check)

            // fallback
            return new MarshalDeref<T>();
        }

        public static explicit operator Ptr<T>(IntPtr p)
        {
            return new Ptr<T>(p);
        }
        public static explicit operator IntPtr(Ptr<T> p)
        {
            return p._addr;
        }

        // operator Ptr<U>(Ptr<T>)
        public static Ptr<U> Cast<U>(Ptr<T> p)
            where U : struct
        {
            return new Ptr<U>(p._addr);
        }

        public void Inc() { Advance((IntPtr)1); }
        public void Dec() { Advance((IntPtr)(-1)); }

        public void Advance(IntPtr d)
        {
            _addr = (IntPtr)((ulong)_addr + SizeofT * (ulong)d);
        }
        public readonly IntPtr Diff(Ptr<T> p2)
        {
            var diff = (long)(((ulong)_addr) - ((ulong)p2._addr));
            Debug.Assert(diff % SizeofT == 0);

            return checked((IntPtr)(diff / SizeofT));
        }
        public readonly T Deref()
        {
            return s_deref.Deref(_addr);
        }
        public readonly void DerefWrite(T newValue)
        {
            s_deref.DerefWrite(_addr, newValue);
        }

        // C-style pointer arithmetic. IntPtr is used in place of C's ptrdiff_t
        #region pointer/intptr arithmetic
        public static Ptr<T> operator ++(Ptr<T> p)
        {
            p.Inc();
            return p;
        }
        public static Ptr<T> operator --(Ptr<T> p)
        {
            p.Dec();
            return p;
        }
        public static Ptr<T> operator +(Ptr<T> p, IntPtr offset)
        {
            p.Advance(offset);
            return p;
        }
        public static Ptr<T> operator +(IntPtr offset, Ptr<T> p)
        {
            p.Advance(offset);
            return p;
        }
        public static Ptr<T> operator -(Ptr<T> p, IntPtr offset)
        {
            p.Advance((IntPtr)(0 - (ulong)offset));
            return p;
        }
        public static IntPtr operator -(Ptr<T> p, Ptr<T> p2)
        {
            return p.Diff(p2);
        }
        public readonly T this[IntPtr offset]
        {
            get { return (this + offset).Deref(); }

            set { (this + offset).DerefWrite(value); }
        }
        #endregion

        #region comparisons
        public override readonly bool Equals(object obj)
        {
            if (obj is not Ptr<T>)
                return false;
            return this == (Ptr<T>)obj;
        }
        public override readonly int GetHashCode()
        {
            return checked((int)_addr ^ (int)(IntPtr)((long)_addr >> 6));
        }
        public static bool operator ==(Ptr<T> p, Ptr<T> p2)
        {
            return (IntPtr)p == (IntPtr)p2;
        }
        public static bool operator !=(Ptr<T> p, Ptr<T> p2)
        {
            return (IntPtr)p != (IntPtr)p2;
        }
        public static bool operator <(Ptr<T> p, Ptr<T> p2)
        {
            return (ulong)(IntPtr)p < (ulong)(IntPtr)p2;
        }
        public static bool operator >(Ptr<T> p, Ptr<T> p2)
        {
            return (ulong)(IntPtr)p > (ulong)(IntPtr)p2;
        }
        public static bool operator <=(Ptr<T> p, Ptr<T> p2)
        {
            return (ulong)(IntPtr)p <= (ulong)(IntPtr)p2;
        }
        public static bool operator >=(Ptr<T> p, Ptr<T> p2)
        {
            return (ulong)(IntPtr)p >= (ulong)(IntPtr)p2;
        }
        #endregion

        #region pointer/int/long arithmetic (convenience)
        public static Ptr<T> operator +(Ptr<T> p, long offset)
        {
            return p + checked((IntPtr)offset);
        }
        public static Ptr<T> operator +(long offset, Ptr<T> p)
        {
            return p + checked((IntPtr)offset);
        }
        public static Ptr<T> operator -(Ptr<T> p, long offset)
        {
            return p - checked((IntPtr)offset);
        }
        public T this[long offset]
        {
            readonly get { return this[checked((IntPtr)offset)]; }
            set { this[checked((IntPtr)offset)] = value; }
        }
        #endregion
    }

    public struct NativeArray
        : IDisposable
    {
        public IntPtr _baseAddr;
        public IntPtr _byteLength;

        public SafeHandle _handle;

        public readonly void Dispose()
        {
            _handle?.Dispose();
        }

        public static NativeArray<T> FromArray<T>(T[] arr, long start = 0, long count = -1)
            where T : struct
        {
            if (count < 0) count = arr.LongLength - start;

            var h = new PinnedSafeHandle<T>(arr);
            return new NativeArray<T> { _baseAddr = h.Ptr + start, _count = checked((IntPtr)count), _handle = h };
        }
    }

    public struct NativeArray<T>
        : IEnumerable<T>
        , IDisposable
        where T : struct
    {
        public Ptr<T> _baseAddr;
        public IntPtr _count;

        public SafeHandle _handle;

        public static implicit operator NativeArray(NativeArray<T> arr)
        {
            return new NativeArray
            {
                _baseAddr = (IntPtr)arr._baseAddr,
                _byteLength = (IntPtr)((ulong)(IntPtr)(arr._baseAddr + arr._count) - (ulong)(IntPtr)(arr._baseAddr)),
                _handle = arr._handle
            };
        }
        public static explicit operator NativeArray<T>(NativeArray arr)
        {
            var baseAddr = (Ptr<T>)arr._baseAddr;
            var count = ((Ptr<T>)(IntPtr)((ulong)arr._baseAddr + (ulong)arr._byteLength)) - baseAddr;

            return new NativeArray<T> { _baseAddr = baseAddr, _count = count, _handle = arr._handle };
        }

        #region IEnumerable

        public readonly IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(_baseAddr, _baseAddr + _count, _handle);
        }

        readonly System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class Enumerator(Ptr<T> start, Ptr<T> end, SafeHandle handle) : IEnumerator<T>
        {
            private Ptr<T> _current = start;
            private Ptr<T> _end = end;
            private int _state = 0;
            private readonly SafeHandle _handle = handle;

            public void Dispose()
            {
                GC.KeepAlive(_handle);
            }

            public T Current
            {
                get
                {
                    if (_handle != null && _handle.IsClosed)
                        throw new InvalidOperationException("Dereferencing a closed handle");
                    if (_state != 1)
                        throw new InvalidOperationException("Attempt to invoke Current on invalid enumerator");
                    return _current.Deref();
                }
            }

            public bool MoveNext()
            {
                switch (_state)
                {
                    case 0:
                        _state = 1;
                        return _current != _end;
                    case 1:
                        ++_current;
                        if (_current == _end)
                            _state = 2;
                        return _current != _end;
                    case 2:
                    default:
                        return false;
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }
        }

        #endregion

        public T this[IntPtr offset]
        {
            readonly get
            {
                if ((ulong)offset >= (ulong)_count)
                    throw new IndexOutOfRangeException("offest");
                var val = _baseAddr[offset];
                GC.KeepAlive(this);
                return val;
            }
            set
            {
                if ((ulong)offset >= (ulong)_count)
                    throw new IndexOutOfRangeException("offest");
                _baseAddr[offset] = value;
                GC.KeepAlive(this);
            }
        }
        public T this[long offset]
        {
            readonly get { return this[checked((IntPtr)offset)]; }
            set { this[checked((IntPtr)offset)] = value; }
        }

        public readonly void Dispose()
        {
            _handle?.Dispose();
        }
    }

    #region dereferencing abstraction
    interface IDeref<T>
    {
        T Deref(IntPtr addr);
        void DerefWrite(IntPtr addr, T newValue);
    }
    internal unsafe class IntDeref : IDeref<int>
    {
        public int Deref(IntPtr addr)
        {
            var p = (int*)addr;
            return *p;
        }

        public void DerefWrite(IntPtr addr, int newValue)
        {
            var p = (int*)addr;
            *p = newValue;
        }
    }
    internal class MarshalDeref<T> : IDeref<T>
    {
        public T Deref(IntPtr addr)
        {
            return (T)Marshal.PtrToStructure(addr, typeof(T));
        }

        public void DerefWrite(IntPtr addr, T newValue)
        {
            Marshal.StructureToPtr(newValue, addr, false);
        }
    }
    #endregion    
}
