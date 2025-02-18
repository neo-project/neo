// Copyright (C) 2015-2025 The Neo Project.
//
// SecureBuffer.Security.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Neo.Build.Core.Security
{
    public partial class SecureBuffer
    {
        private void ProtectMemory()
        {
            Debug.Assert(_buffer != null);
            Debug.Assert(_buffer.IsInvalid == false, "Invalid buffer!");
            Debug.Assert(_key != null);
            Debug.Assert(_key.IsInvalid == false, "Invalid buffer!");

            if (_decryptedLength != 0 && _encrypted == false)
            {
                SafeBuffer? bufferToRelease = null;
                SafeBuffer? keyToRelease = null;

                try
                {
                    var dataSpan = AcquireDataSpan(ref bufferToRelease);
                    var keySpan = AcquireKeySpan(ref keyToRelease);
                    var rawData = dataSpan.ToArray();
                    var keyData = keySpan.ToArray();

                    bufferToRelease?.DangerousRelease();

                    var derivedHalf1 = keyData[..32];
                    var derivedHalf2 = keyData[32..];
                    var encryptedData = Encrypt(XOR(rawData, derivedHalf1), derivedHalf2);

                    EnsureCapacity(encryptedData.Length);
                    dataSpan = AcquireDataSpan(ref bufferToRelease);

                    encryptedData.CopyTo(dataSpan);

                    Array.Clear(keyData, 0, keyData.Length);
                    Array.Clear(derivedHalf1, 0, derivedHalf1.Length);
                    Array.Clear(derivedHalf2, 0, derivedHalf2.Length);
                    Array.Clear(rawData, 0, rawData.Length);
                    Array.Clear(encryptedData, 0, encryptedData.Length);
                }
                finally
                {
                    bufferToRelease?.DangerousRelease();
                    keyToRelease?.DangerousRelease();
                }
            }

            _encrypted = true;
        }

        private void UnprotectMemory()
        {
            Debug.Assert(_buffer != null);
            Debug.Assert(_buffer.IsInvalid == false, "Invalid buffer!");
            Debug.Assert(_key != null);
            Debug.Assert(_key.IsInvalid == false, "Invalid buffer!");

            if (_decryptedLength != 0 && _encrypted)
            {
                SafeBuffer? bufferToRelease = null;
                SafeBuffer? keyToRelease = null;

                try
                {
                    var dataSpan = AcquireDataSpan(ref bufferToRelease);
                    var keySpan = AcquireKeySpan(ref keyToRelease);
                    var rawData = dataSpan.ToArray();
                    var keyData = keySpan.ToArray();

                    bufferToRelease?.DangerousRelease();

                    var derivedHalf1 = keyData[..32];
                    var derivedHalf2 = keyData[32..];
                    var decryptedData = XOR(Decrypt(rawData, derivedHalf2), derivedHalf1);

                    EnsureCapacity(decryptedData.Length);
                    dataSpan = AcquireDataSpan(ref bufferToRelease);

                    decryptedData.CopyTo(dataSpan);

                    Array.Clear(keyData, 0, keyData.Length);
                    Array.Clear(derivedHalf1, 0, derivedHalf1.Length);
                    Array.Clear(derivedHalf2, 0, derivedHalf2.Length);
                    Array.Clear(rawData, 0, rawData.Length);
                    Array.Clear(decryptedData, 0, decryptedData.Length);
                }
                finally
                {
                    bufferToRelease?.DangerousRelease();
                    keyToRelease?.DangerousRelease();
                }
            }

            _encrypted = false;
        }

        private static byte[] Encrypt(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.ISO10126;
            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] Decrypt(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] XOR(byte[] x, byte[] y)
        {
            Debug.Assert(x.Length != y.Length);
            var r = new byte[x.Length];
            for (var i = 0; i < r.Length; i++)
                r[i] = (byte)(x[i] ^ y[i]);
            return r;
        }
    }
}
