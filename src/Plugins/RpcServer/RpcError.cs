// Copyright (C) 2015-2024 The Neo Project.
//
// RpcError.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;

namespace Neo.Plugins.RpcServer
{
    public class RpcError
    {
        #region Default Values

        // https://www.jsonrpc.org/specification
        // | code               | message         | meaning                                                                           |
        // |--------------------|-----------------|-----------------------------------------------------------------------------------|
        // | -32700             | Parse error     | Invalid JSON was received by the server. An error occurred on the server while parsing the JSON text. |
        // | -32600             | Invalid request | The JSON sent is not a valid Request object.                                      |
        // | -32601             | Method not found| The method does not exist / is not available.                                     |
        // | -32602             | Invalid params  | Invalid method parameter(s).                                                      |
        // | -32603             | Internal error  | Internal JSON-RPC error.                                                          |
        // | -32000 to -32099   | Server error    | Reserved for implementation-defined server-errors.                                |
        public static readonly RpcError InvalidRequest = new(-32600, "Invalid request");
        public static readonly RpcError MethodNotFound = new(-32601, "Method not found");
        public static readonly RpcError InvalidParams = new(-32602, "Invalid params");
        public static readonly RpcError InternalServerError = new(-32603, "Internal server RpcError");
        public static readonly RpcError BadRequest = new(-32700, "Bad request");

        // https://github.com/neo-project/proposals/pull/156/files
        public static readonly RpcError UnknownBlock = new(-101, "Unknown block");
        public static readonly RpcError UnknownContract = new(-102, "Unknown contract");
        public static readonly RpcError UnknownTransaction = new(-103, "Unknown transaction");
        public static readonly RpcError UnknownStorageItem = new(-104, "Unknown storage item");
        public static readonly RpcError UnknownScriptContainer = new(-105, "Unknown script container");
        public static readonly RpcError UnknownStateRoot = new(-106, "Unknown state root");
        public static readonly RpcError UnknownSession = new(-107, "Unknown session");
        public static readonly RpcError UnknownIterator = new(-108, "Unknown iterator");
        public static readonly RpcError UnknownHeight = new(-109, "Unknown height");

        public static readonly RpcError InsufficientFundsWallet = new(-300, "Insufficient funds in wallet");
        public static readonly RpcError WalletFeeLimit = new(-301, "Wallet fee limit exceeded", "The necessary fee is more than the Max_fee, this transaction is failed. Please increase your Max_fee value.");
        public static readonly RpcError NoOpenedWallet = new(-302, "No opened wallet");
        public static readonly RpcError WalletNotFound = new(-303, "Wallet not found");
        public static readonly RpcError WalletNotSupported = new(-304, "Wallet not supported");

        public static readonly RpcError VerificationFailed = new(-500, "Inventory verification failed");
        public static readonly RpcError AlreadyExists = new(-501, "Inventory already exists");
        public static readonly RpcError MempoolCapReached = new(-502, "Memory pool capacity reached");
        public static readonly RpcError AlreadyInPool = new(-503, "Already in pool");
        public static readonly RpcError InsufficientNetworkFee = new(-504, "Insufficient network fee");
        public static readonly RpcError PolicyFailed = new(-505, "Policy check failed");
        public static readonly RpcError InvalidScript = new(-509, "Invalid transaction script");
        public static readonly RpcError InvalidAttribute = new(-507, "Invalid transaction attribute");
        public static readonly RpcError InvalidSignature = new(-508, "Invalid signature");
        public static readonly RpcError InvalidSize = new(-509, "Invalid inventory size");
        public static readonly RpcError ExpiredTransaction = new(-510, "Expired transaction");
        public static readonly RpcError InsufficientFunds = new(-511, "Insufficient funds for fee");
        public static readonly RpcError InvalidContractVerification = new(-512, "Invalid contract verification function");

        public static readonly RpcError AccessDenied = new(-600, "Access denied");
        public static readonly RpcError SessionsDisabled = new(-601, "State iterator sessions disabled");
        public static readonly RpcError OracleDisabled = new(-602, "Oracle service disabled");
        public static readonly RpcError OracleRequestFinished = new(-603, "Oracle request already finished");
        public static readonly RpcError OracleRequestNotFound = new(-604, "Oracle request not found");
        public static readonly RpcError OracleNotDesignatedNode = new(-605, "Not a designated oracle node");
        public static readonly RpcError UnsupportedState = new(-606, "Old state not supported");
        public static readonly RpcError InvalidProof = new(-607, "Invalid state proof");
        public static readonly RpcError ExecutionFailed = new(-608, "Contract execution failed");

        #endregion

        public int Code { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }

        public RpcError(int code, string message, string data = null)
        {
            Code = code;
            Message = message;
            Data = data;
        }

        public override string ToString() => string.IsNullOrEmpty(Data) ? $"{Message} ({Code})" : $"{Message} ({Code}) - {Data}";

        public JToken ToJson()
        {
            JObject json = new();
            json["code"] = Code;
            json["message"] = ErrorMessage;
            if (!string.IsNullOrEmpty(Data))
                json["data"] = Data;
            return json;
        }

        public string ErrorMessage => string.IsNullOrEmpty(Data) ? $"{Message}" : $"{Message} - {Data}";
    }
}
