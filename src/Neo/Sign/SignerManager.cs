// Copyright (C) 2015-2025 The Neo Project.
//
// SignerManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Concurrent;

namespace Neo.Sign;

public static class SignerManager
{
    private static readonly ConcurrentDictionary<string, ISigner> s_signers = new();

    /// <summary>
    /// Get a signer by name. If only one signer is registered, it will return the only one signer.
    /// </summary>
    /// <param name="name">The name of the signer</param>
    /// <returns>
    /// The signer; <see langword="null"/> if not found or no signer or multiple signers are registered.
    /// </returns>
    public static ISigner? GetSignerOrDefault(string name)
    {
        if (!string.IsNullOrEmpty(name) && s_signers.TryGetValue(name, out var signer)) return signer;
        if (s_signers.Count == 1) return s_signers.Values.First();
        return null;
    }

    /// <summary>
    /// Register a signer, and it only can be called before the node starts.
    /// </summary>
    /// <param name="name">The name of the signer</param>
    /// <param name="signer">The signer to register</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="signer"/> is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="name"/> is already registered</exception>
    public static void RegisterSigner(string name, ISigner signer)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty", nameof(name));
        ArgumentNullException.ThrowIfNull(signer);

        if (!s_signers.TryAdd(name, signer)) throw new InvalidOperationException($"Signer {name} already exists");
    }

    /// <summary>
    /// Unregister a signer, and it only can be called before the node starts.
    /// </summary>
    /// <param name="name">The name of the signer</param>
    /// <returns>
    /// <see langword="true"/> if the signer is unregistered; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool UnregisterSigner(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return s_signers.TryRemove(name, out _);
    }
}
