// Copyright (C) 2015-2025 The Neo Project.
//
// AccountHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System.Numerics;

namespace Neo.Build.Core.Helpers
{
    public static class AccountHelper
    {
        public static byte[] CreateBalanceScript(UInt160 contractHash, UInt160 accountHash)
        {
            using var sb = new ScriptBuilder()
                .EmitDynamicCall(contractHash, "balanceOf", accountHash)
                .EmitDynamicCall(contractHash, "symbol")
                .EmitDynamicCall(contractHash, "decimals");
            return sb.ToArray();
        }

        public static (BigInteger balance, string symbol, BigInteger decimals) GetBalance(DataCache snapshot, UInt160 contractHash, UInt160 accountHash)
        {
            var script = CreateBalanceScript(contractHash, accountHash);

            using var app = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            app.LoadScript(script);

            var appResult = app.Execute();

            if (appResult != VMState.HALT)
                // TODO: add exception class for exception
                throw new NeoBuildException("VM Fault", NeoBuildErrorCodes.General.InternalException);

            var appStackResults = app.ResultStack;

            if (appStackResults.Count != 3)
                // TODO: add exception class for exception
                throw new NeoBuildException("VM Fault", NeoBuildErrorCodes.General.InternalException);

            var decimals = appStackResults.Pop().GetInteger();
            var symbol = appStackResults.Pop().GetString();
            var balance = appStackResults.Pop().GetInteger();

            return (balance, symbol!, decimals);
        }
    }
}
