// Copyright (C) 2015-2025 The Neo Project.
//
// UtilsController.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neo.Plugins.RestServer.Exceptions;
using Neo.Plugins.RestServer.Models.Error;
using Neo.Plugins.RestServer.Models.Utils;
using Neo.Wallets;
using System;
using System.Net.Mime;

namespace Neo.Plugins.RestServer.Controllers.v1
{
    [Route("/api/v{version:apiVersion}/utils")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
    [ApiVersion("1.0")]
    [ApiController]
    public class UtilsController : ControllerBase
    {
        private readonly NeoSystem _neoSystem;

        public UtilsController()
        {
            _neoSystem = RestServerPlugin.NeoSystem ?? throw new NodeNetworkException();
        }

        #region Validation

        /// <summary>
        /// Converts script to Neo address.
        /// </summary>
        /// <param name="ScriptHash" example="0xed7cc6f5f2dd842d384f254bc0c2d58fb69a4761">ScriptHash</param>
        /// <returns>Util Address Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("{hash:required}/address", Name = "GetAddressByScripthash")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UtilsAddressModel))]
        public IActionResult ScriptHashToWalletAddress(
            [FromRoute(Name = "hash")]
            UInt160 ScriptHash)
        {
            try
            {
                return Ok(new UtilsAddressModel() { Address = ScriptHash.ToAddress(_neoSystem.Settings.AddressVersion) });
            }
            catch (FormatException)
            {
                throw new ScriptHashFormatException();
            }
        }

        /// <summary>
        /// Converts Neo address to ScriptHash
        /// </summary>
        /// <param name="address" example="NNLi44dJNXtDNSBkofB48aTVYtb1zZrNEs">Neo Address</param>
        /// <returns>Util ScriptHash Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("{address:required}/scripthash", Name = "GetScripthashByAddress")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UtilsScriptHashModel))]
        public IActionResult WalletAddressToScriptHash(
            [FromRoute(Name = "address")]
            string address)
        {
            try
            {
                return Ok(new UtilsScriptHashModel() { ScriptHash = address.ToScriptHash(_neoSystem.Settings.AddressVersion) });
            }
            catch (FormatException)
            {
                throw new AddressFormatException();
            }
        }

        /// <summary>
        /// Get whether or not a Neo address or ScriptHash is valid.
        /// </summary>
        /// <param name="AddressOrScriptHash"></param>
        /// <returns>Util Address Valid Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("{address:required}/validate", Name = "IsValidAddressOrScriptHash")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UtilsAddressIsValidModel))]
        public IActionResult ValidateAddress(
            [FromRoute(Name = "address")]
            string AddressOrScriptHash)
        {
            return Ok(new UtilsAddressIsValidModel()
            {
                Address = AddressOrScriptHash,
                IsValid = RestServerUtility.TryConvertToScriptHash(AddressOrScriptHash, _neoSystem.Settings, out _),
            });
        }

        #endregion
    }
}
