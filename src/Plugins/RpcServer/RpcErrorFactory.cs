// Copyright (C) 2015-2024 The Neo Project.
//
// RpcErrorFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;

namespace Neo.Plugins.RpcServer
{
    public static class RpcErrorFactory
    {
        public static RpcError WithData(this RpcError error, string data = null)
        {
            return new RpcError(error.Code, error.Message, data);
        }

        public static RpcError NewCustomError(int code, string message, string data = null)
        {
            return new RpcError(code, message, data);
        }

        #region Require data

        public static RpcError MethodNotFound(string method) => RpcError.MethodNotFound.WithData($"The method '{method}' doesn't exists.");
        public static RpcError AlreadyExists(string data) => RpcError.AlreadyExists.WithData(data);
        public static RpcError InvalidParams(string data) => RpcError.InvalidParams.WithData(data);
        public static RpcError BadRequest(string data) => RpcError.BadRequest.WithData(data);
        public static RpcError InsufficientFundsWallet(string data) => RpcError.InsufficientFundsWallet.WithData(data);
        public static RpcError VerificationFailed(string data) => RpcError.VerificationFailed.WithData(data);
        public static RpcError InvalidContractVerification(UInt160 contractHash) => RpcError.InvalidContractVerification.WithData($"The smart contract {contractHash} haven't got verify method.");
        public static RpcError InvalidContractVerification(string data) => RpcError.InvalidContractVerification.WithData(data);
        public static RpcError InvalidSignature(string data) => RpcError.InvalidSignature.WithData(data);
        public static RpcError OracleNotDesignatedNode(ECPoint oraclePub) => RpcError.OracleNotDesignatedNode.WithData($"{oraclePub} isn't an oracle node.");

        #endregion
    }
}
