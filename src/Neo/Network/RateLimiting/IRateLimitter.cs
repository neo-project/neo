// Copyright (C) 2015-2025 The Neo Project.
//
// IRateLimitter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Net;

namespace Neo.Network.RateLimiting
{
    public interface IRateLimitter
    {
        /// <summary>
        /// Allow or deny request
        /// </summary>
        /// <param name="ip">Ip Address</param>
        /// <param name="protocol">Protocol (P2P or RPC)</param>
        /// <param name="requestBytes">Request data length</param>
        /// <returns>True if allowed</returns>
        public bool AllowRequest(IPAddress ip, string protocol, long requestBytes);

        /// <summary>
        /// Allow or deny response based on previous request
        /// </summary>
        /// <param name="ip">Ip Address</param>
        /// <param name="protocol">Protocol (P2P or RPC)</param>
        /// <param name="responseBytes">Response data length</param>
        /// <returns>True if allowed</returns>
        public bool AllowResponse(IPAddress ip, string protocol, long responseBytes);
    }
}
