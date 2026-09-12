// Copyright (C) 2015-2026 The Neo Project.
//
// InteropDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Neo.SmartContract
{
    internal delegate object? InteropInvoker(ApplicationEngine engine, object?[] args);

    /// <summary>
    /// Represents a descriptor of an interoperable service.
    /// </summary>
    public record InteropDescriptor
    {
        /// <summary>
        /// The name of the interoperable service.
        /// </summary>
        public required string Name { get; init; }

        private uint _hash;
        /// <summary>
        /// The hash of the interoperable service.
        /// </summary>
        public uint Hash
        {
            get
            {
                if (_hash == 0)
                    _hash = BinaryPrimitives.ReadUInt32LittleEndian(Encoding.ASCII.GetBytes(Name).Sha256());
                return _hash;
            }
        }

        /// <summary>
        /// The <see cref="MethodInfo"/> used to handle the interoperable service.
        /// </summary>
        public required MethodInfo Handler { get; init; }

        /// <summary>
        /// The parameters of the interoperable service.
        /// </summary>
        public IReadOnlyList<InteropParameterDescriptor> Parameters => field ??= Handler.GetParameters().Select(p => new InteropParameterDescriptor(p)).ToList().AsReadOnly();

        internal InteropInvoker Invoker => field ??= CreateInvoker(Handler);

        /// <summary>
        /// The fixed price for calling the interoperable service. It can be 0 if the interoperable service has a variable price.
        /// </summary>
        public long FixedPrice { get; init; }

        /// <summary>
        /// Required Hardfork to be active.
        /// </summary>
        public Hardfork? Hardfork { get; init; }

        /// <summary>
        /// The required <see cref="CallFlags"/> for the interoperable service.
        /// </summary>
        public CallFlags RequiredCallFlags { get; init; }

        public static implicit operator uint(InteropDescriptor descriptor)
        {
            return descriptor.Hash;
        }

        internal object? Invoke(ApplicationEngine engine, object?[] args)
        {
            try
            {
                return Invoker(engine, args);
            }
            catch (Exception ex) when (ex is not TargetInvocationException)
            {
                throw new TargetInvocationException(ex);
            }
        }

        private static InteropInvoker CreateInvoker(MethodInfo handler)
        {
            var engine = Expression.Parameter(typeof(ApplicationEngine), "engine");
            var args = Expression.Parameter(typeof(object[]), "args");
            var handlerParameters = handler.GetParameters();
            var callParameters = new Expression[handlerParameters.Length];

            for (int i = 0; i < handlerParameters.Length; i++)
            {
                callParameters[i] = Expression.Convert(
                    Expression.ArrayIndex(args, Expression.Constant(i)),
                    handlerParameters[i].ParameterType);
            }

            Expression? instance = handler.IsStatic ? null : Expression.Convert(engine, handler.DeclaringType!);
            Expression call = Expression.Call(instance, handler, callParameters);
            Expression body = handler.ReturnType == typeof(void)
                ? Expression.Block(call, Expression.Constant(null, typeof(object)))
                : Expression.Convert(call, typeof(object));

            return Expression.Lambda<InteropInvoker>(body, engine, args).Compile();
        }
    }
}
