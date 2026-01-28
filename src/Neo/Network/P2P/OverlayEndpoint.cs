// Copyright (C) 2015-2026 The Neo Project.
//
// OverlayEndpoint.cs file belongs to the neo project and is free
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
/// An overlay-network endpoint with transport and discovery semantics.
/// Equality intentionally ignores <see cref="Kind"/> so that the same transport+ip+port
/// can accumulate multiple kinds (e.g., Observed | Advertised).
/// </summary>
public readonly struct OverlayEndpoint : IEquatable<OverlayEndpoint>
{
    /// <summary>
    /// The transport protocol used to communicate with this endpoint.
    /// </summary>
    public TransportProtocol Transport { get; }

    /// <summary>
    /// The IP endpoint of this overlay endpoint.
    /// </summary>
    public IPEndPoint EndPoint { get; }

    /// <summary>
    /// The kind of this overlay endpoint.
    /// </summary>
    public EndpointKind Kind { get; }

    /// <summary>
    /// Initializes a new instance of the OverlayEndpoint class with the specified transport protocol, network endpoint,
    /// and endpoint kind.
    /// </summary>
    /// <param name="transport">The transport protocol to use for the overlay endpoint.</param>
    /// <param name="endPoint">The network endpoint associated with this overlay endpoint.</param>
    /// <param name="kind">The kind of endpoint represented by this instance.</param>
    public OverlayEndpoint(TransportProtocol transport, IPEndPoint endPoint, EndpointKind kind)
    {
        Transport = transport;
        EndPoint = endPoint;
        Kind = kind;
    }

    /// <summary>
    /// Returns a new OverlayEndpoint instance with the specified endpoint kind, preserving the existing transport and
    /// endpoint values.
    /// </summary>
    /// <param name="kind">The endpoint kind to associate with the new OverlayEndpoint instance.</param>
    /// <returns>A new OverlayEndpoint instance with the specified kind. The transport and endpoint values are copied from the
    /// current instance.</returns>
    public OverlayEndpoint WithKind(EndpointKind kind) => new(Transport, EndPoint, kind);

    /// <summary>
    /// Determines whether the current instance and the specified <see cref="OverlayEndpoint"/> are equal.
    /// </summary>
    /// <remarks>This method compares the Transport and EndPoint properties for equality. The Kind property is
    /// not considered in the comparison.</remarks>
    /// <param name="other">The <see cref="OverlayEndpoint"/> to compare with the current instance.</param>
    /// <returns><see langword="true"/> if the current instance and <paramref name="other"/> represent the same transport and
    /// endpoint; otherwise, <see langword="false"/>.</returns>
    public bool Equals(OverlayEndpoint other)
    {
        // NOTE: ignore Kind on purpose
        return Transport == other.Transport && EndPoint.Equals(other.EndPoint);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current OverlayEndpoint instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current OverlayEndpoint instance.</param>
    /// <returns>true if the specified object is an OverlayEndpoint and is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is OverlayEndpoint other && Equals(other);
    }

    public override int GetHashCode()
    {
        // NOTE: ignore Kind on purpose
        return HashCode.Combine(Transport, EndPoint);
    }

    public override string ToString()
    {
        return $"{Transport.ToString().ToLowerInvariant()}:{EndPoint}";
    }

    public static bool operator ==(OverlayEndpoint left, OverlayEndpoint right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(OverlayEndpoint left, OverlayEndpoint right)
    {
        return !(left == right);
    }
}
