// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineBase.SystemCrypto.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Build.Core.Logging;
using Neo.Extensions;
using System.Linq;

namespace Neo.Build.Core.SmartContract
{
    public partial class ApplicationEngineBase
    {
        protected virtual bool SystemCryptoCheckSig(byte[] publicKey, byte[] signature)
        {
            var result = CheckSig(publicKey, signature);

            _traceLogger.LogDebug(DebugEventLog.Call,
                "{SysCall} Key=\"0x{Key}\", Signature=\"0x{Signature}\", Result=\"{Result}\"",
                nameof(System_Crypto_CheckSig), publicKey.ToHexString(), signature.ToHexString(), result);

            return result;
        }

        protected virtual bool SystemCryptoCheckMultisig(byte[][] publicKeys, byte[][] signatures)
        {
            var publicKeyStrings = publicKeys.Select(s => "0x" + s.ToHexString());
            var signatureStrings = signatures.Select(s => "0x" + s.ToHexString());
            var result = CheckMultisig(publicKeys, signatures);

            _traceLogger.LogDebug(DebugEventLog.Call,
                "{SysCall} Keys=\"[{Keys}]\" Signatures=\"[{Signatures}]\" Result=\"{Result}\"",
                nameof(System_Crypto_CheckMultisig), string.Join(',', publicKeyStrings), string.Join(',', signatureStrings), result);

            return result;
        }
    }
}
