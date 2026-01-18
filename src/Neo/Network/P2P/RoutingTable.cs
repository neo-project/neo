// Copyright (C) 2015-2026 The Neo Project.
//
// RoutingTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Net;

namespace Neo.Network.P2P;

/// <summary>
/// Kademlia-style routing table built from 256 k-buckets.
/// </summary>
public sealed class RoutingTable
{
    const int IdBits = UInt256.Length * 8;

    readonly UInt256 _selfId;
    readonly KBucket[] _buckets;

    /// <summary>
    /// Max contacts per bucket (K in Kademlia).
    /// </summary>
    public int BucketSize { get; }

    /// <summary>
    /// Initializes a new instance of the RoutingTable class with the specified node identifier and bucket configuration
    /// parameters.
    /// </summary>
    /// <remarks>The routing table is organized into buckets based on the distance from the local node
    /// identifier. Adjusting bucketSize, replacementSize, or badThreshold can affect the table's resilience and
    /// performance in peer-to-peer network scenarios.</remarks>
    /// <param name="selfId">The unique identifier of the local node for which the routing table is constructed.</param>
    /// <param name="bucketSize">The maximum number of entries allowed in each bucket. Must be positive.</param>
    /// <param name="replacementSize">The maximum number of replacement entries maintained for each bucket. Must be non-negative.</param>
    /// <param name="badThreshold">The number of failed contact attempts after which a node is considered bad and eligible for replacement. Must be
    /// positive.</param>
    public RoutingTable(UInt256 selfId, int bucketSize = 20, int replacementSize = 10, int badThreshold = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bucketSize);
        ArgumentOutOfRangeException.ThrowIfNegative(replacementSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(badThreshold);

        _selfId = selfId;
        BucketSize = bucketSize;

        _buckets = new KBucket[IdBits];
        for (int i = 0; i < _buckets.Length; i++)
            _buckets[i] = new KBucket(bucketSize, replacementSize, badThreshold);
    }

    /// <summary>
    /// Adds or refreshes a contact in the routing table.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node whose contact information is to be updated.</param>
    /// <param name="endpoint">The overlay endpoint associated with the node. Must not be null.</param>
    /// <param name="features">An optional set of feature flags describing the node's capabilities. The default is 0, indicating no features.</param>
    /// <returns>true if the node contact information was updated successfully; otherwise, false.</returns>
    public bool Update(UInt256 nodeId, OverlayEndpoint endpoint, ulong features = 0)
    {
        return Update(new(nodeId, [endpoint], features));
    }

    /// <summary>
    /// Adds or refreshes a contact in the routing table.
    /// </summary>
    /// <remarks>If the specified contact represents the local node, the update is ignored and the method
    /// returns false.</remarks>
    /// <param name="contact">The contact information for the node to update. Must not represent the local node.</param>
    /// <returns>true if the contact was successfully updated; otherwise, false.</returns>
    public bool Update(NodeContact contact)
    {
        int bucket = GetBucketIndex(contact.NodeId);
        if (bucket < 0) return false; // ignore self
        lock (_buckets[bucket])
            return _buckets[bucket].Update(contact);
    }

    /// <summary>
    /// Marks a contact as recently successful (e.g., handshake OK, DHT request succeeded).
    /// </summary>
    public void MarkSuccess(UInt256 nodeId)
    {
        int bucket = GetBucketIndex(nodeId);
        if (bucket < 0) return;
        lock (_buckets[bucket])
            _buckets[bucket].MarkSuccess(nodeId);
    }

    /// <summary>
    /// Marks a contact as failed (e.g., connection timeout). May evict it if it becomes bad.
    /// </summary>
    public void MarkFailure(UInt256 nodeId)
    {
        int bucket = GetBucketIndex(nodeId);
        if (bucket < 0) return;
        lock (_buckets[bucket])
            _buckets[bucket].MarkFailure(nodeId);
    }

    /// <summary>
    /// Removes a contact from the routing table.
    /// </summary>
    public void Remove(UInt256 nodeId)
    {
        int bucket = GetBucketIndex(nodeId);
        if (bucket < 0) return;
        lock (_buckets[bucket])
            _buckets[bucket].Remove(nodeId);
    }

    /// <summary>
    /// Returns up to <paramref name="count"/> contacts closest to <paramref name="targetId"/>.
    /// </summary>
    public IReadOnlyList<NodeContact> FindClosest(UInt256 targetId, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return Array.Empty<NodeContact>();

        // Start from the bucket corresponding to target distance, then expand outward.
        int start = GetBucketIndex(targetId);
        if (start < 0) start = 0;

        var candidates = new List<NodeContact>(Math.Min(count * 3, BucketSize * 8));
        CollectFromBuckets(start, candidates, hardLimit: Math.Max(count * 8, BucketSize * 8));

        // Sort by XOR distance to target.
        candidates.Sort((a, b) => CompareDistance(a.NodeId, b.NodeId, targetId));

        if (candidates.Count <= count) return candidates;
        return candidates.GetRange(0, count);
    }

    /// <summary>
    /// Returns a sample of contacts across buckets (useful for bootstrap / gossip / health checks).
    /// </summary>
    public IReadOnlyList<NodeContact> Sample(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return Array.Empty<NodeContact>();

        var list = new List<NodeContact>(count);
        // Prefer spread: take one from each bucket, round-robin.
        int index = 0;
        while (list.Count < count && index < IdBits)
        {
            lock (_buckets[index])
            {
                var bucket = _buckets[index].Contacts;
                if (bucket.Count > 0)
                {
                    // take most recently seen (tail)
                    list.Add(bucket.Last());
                }
            }
            index++;
        }

        // If still short, just flatten and take.
        if (list.Count < count)
        {
            foreach (var c in EnumerateAllContacts())
            {
                if (list.Count >= count) break;
                if (!list.Contains(c)) list.Add(c);
            }
        }
        return list;
    }

    void CollectFromBuckets(int start, List<NodeContact> output, int hardLimit)
    {
        void AddRange(int bucketIndex)
        {
            lock (_buckets[bucketIndex])
                foreach (var c in _buckets[bucketIndex].Contacts)
                {
                    output.Add(c);
                    if (output.Count >= hardLimit) return;
                }
        }

        AddRange(start);
        if (output.Count >= hardLimit) return;

        for (int step = 1; step < IdBits; step++)
        {
            int left = start - step;
            int right = start + step;

            if (left >= 0) AddRange(left);
            if (output.Count >= hardLimit) break;
            if (right < IdBits) AddRange(right);
            if (output.Count >= hardLimit) break;
            if (left < 0 && right >= IdBits) break;
        }
    }

    IEnumerable<NodeContact> EnumerateAllContacts()
    {
        for (int i = 0; i < IdBits; i++)
            lock (_buckets[i])
                foreach (var c in _buckets[i].Contacts)
                    yield return c;
    }

    int GetBucketIndex(UInt256 nodeId)
    {
        if (nodeId == _selfId) return -1;
        int msb = (_selfId ^ nodeId).MostSignificantBit;
        return msb; // -1..255
    }

    static int CompareDistance(UInt256 a, UInt256 b, UInt256 target)
    {
        var da = a ^ target;
        var db = b ^ target;
        return da.CompareTo(db);
    }
}
