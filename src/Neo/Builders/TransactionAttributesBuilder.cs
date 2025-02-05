// Copyright (C) 2015-2025 The Neo Project.
//
// TransactionAttributesBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using System;
using System.Linq;

namespace Neo.Builders
{
    public sealed class TransactionAttributesBuilder
    {
        private TransactionAttribute[] _attributes = [];

        private TransactionAttributesBuilder() { }

        public static TransactionAttributesBuilder CreateEmpty()
        {
            return new TransactionAttributesBuilder();
        }

        public TransactionAttributesBuilder AddConflict(Action<Conflicts> config)
        {
            var conflicts = new Conflicts();
            config(conflicts);
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
                throw new InvalidOperationException("HighPriority already exists in the attributes.");

            var highPriority = new HighPriorityAttribute();
            _attributes = [.. _attributes, highPriority];
            return this;
        }

        public TransactionAttributesBuilder AddNotValidBefore(uint block)
        {
            if (_attributes.Any(a => a is NotValidBefore b && b.Height == block))
                throw new InvalidOperationException($"Block {block} already exists in the attributes.");

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
}
