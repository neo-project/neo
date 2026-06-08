// Copyright (C) 2015-2026 The Neo Project.
//
// NodeContact.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Network.P2P;

/// <summary>
/// Represents a reachability hint for a DHT node (NOT a live connection).
/// </summary>
public sealed class NodeContact
{
    const int MaxEndpoints = 4;

    /// <summary>
    /// The verified DHT node identifier.
    /// </summary>
    public UInt256 NodeId { get; }

    /// <summary>
    /// Known overlay endpoints for contacting the node. The first item is the preferred endpoint.
    /// </summary>
    public List<OverlayEndpoint> Endpoints { get; } = new();

    /// <summary>
    /// Last time we successfully communicated with this node (handshake or DHT message).
    /// </summary>
    public DateTime LastSeen { get; internal set; }

    /// <summary>
    /// Consecutive failures when trying to contact this node.
    /// </summary>
    public int FailCount { get; internal set; }

    /// <summary>
    /// Optional capability flags (reserved).
    /// </summary>
    public ulong Features { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the NodeContact class with the specified node identifier, optional endpoints, and
    /// feature flags.
    /// </summary>
    /// <param name="nodeId">The unique identifier for the node. This value is used to distinguish the node within the network.</param>
    /// <param name="endpoints">A collection of overlay endpoints associated with the node. If not specified, the contact will have no initial endpoints.</param>
    /// <param name="features">A bit field representing the features supported by the node. The default is 0, indicating no features.</param>
    public NodeContact(UInt256 nodeId, IEnumerable<OverlayEndpoint>? endpoints = null, ulong features = 0)
    {
        NodeId = nodeId;
        if (endpoints is not null)
            foreach (var ep in endpoints)
                AddOrPromoteEndpoint(ep);
        LastSeen = TimeProvider.Current.UtcNow;
        Features = features;
    }

    internal void AddOrPromoteEndpoint(OverlayEndpoint endpoint)
    {
        // Keep unique endpoints by (Transport, IP, Port); merge kinds when we learn new semantics.
        int index = Endpoints.IndexOf(endpoint);
        if (index >= 0)
        {
            var merged = Endpoints[index].WithKind(Endpoints[index].Kind | endpoint.Kind);
            if (index == 0)
            {
                Endpoints[0] = merged;
                return;
            }
            Endpoints.RemoveAt(index);
            Endpoints.Insert(0, merged);
        }
        else
        {
            Endpoints.Insert(0, endpoint);
            TrimEndpoints();
        }
    }

    public override string ToString()
    {
        return $"{NodeId} ({(Endpoints.Count > 0 ? Endpoints[0].ToString() : "no-endpoint")})";
    }

    void TrimEndpoints()
    {
        while (Endpoints.Count > MaxEndpoints)
        {
            int removeIndex = Endpoints.Count - 1;
            int worstPriority = GetKindPriority(Endpoints[removeIndex].Kind);
            for (int i = Endpoints.Count - 2; i >= 0; i--)
            {
                int priority = GetKindPriority(Endpoints[i].Kind);
                if (priority < worstPriority)
                {
                    worstPriority = priority;
                    removeIndex = i;
                    if (worstPriority == 0) break;
                }
            }
            Endpoints.RemoveAt(removeIndex);
        }
    }

    static int GetKindPriority(EndpointKind kind)
    {
        if (kind.HasFlag(EndpointKind.Advertised)) return 3;
        if (kind.HasFlag(EndpointKind.Observed)) return 2;
        if (kind.HasFlag(EndpointKind.Derived)) return 1;
        return 0;
    }
}
