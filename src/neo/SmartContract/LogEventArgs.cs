// Copyright (C) 2015-2021 NEO GLOBAL DEVELOPMENT.
// 
// The Neo project is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using System;

namespace Neo.SmartContract
{
    /// <summary>
    /// The <see cref="EventArgs"/> of <see cref="ApplicationEngine.Log"/>.
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// The container that containing the executed script.
        /// </summary>
        public IVerifiable ScriptContainer { get; }

        /// <summary>
        /// The script hash of the contract that sends the log.
        /// </summary>
        public UInt160 ScriptHash { get; }

        /// <summary>
        /// The message of the log.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEventArgs"/> class.
        /// </summary>
        /// <param name="container">The container that containing the executed script.</param>
        /// <param name="script_hash">The script hash of the contract that sends the log.</param>
        /// <param name="message">The message of the log.</param>
        public LogEventArgs(IVerifiable container, UInt160 script_hash, string message)
        {
            this.ScriptContainer = container;
            this.ScriptHash = script_hash;
            this.Message = message;
        }
    }
}
