// Copyright (C) 2015-2025 The Neo Project.
//
// StorageEntry.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.CompilerServices;

namespace Neo.IO.Caching
{
    internal class StorageEntry<TKey, TValue>(
        TKey key,
        StorageCache<TKey, TValue> cache,
        TimeSpan? ttl = null) : IStorageEntry<TKey, TValue>
        where TKey : class, IKeySerializable
        where TValue : class, ISerializable, new()
    {
        private const int NotSet = -1;

        internal static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromSeconds(30);

        public TKey Key { get; } = key ?? throw new ArgumentNullException(nameof(key));

        public TValue Value
        {
            get => _value;
            set => _value = value;
        }

        TimeSpan? IStorageEntry<TKey, TValue>.AbsoluteExpirationRelativeToNow
        {
            get => _absoluteExpirationRelativeToNow.Ticks == 0 ? null : _absoluteExpirationRelativeToNow;
            set
            {
                if (value is { Ticks: <= 0 })
                    throw new ArgumentOutOfRangeException(nameof(AbsoluteExpirationRelativeToNow), value, "The relative expiration value must be positive.");

                _absoluteExpirationRelativeToNow = value.GetValueOrDefault();
            }
        }

        DateTimeOffset? IStorageEntry<TKey, TValue>.AbsoluteExpiration
        {
            get
            {
                if (_absoluteExpirationTicks < 0)
                    return null;

                var offset = new TimeSpan(_absoluteExpirationOffsetMinutes * TimeSpan.TicksPerMinute);
                return new DateTimeOffset(_absoluteExpirationTicks + offset.Ticks, offset);
            }
            set
            {
                if (value is null)
                {
                    _absoluteExpirationTicks = NotSet;
                    _absoluteExpirationOffsetMinutes = default;
                }
                else
                {
                    var expiration = value.GetValueOrDefault();
                    _absoluteExpirationTicks = expiration.UtcTicks;
                    _absoluteExpirationOffsetMinutes = (short)(expiration.Offset.Ticks / TimeSpan.TicksPerMinute);
                }
            }
        }

        internal long AbsoluteExpirationTicks
        {
            get => _absoluteExpirationTicks;
            set
            {
                _absoluteExpirationTicks = value;
                _absoluteExpirationOffsetMinutes = 0;
            }
        }

        internal DateTime LastAccessed { get; set; }
        internal TimeSpan AbsoluteExpirationRelativeToNow => _absoluteExpirationRelativeToNow;

        internal CacheEvictionReason EvictionReason
        {
            get => _evictionReason;
            private set => _evictionReason = value;
        }

        private readonly StorageCache<TKey, TValue> _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        private TValue _value;

        private CacheEvictionReason _evictionReason;
        private TimeSpan _absoluteExpirationRelativeToNow = ttl ?? DefaultTimeToLive;
        private long _absoluteExpirationTicks = NotSet;
        private short _absoluteExpirationOffsetMinutes;

        private bool _isDisposed;
        private bool _isExpired;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed == false)
            {
                _isDisposed = true;
                _cache.Remove(Key);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // added based on profiling
        internal bool CheckExpired(DateTime utcNow)
            => _isExpired ||
                CheckForExpiredTime(utcNow);

        internal void SetExpired(CacheEvictionReason reason)
        {
            if (EvictionReason == CacheEvictionReason.None)
            {
                EvictionReason = reason;
            }
            _isExpired = true;
        }

        internal void SetTimeout(TimeSpan? expires)
        {
            if (expires is { Ticks: <= 0 })
                throw new ArgumentOutOfRangeException(nameof(expires), expires, "The relative expiration value must be positive.");
            _absoluteExpirationRelativeToNow = expires.GetValueOrDefault();
        }

        internal void SetExpirationTimeRelativeTo(DateTime utcNow)
        {
            if (_absoluteExpirationRelativeToNow.Ticks > 0)
            {
                var absoluteExpiration = (utcNow + _absoluteExpirationRelativeToNow).Ticks;

                if ((ulong)absoluteExpiration < (ulong)_absoluteExpirationTicks)
                    _absoluteExpirationTicks = absoluteExpiration;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // added based on profiling
        private bool CheckForExpiredTime(DateTime utcNow)
        {
            if (_absoluteExpirationTicks < 0)
                return false;

            return FullCheck(utcNow);

            bool FullCheck(DateTime utcNow)
            {
                if ((ulong)_absoluteExpirationTicks <= (ulong)utcNow.Ticks)
                {
                    SetExpired(CacheEvictionReason.Expired);
                    return true;
                }

                return false;
            }
        }
    }
}
