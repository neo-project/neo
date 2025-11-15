// Copyright (C) 2015-2025 The Neo Project.
//
// TransactionAttributesBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;

namespace Neo.Builders;

public sealed class TransactionAttributesBuilder
{
    private TransactionAttribute[] _attributes = [];

    private TransactionAttributesBuilder() { }

    public static TransactionAttributesBuilder CreateEmpty()
    {
        return new TransactionAttributesBuilder();
    }

    public TransactionAttributesBuilder AddConflict(UInt256 hash)
    {
        var conflicts = new Conflicts { Hash = hash };
        _attributes = [.. _attributes, conflicts];
        return this;
    }

    public TransactionAttributesBuilder AddOracleResponse(Action<OracleResponse> config)
    {
        var oracleResponse = new OracleResponse();
        config(oracleResponse);
        _attributes = [.. _attributes, oracleResponse];
        return this;
    }

    public TransactionAttributesBuilder AddHighPriority()
    {
        if (_attributes.Any(a => a is HighPriorityAttribute))
            throw new InvalidOperationException("HighPriority attribute already exists in the transaction attributes. Only one HighPriority attribute is allowed per transaction.");

        var highPriority = new HighPriorityAttribute();
        _attributes = [.. _attributes, highPriority];
        return this;
    }

    public TransactionAttributesBuilder AddNotValidBefore(uint block)
    {
        if (_attributes.Any(a => a is NotValidBefore b && b.Height == block))
            throw new InvalidOperationException($"NotValidBefore attribute for block {block} already exists in the transaction attributes. Each block height can only be specified once.");

        var validUntilBlock = new NotValidBefore()
        {
            Height = block
        };

        _attributes = [.. _attributes, validUntilBlock];
        return this;
    }

    public TransactionAttribute[] Build()
    {
        return _attributes;
    }
}
