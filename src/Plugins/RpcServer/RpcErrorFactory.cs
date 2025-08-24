// Copyright (C) 2015-2025 The Neo Project.
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
        public static RpcError WithData(this RpcError error, string data = "")
        {
            return new RpcError(error.Code, error.Message, data);
        }

        public static RpcError NewCustomError(int code, string message, string data = "")
        {
            return new RpcError(code, message, data);
        }

        #region Require data

        /// <summary>
        /// The resource already exists. For example, the transaction is already confirmed, can't be cancelled.
        /// </summary>
        /// <param name="data">The data of the error.</param>
        /// <returns>The RpcError.</returns>
        public static RpcError AlreadyExists(string data) => RpcError.AlreadyExists.WithData(data);

        /// <summary>
        /// The request parameters are invalid. For example, the block hash or index is invalid.
        /// </summary>
        /// <param name="data">The data of the error.</param>
        /// <returns>The RpcError.</returns>
        public static RpcError InvalidParams(string data) => RpcError.InvalidParams.WithData(data);

        /// <summary>
        /// The request is invalid. For example, the request body is invalid.
        /// </summary>
        /// <param name="data">The data of the error.</param>
        /// <returns>The RpcError.</returns>
        public static RpcError BadRequest(string data) => RpcError.BadRequest.WithData(data);

        /// <summary>
        /// The contract verification function is invalid.
        /// For example, the contract doesn't have a verify method with the correct number of input parameters.
        /// </summary>
        /// <param name="contractHash">The hash of the contract.</param>
        /// <param name="pcount">The number of input parameters.</param>
        /// <returns>The RpcError.</returns>
        public static RpcError InvalidContractVerification(UInt160 contractHash, int pcount)
            => RpcError.InvalidContractVerification.WithData($"The smart contract {contractHash} haven't got verify method with {pcount} input parameters.");

        /// <summary>
        /// The contract function to verification is invalid.
        /// For example, the contract doesn't have a verify method with the correct number of input parameters.
        /// </summary>
        /// <param name="data">The data of the error.</param>
        /// <returns>The RpcError.</returns>
        public static RpcError InvalidContractVerification(string data) => RpcError.InvalidContractVerification.WithData(data);

        /// <summary>
        /// The signature is invalid.
        /// </summary>
        /// <param name="data">The data of the error.</param>
        /// <returns>The RpcError.</returns>
        public static RpcError InvalidSignature(string data) => RpcError.InvalidSignature.WithData(data);

        /// <summary>
        /// The oracle is not a designated node.
        /// </summary>
        /// <param name="oraclePub">The public key of the oracle.</param>
        /// <returns>The RpcError.</returns>
        public static RpcError OracleNotDesignatedNode(ECPoint oraclePub)
            => RpcError.OracleNotDesignatedNode.WithData($"{oraclePub} isn't an oracle node.");

        #endregion
    }
}
