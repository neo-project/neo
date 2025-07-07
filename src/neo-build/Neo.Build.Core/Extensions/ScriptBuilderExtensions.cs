// Copyright (C) 2015-2025 The Neo Project.
//
// ScriptBuilderExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Helpers;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Linq.Expressions;

namespace Neo.Build.Core.Extensions
{
    public static class ScriptBuilderExtensions
    {
        public static ScriptBuilder EmitContractCall<T>(this ScriptBuilder builder, Expression<Action<T>> expression)
            where T : class
        {
            var methodCallBody = (MethodCallExpression)expression.Body;
            var methodName = methodCallBody.Method.Name;
            var scriptHash = NeoBuildAttributeHelper.ExtractContractScriptHash(typeof(T));

            if (char.IsLower(methodName[0]) == false)
            {
                methodName = char.ToLower(methodName[0]) + methodName[1..];
            }

            for (var i = methodCallBody.Arguments.Count - 1; i >= 0; i--)
            {
                var argumentValue = Expression.Lambda(methodCallBody.Arguments[i])
                    .Compile()
                    .DynamicInvoke();
                builder.EmitPush(argumentValue);
            }

            builder.EmitPush(methodCallBody.Arguments.Count);
            builder.Emit(OpCode.PACK);
            builder.EmitPush(CallFlags.All);
            builder.EmitPush(methodName);
            builder.EmitPush(scriptHash);
            builder.EmitSysCall(ApplicationEngine.System_Contract_Call);

            return builder;
        }
    }
}
