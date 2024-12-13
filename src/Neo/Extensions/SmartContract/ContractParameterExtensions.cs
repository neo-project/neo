// Copyright (C) 2015-2024 The Neo Project.
//
// ContractParameterExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.Extensions
{
    public static class ContractParameterExtensions
    {
        /// <summary>
        /// Converts the <see cref="ContractParameter"/> to a <see cref="StackItem"/>.
        /// </summary>
        /// <param name="parameter">The <see cref="ContractParameter"/> to convert.</param>
        /// <returns>The converted <see cref="StackItem"/>.</returns>
        public static StackItem ToStackItem(this ContractParameter parameter)
        {
            return ToStackItem(parameter, null);
        }

        private static StackItem ToStackItem(ContractParameter parameter, List<(StackItem, ContractParameter)> context)
        {
            if (parameter is null) throw new ArgumentNullException(nameof(parameter));
            if (parameter.Value is null) return StackItem.Null;
            StackItem stackItem = null;
            switch (parameter.Type)
            {
                case ContractParameterType.Array:
                    if (context is null)
                        context = [];
                    else
                        (stackItem, _) = context.FirstOrDefault(p => ReferenceEquals(p.Item2, parameter));
                    if (stackItem is null)
                    {
                        stackItem = new Array(((IList<ContractParameter>)parameter.Value).Select(p => ToStackItem(p, context)));
                        context.Add((stackItem, parameter));
                    }
                    break;
                case ContractParameterType.Map:
                    if (context is null)
                        context = [];
                    else
                        (stackItem, _) = context.FirstOrDefault(p => ReferenceEquals(p.Item2, parameter));
                    if (stackItem is null)
                    {
                        Map map = new();
                        foreach (var pair in (IList<KeyValuePair<ContractParameter, ContractParameter>>)parameter.Value)
                            map[(PrimitiveType)ToStackItem(pair.Key, context)] = ToStackItem(pair.Value, context);
                        stackItem = map;
                        context.Add((stackItem, parameter));
                    }
                    break;
                case ContractParameterType.Boolean:
                    stackItem = (bool)parameter.Value;
                    break;
                case ContractParameterType.ByteArray:
                case ContractParameterType.Signature:
                    stackItem = (byte[])parameter.Value;
                    break;
                case ContractParameterType.Integer:
                    stackItem = (BigInteger)parameter.Value;
                    break;
                case ContractParameterType.Hash160:
                    stackItem = ((UInt160)parameter.Value).ToArray();
                    break;
                case ContractParameterType.Hash256:
                    stackItem = ((UInt256)parameter.Value).ToArray();
                    break;
                case ContractParameterType.PublicKey:
                    stackItem = ((ECPoint)parameter.Value).EncodePoint(true);
                    break;
                case ContractParameterType.String:
                    stackItem = (string)parameter.Value;
                    break;
                default:
                    throw new ArgumentException($"ContractParameterType({parameter.Type}) is not supported to StackItem.");
            }
            return stackItem;
        }
    }
}
