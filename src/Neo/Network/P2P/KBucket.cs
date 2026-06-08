// Copyright (C) 2015-2026 The Neo Project.
//
// KBucket.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Diagnostics.CodeAnalysis;

namespace Neo.Network.P2P;

/// <summary>
/// A Kademlia-style k-bucket: stores up to <see cref="Capacity"/> contacts in LRU order.
/// </summary>
sealed class KBucket
{
    private readonly LinkedList<NodeContact> _lru = new();
    private readonly Dictionary<UInt256, LinkedListNode<NodeContact>> _index = new();

    // Replacement cache: best-effort candidates when the bucket is full.
    private readonly LinkedList<NodeContact> _replacements = new();
    private readonly Dictionary<UInt256, LinkedListNode<NodeContact>> _repIndex = new();

    public int Capacity { get; }
    public int ReplacementCapacity { get; }
    public int BadThreshold { get; }
    public int Count => _lru.Count;
    public IReadOnlyCollection<NodeContact> Contacts => _lru;

    public KBucket(int capacity, int replacementCapacity, int badThreshold)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegative(replacementCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(badThreshold);
        Capacity = capacity;
        ReplacementCapacity = replacementCapacity;
        BadThreshold = badThreshold;
    }

    public bool TryGet(UInt256 nodeId, [NotNullWhen(true)] out NodeContact? contact)
    {
        if (_index.TryGetValue(nodeId, out var node))
        {
            contact = node.Value;
            return true;
        }
        contact = null;
        return false;
    }

    /// <summary>
    /// Updates LRU position and contact metadata. If bucket is full and the node is new,
    /// the node is placed into replacement cache.
    /// </summary>
    /// <returns>
    /// True if the contact ended up in the main bucket; false if it was cached as a replacement.
    /// </returns>
    public bool Update(NodeContact incoming)
    {
        if (_index.TryGetValue(incoming.NodeId, out var existingNode))
        {
            Merge(existingNode.Value, incoming);
            Touch(existingNode);
            return true;
        }

        if (_lru.Count < Capacity)
        {
            var node = _lru.AddLast(incoming);
            _index[incoming.NodeId] = node;
            return true;
        }

        // Bucket full: keep as replacement candidate.
        AddOrUpdateReplacement(incoming);
        return false;
    }

    public void MarkSuccess(UInt256 nodeId)
    {
        if (_index.TryGetValue(nodeId, out var node))
        {
            node.Value.FailCount = 0;
            node.Value.LastSeen = TimeProvider.Current.UtcNow;
            Touch(node);
            return;
        }

        // If it was only a replacement, promote its freshness.
        if (_repIndex.TryGetValue(nodeId, out var repNode))
        {
            repNode.Value.FailCount = 0;
            repNode.Value.LastSeen = TimeProvider.Current.UtcNow;
            Touch(repNode);
        }
    }

    public void MarkFailure(UInt256 nodeId)
    {
        if (_index.TryGetValue(nodeId, out var node))
        {
            node.Value.FailCount++;
            if (node.Value.FailCount < BadThreshold) return;

            // Evict bad node and promote best replacement (if any).
            RemoveFrom(node, _index);
            PromoteReplacementIfAny();
        }
        else if (_repIndex.TryGetValue(nodeId, out var repNode))
        {
            // If it is a replacement, decay it and possibly drop.
            repNode.Value.FailCount++;
            if (repNode.Value.FailCount >= BadThreshold)
                RemoveFrom(repNode, _repIndex);
        }
    }

    public void Remove(UInt256 nodeId)
    {
        if (_index.TryGetValue(nodeId, out var node))
        {
            RemoveFrom(node, _index);
            PromoteReplacementIfAny();
        }
        else if (_repIndex.TryGetValue(nodeId, out var repNode))
        {
            RemoveFrom(repNode, _repIndex);
        }
    }

    void AddOrUpdateReplacement(NodeContact incoming)
    {
        if (_repIndex.TryGetValue(incoming.NodeId, out var existing))
        {
            Merge(existing.Value, incoming);
            Touch(existing);
            return;
        }

        if (ReplacementCapacity == 0) return;

        var node = _replacements.AddLast(incoming);
        _repIndex[incoming.NodeId] = node;

        if (_replacements.Count > ReplacementCapacity)
        {
            // Drop oldest replacement.
            var first = _replacements.First;
            if (first is not null)
                RemoveFrom(first, _repIndex);
        }
    }

    void PromoteReplacementIfAny()
    {
        if (_lru.Count >= Capacity) return;
        if (_replacements.Last is null) return;

        // Promote the most recently seen replacement.
        var rep = _replacements.Last;
        RemoveFrom(rep, _repIndex);
        var main = _lru.AddLast(rep.Value);
        _index[main.Value.NodeId] = main;
    }

    static void Merge(NodeContact dst, NodeContact src)
    {
        // Merge overlay endpoints (preserve transport; merge endpoint kinds).
        for (int i = 0; i < src.Endpoints.Count; i++)
            dst.AddOrPromoteEndpoint(src.Endpoints[i]);

        // Prefer latest seen & features.
        if (src.LastSeen > dst.LastSeen) dst.LastSeen = src.LastSeen;
        dst.Features |= src.Features;
    }

    static void Touch(LinkedListNode<NodeContact> node)
    {
        var list = node.List!;
        list.Remove(node);
        list.AddLast(node);
    }

    static void RemoveFrom(LinkedListNode<NodeContact> node, Dictionary<UInt256, LinkedListNode<NodeContact>> index)
    {
        index.Remove(node.Value.NodeId);
        node.List!.Remove(node);
    }
}
