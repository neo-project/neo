// Copyright (C) 2015-2025 The Neo Project.
//
// SecureBuffer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace Neo.Build.Core.Security
{
    public partial class SecureBuffer : IDisposable
    {
        private const int MaxLength = 65536;
        private const int MaxKeySize = 64;

        public int Length
        {
            get
            {
                EnsureNotDisposed();
                return Volatile.Read(ref _decryptedLength);
            }
        }

        private readonly object _lock = new();

        private UnmanagedBuffer? _buffer;
        private UnmanagedBuffer? _key;

        private int _decryptedLength;
        private bool _encrypted;
        private bool _readOnly;

        public SecureBuffer(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (length > MaxLength)
                throw new ArgumentOutOfRangeException(nameof(length));

            _decryptedLength = length;

            Initialize();
        }

        private SecureBuffer(SecureBuffer buff)
        {
            Debug.Assert(buff._buffer != null, "Expected other buffer to be non-null");
            Debug.Assert(buff._key != null, "Expected other buffer to be non-null");
            Debug.Assert(buff._encrypted, "Expected to be used only on encrypted buffer");

            _buffer = UnmanagedBuffer.Allocate((int)buff._buffer.ByteLength);
            Debug.Assert(_buffer != null);
            UnmanagedBuffer.Copy(buff._buffer, _buffer, buff._buffer.ByteLength);

            _key = UnmanagedBuffer.Allocate((int)buff._key.ByteLength);
            Debug.Assert(_key != null);
            UnmanagedBuffer.Copy(buff._key, _key, buff._buffer.ByteLength);

            _decryptedLength = buff._decryptedLength;
            _encrypted = buff._encrypted;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_buffer != null)
                {
                    _buffer.Dispose();
                    _buffer = null;
                }

                if (_key != null)
                {
                    _key.Dispose();
                    _key = null;
                }
            }
            GC.SuppressFinalize(this);
        }

        public bool IsReadOnly()
        {
            EnsureNotDisposed();
            return Volatile.Read(ref _readOnly);
        }

        public void MakeReadOnly()
        {
            EnsureNotDisposed();
            Volatile.Write(ref _readOnly, true);
        }

        public SecureBuffer DeepCopy()
        {
            lock (_lock)
            {
                EnsureNotDisposed();
                return new(this);
            }
        }

        public void Append(ReadOnlySpan<byte> value)
        {
            lock (_lock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();

                Debug.Assert(_buffer != null);

                SafeBuffer? bufferToRelease = null;

                try
                {
                    UnprotectMemory();

                    EnsureCapacity(_decryptedLength + value.Length);

                    var span = AcquireDataSpan(ref bufferToRelease);
                    value.CopyTo(span[_decryptedLength..]);
                    _decryptedLength += value.Length;
                }
                finally
                {
                    ProtectMemory();
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        public void InsertAt(int index, byte value)
        {
            lock (_lock)
            {
                if (index < 0 || index > _decryptedLength)
                    throw new ArgumentOutOfRangeException(nameof(index));

                EnsureNotDisposed();
                EnsureNotReadOnly();

                Debug.Assert(_buffer != null);

                SafeBuffer? bufferToRelease = null;

                try
                {
                    UnprotectMemory();

                    EnsureCapacity(_decryptedLength + 1);

                    var dataSpan = AcquireDataSpan(ref bufferToRelease);
                    dataSpan.Slice(index, _decryptedLength - index).CopyTo(dataSpan.Slice(index + 1));
                    dataSpan[index] = value;
                    _decryptedLength++;
                }
                finally
                {
                    ProtectMemory();
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                if (index < 0 || index >= _decryptedLength)
                    throw new ArgumentOutOfRangeException(nameof(index));

                EnsureNotDisposed();
                EnsureNotReadOnly();

                Debug.Assert(_buffer != null);

                SafeBuffer? bufferToRelease = null;

                try
                {
                    UnprotectMemory();

                    var dataSpan = AcquireDataSpan(ref bufferToRelease);
                    dataSpan.Slice(index + 1, _decryptedLength - (index + 1)).CopyTo(dataSpan.Slice(index));
                    _decryptedLength--;
                }
                finally
                {
                    ProtectMemory();
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        public void SetAt(int index, byte value)
        {
            lock (_lock)
            {
                if (index < 0 || index >= _decryptedLength)
                    throw new ArgumentOutOfRangeException(nameof(index));

                EnsureNotDisposed();
                EnsureNotReadOnly();

                Debug.Assert(_buffer != null);

                SafeBuffer? bufferToRelease = null;

                try
                {
                    UnprotectMemory();

                    var dataSpan = AcquireDataSpan(ref bufferToRelease);
                    dataSpan[index] = value;
                }
                finally
                {
                    ProtectMemory();
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                EnsureNotDisposed();
                EnsureNotReadOnly();

                _decryptedLength = 0;

                SafeBuffer? bufferToRelease = null;

                try
                {
                    var dataSpan = AcquireDataSpan(ref bufferToRelease);
                    dataSpan.Clear();
                }
                finally
                {
                    bufferToRelease?.DangerousRelease();
                }
            }
        }

        private void Initialize()
        {
            _buffer = UnmanagedBuffer.Allocate(_decryptedLength);
            _key = UnmanagedBuffer.Allocate(MaxKeySize);

            SafeBuffer? bufferToRelease = null;
            SafeBuffer? keyToRelease = null;

            try
            {
                var dataSpan = AcquireDataSpan(ref bufferToRelease);
                var keySpan = AcquireKeySpan(ref keyToRelease);

                var passPhaseBytes = new byte[32];
                RandomNumberGenerator.Fill(passPhaseBytes);

                var saltBytes = passPhaseBytes.Sha256().Sha256()[..4];
                var derivedKey = SCrypt.Generate(passPhaseBytes, saltBytes, MaxLength, 8, 8, MaxKeySize);
                derivedKey.CopyTo(keySpan);

                Array.Clear(passPhaseBytes, 0, passPhaseBytes.Length);
                Array.Clear(derivedKey, 0, derivedKey.Length);
            }
            finally
            {
                ProtectMemory();
                bufferToRelease?.DangerousRelease();
                keyToRelease?.DangerousRelease();
            }
        }

        private unsafe Span<byte> AcquireDataSpan(ref SafeBuffer? bufferToRelease)
        {
            SafeBuffer buffer = _buffer!;

            var ignore = false;
            buffer.DangerousAddRef(ref ignore);

            bufferToRelease = buffer;

            return new Span<byte>((byte*)buffer.DangerousGetHandle(), (int)(buffer.ByteLength));
        }

        private unsafe Span<byte> AcquireKeySpan(ref SafeBuffer? bufferToRelease)
        {
            SafeBuffer buffer = _key!;

            var ignore = false;
            buffer.DangerousAddRef(ref ignore);

            bufferToRelease = buffer;

            return new Span<byte>((byte*)buffer.DangerousGetHandle(), (int)(buffer.ByteLength));
        }

        private void EnsureNotReadOnly()
        {
            if (_readOnly)
            {
                throw new InvalidOperationException();
            }
        }

        private void EnsureNotDisposed()
        {
            if (_buffer == null)
                throw new ObjectDisposedException(nameof(SecureBuffer));
        }

        private void EnsureCapacity(int capacity)
        {
            if (capacity > MaxLength)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            Debug.Assert(_buffer != null);

            if ((uint)capacity <= _buffer.ByteLength)
                return;

            var oldBuffer = _buffer;
            var newBuffer = UnmanagedBuffer.Allocate(capacity);
            UnmanagedBuffer.Copy(oldBuffer, newBuffer, (uint)_decryptedLength);
            _buffer = newBuffer;
            oldBuffer.Dispose();
        }

        internal unsafe IntPtr MarshalToByteArray(bool globalAlloc, bool decrypt = true)
        {
            lock (_lock)
            {
                EnsureNotDisposed();

                if (decrypt)
                    UnprotectMemory();

                SafeBuffer? bufferToRelease = null;
                var ptr = IntPtr.Zero;
                var byteLength = 0;

                try
                {
                    var dataSpan = AcquireDataSpan(ref bufferToRelease).Slice(0, _decryptedLength);

                    byteLength = dataSpan.Length;

                    if (globalAlloc)
                    {
                        ptr = Marshal.AllocHGlobal(byteLength);
                    }
                    else
                    {
                        ptr = Marshal.AllocCoTaskMem(byteLength);
                    }

                    var resultSpan = new Span<byte>((void*)ptr, byteLength);
                    dataSpan.CopyTo(resultSpan);

                    var result = ptr;
                    ptr = IntPtr.Zero;
                    return result;
                }
                finally
                {
                    // If we failed for any reason, free the new buffer
                    if (ptr != IntPtr.Zero)
                    {
                        new Span<byte>((void*)ptr, byteLength).Clear();

                        if (globalAlloc)
                        {
                            Marshal.FreeHGlobal(ptr);
                        }
                        else
                        {
                            Marshal.FreeCoTaskMem(ptr);
                        }
                    }

                    if (decrypt)
                        ProtectMemory();

                    bufferToRelease?.DangerousRelease();
                }
            }
        }
    }
}
