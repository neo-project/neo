// Copyright (C) The Neo Project.
//
// RecoveryMessageExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Messages
{
    /// <summary>
    /// Extension methods for RecoveryMessage to convert dictionary collections to arrays
    /// </summary>
    public static class RecoveryMessageExtensions
    {
        /// <summary>
        /// Converts the PreparationMessages dictionary to an array of ExtensiblePayload
        /// </summary>
        /// <param name="recoveryMessage">The RecoveryMessage instance</param>
        /// <returns>An array of ExtensiblePayload objects</returns>
        public static ExtensiblePayload[] GetPreparationPayloadArray(this RecoveryMessage recoveryMessage)
        {
            if (recoveryMessage == null || recoveryMessage.PreparationMessages == null || recoveryMessage.PreparationMessages.Count == 0)
                return Array.Empty<ExtensiblePayload>();

            // Create array of the right size (max validator index + 1)
            int maxIndex = recoveryMessage.PreparationMessages.Keys.Count > 0
                ? recoveryMessage.PreparationMessages.Keys.Max() + 1
                : 0;
            var result = new ExtensiblePayload[Math.Max(maxIndex, 1)];

            // Fill the array with payloads at the correct indices
            foreach (var pair in recoveryMessage.PreparationMessages)
            {
                var prepareResponse = new PrepareResponse
                {
                    BlockIndex = recoveryMessage.BlockIndex,
                    ViewNumber = recoveryMessage.ViewNumber,
                    ValidatorIndex = pair.Value.ValidatorIndex,
                    PreparationHash = recoveryMessage.PreparationHash ?? UInt256.Zero
                };

                // Serialize the message
                byte[] data;
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    prepareResponse.Serialize(writer);
                    writer.Flush();
                    data = ms.ToArray();
                }

                // Create the payload
                var payload = new ExtensiblePayload
                {
                    Category = "dBFT",
                    ValidBlockStart = recoveryMessage.BlockIndex > 0 ? recoveryMessage.BlockIndex - 1 : 0,
                    ValidBlockEnd = recoveryMessage.BlockIndex + 100,
                    Sender = Contract.CreateSignatureRedeemScript(ECCurve.Secp256r1.G).ToScriptHash(),
                    Data = data,
                    Witness = new Witness
                    {
                        InvocationScript = pair.Value.InvocationScript.ToArray(),
                        VerificationScript = Array.Empty<byte>()
                    }
                };

                result[pair.Key] = payload;
            }

            return result;
        }

        /// <summary>
        /// Converts the CommitMessages dictionary to an array of ExtensiblePayload
        /// </summary>
        /// <param name="recoveryMessage">The RecoveryMessage instance</param>
        /// <returns>An array of ExtensiblePayload objects</returns>
        public static ExtensiblePayload[] GetCommitPayloadArray(this RecoveryMessage recoveryMessage)
        {
            if (recoveryMessage == null || recoveryMessage.CommitMessages == null || recoveryMessage.CommitMessages.Count == 0)
                return Array.Empty<ExtensiblePayload>();

            // Create array of the right size (max validator index + 1)
            int maxIndex = recoveryMessage.CommitMessages.Keys.Count > 0
                ? recoveryMessage.CommitMessages.Keys.Max() + 1
                : 0;
            var result = new ExtensiblePayload[Math.Max(maxIndex, 1)];

            // Fill the array with payloads at the correct indices
            foreach (var pair in recoveryMessage.CommitMessages)
            {
                var commit = new Commit
                {
                    BlockIndex = recoveryMessage.BlockIndex,
                    ViewNumber = pair.Value.ViewNumber,
                    ValidatorIndex = pair.Value.ValidatorIndex,
                    Signature = pair.Value.Signature
                };

                // Serialize the message
                byte[] data;
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    commit.Serialize(writer);
                    writer.Flush();
                    data = ms.ToArray();
                }

                // Create the payload
                var payload = new ExtensiblePayload
                {
                    Category = "dBFT",
                    ValidBlockStart = recoveryMessage.BlockIndex > 0 ? recoveryMessage.BlockIndex - 1 : 0,
                    ValidBlockEnd = recoveryMessage.BlockIndex + 100,
                    Sender = Contract.CreateSignatureRedeemScript(ECCurve.Secp256r1.G).ToScriptHash(),
                    Data = data,
                    Witness = new Witness
                    {
                        InvocationScript = pair.Value.InvocationScript.ToArray(),
                        VerificationScript = Array.Empty<byte>()
                    }
                };

                result[pair.Key] = payload;
            }

            return result;
        }

        /// <summary>
        /// Converts the ChangeViewMessages dictionary to an array of ExtensiblePayload
        /// </summary>
        /// <param name="recoveryMessage">The RecoveryMessage instance</param>
        /// <returns>An array of ExtensiblePayload objects</returns>
        public static ExtensiblePayload[] GetChangeViewPayloadArray(this RecoveryMessage recoveryMessage)
        {
            if (recoveryMessage == null || recoveryMessage.ChangeViewMessages == null || recoveryMessage.ChangeViewMessages.Count == 0)
                return Array.Empty<ExtensiblePayload>();

            // Create array of the right size (max validator index + 1)
            int maxIndex = recoveryMessage.ChangeViewMessages.Keys.Count > 0
                ? recoveryMessage.ChangeViewMessages.Keys.Max() + 1
                : 0;
            var result = new ExtensiblePayload[Math.Max(maxIndex, 1)];

            // Fill the array with payloads at the correct indices
            foreach (var pair in recoveryMessage.ChangeViewMessages)
            {
                var changeView = new ChangeView
                {
                    BlockIndex = recoveryMessage.BlockIndex,
                    ViewNumber = recoveryMessage.ViewNumber,
                    ValidatorIndex = pair.Value.ValidatorIndex,
                    Timestamp = pair.Value.Timestamp,
                    // NewViewNumber is calculated internally based on ViewNumber
                };

                // Serialize the message
                byte[] data;
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    changeView.Serialize(writer);
                    writer.Flush();
                    data = ms.ToArray();
                }

                // Create the payload
                var payload = new ExtensiblePayload
                {
                    Category = "dBFT",
                    ValidBlockStart = recoveryMessage.BlockIndex > 0 ? recoveryMessage.BlockIndex - 1 : 0,
                    ValidBlockEnd = recoveryMessage.BlockIndex + 100,
                    Sender = Contract.CreateSignatureRedeemScript(ECCurve.Secp256r1.G).ToScriptHash(),
                    Data = data,
                    Witness = new Witness
                    {
                        InvocationScript = pair.Value.InvocationScript.ToArray(),
                        VerificationScript = Array.Empty<byte>()
                    }
                };

                result[pair.Key] = payload;
            }

            return result;
        }
    }
}
