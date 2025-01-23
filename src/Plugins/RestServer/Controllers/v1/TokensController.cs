// Copyright (C) 2015-2025 The Neo Project.
//
// TokensController.cs file belongs to the neo project and is free
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
using Neo.Plugins.RestServer.Extensions;
using Neo.Plugins.RestServer.Helpers;
using Neo.Plugins.RestServer.Models;
using Neo.Plugins.RestServer.Models.Error;
using Neo.Plugins.RestServer.Models.Token;
using Neo.Plugins.RestServer.Tokens;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;

namespace Neo.Plugins.RestServer.Controllers.v1
{
    [Route("/api/v{version:apiVersion}/tokens")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
    [ApiVersion("1.0")]
    [ApiController]
    public class TokensController : ControllerBase
    {
        private readonly NeoSystem _neoSystem;

        public TokensController()
        {
            _neoSystem = RestServerPlugin.NeoSystem ?? throw new NodeNetworkException();
        }

        #region NEP-17

        /// <summary>
        /// Gets all Nep-17 valid contracts from the blockchain.
        /// </summary>
        /// <param name="skip" example="1">Page</param>
        /// <param name="take" example="50">Page Size</param>
        /// <returns>An array of the Nep-17 Token Object.</returns>
        /// <response code="204">No more pages.</response>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("nep-17", Name = "GetNep17Tokens")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NEP17TokenModel[]))]
        public IActionResult GetNEP17(
            [FromQuery(Name = "page")]
            int skip = 1,
            [FromQuery(Name = "size")]
            int take = 50)
        {
            if (skip < 1 || take < 1 || take > RestServerSettings.Current.MaxPageSize)
                throw new InvalidParameterRangeException();
            var tokenList = NativeContract.ContractManagement.ListContracts(_neoSystem.StoreView);
            var vaildContracts = tokenList
                .Where(ContractHelper.IsNep17Supported)
                .OrderBy(o => o.Manifest.Name)
                .Skip((skip - 1) * take)
                .Take(take);
            if (vaildContracts.Any() == false)
                return NoContent();
            var listResults = new List<NEP17TokenModel>();
            foreach (var contract in vaildContracts)
            {
                try
                {
                    var token = new NEP17Token(_neoSystem, contract.Hash);
                    listResults.Add(token.ToModel());
                }
                catch
                {
                }
            }
            if (listResults.Any() == false)
                return NoContent();
            return Ok(listResults);
        }

        /// <summary>
        /// The count of how many Nep-17 contracts are on the blockchain.
        /// </summary>
        /// <returns>Count Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("nep-17/count", Name = "GetNep17TokenCount")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CountModel))]
        public IActionResult GetNEP17Count()
        {
            return Ok(new CountModel()
            {
                Count = NativeContract.ContractManagement.ListContracts(_neoSystem.StoreView).Count(ContractHelper.IsNep17Supported)
            });
        }

        /// <summary>
        /// Gets the balance of the Nep-17 contract by an address.
        /// </summary>
        /// <param name="tokenAddessOrScripthash" example="0xed7cc6f5f2dd842d384f254bc0c2d58fb69a4761">Nep-17 ScriptHash</param>
        /// <param name="lookupAddressOrScripthash" example="0xed7cc6f5f2dd842d384f254bc0c2d58fb69a4761">Neo Address ScriptHash</param>
        /// <returns>Token Balance Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("nep-17/{scripthash:required}/balanceof/{address:required}", Name = "GetNep17TokenBalanceOf")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenBalanceModel))]
        public IActionResult GetNEP17(
            [FromRoute(Name = "scripthash")]
            UInt160 tokenAddessOrScripthash,
            [FromRoute(Name = "address")]
            UInt160 lookupAddressOrScripthash)
        {
            var contract = NativeContract.ContractManagement.GetContract(_neoSystem.StoreView, tokenAddessOrScripthash) ??
                throw new ContractNotFoundException(tokenAddessOrScripthash);
            if (ContractHelper.IsNep17Supported(contract) == false)
                throw new Nep17NotSupportedException(tokenAddessOrScripthash);
            try
            {
                var token = new NEP17Token(_neoSystem, tokenAddessOrScripthash);
                return Ok(new TokenBalanceModel()
                {
                    Name = token.Name,
                    ScriptHash = token.ScriptHash,
                    Symbol = token.Symbol,
                    Decimals = token.Decimals,
                    Balance = token.BalanceOf(lookupAddressOrScripthash).Value,
                    TotalSupply = token.TotalSupply().Value,
                });
            }
            catch
            {
                throw new Nep17NotSupportedException(tokenAddessOrScripthash);
            }
        }

        #endregion

        #region NEP-11

        /// <summary>
        /// Gets all the Nep-11 valid contracts on from the blockchain.
        /// </summary>
        /// <param name="skip" example="1">Page</param>
        /// <param name="take" example="50">Page Size</param>
        /// <returns>Nep-11 Token Object.</returns>
        /// <response code="204">No more pages.</response>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("nep-11", Name = "GetNep11Tokens")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NEP11TokenModel[]))]
        public IActionResult GetNEP11(
            [FromQuery(Name = "page")]
            int skip = 1,
            [FromQuery(Name = "size")]
            int take = 50)
        {
            if (skip < 1 || take < 1 || take > RestServerSettings.Current.MaxPageSize)
                throw new InvalidParameterRangeException();
            var tokenList = NativeContract.ContractManagement.ListContracts(_neoSystem.StoreView);
            var vaildContracts = tokenList
                .Where(ContractHelper.IsNep11Supported)
                .OrderBy(o => o.Manifest.Name)
                .Skip((skip - 1) * take)
                .Take(take);
            if (vaildContracts.Any() == false)
                return NoContent();
            var listResults = new List<NEP11TokenModel>();
            foreach (var contract in vaildContracts)
            {
                try
                {
                    var token = new NEP11Token(_neoSystem, contract.Hash);
                    listResults.Add(token.ToModel());
                }
                catch
                {
                }
            }
            if (listResults.Any() == false)
                return NoContent();
            return Ok(listResults);
        }

        /// <summary>
        /// The count of how many Nep-11 contracts are on the blockchain.
        /// </summary>
        /// <returns>Count Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("nep-11/count", Name = "GetNep11TokenCount")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CountModel))]
        public IActionResult GetNEP11Count()
        {
            return Ok(new CountModel() { Count = NativeContract.ContractManagement.ListContracts(_neoSystem.StoreView).Count(ContractHelper.IsNep11Supported) });
        }

        /// <summary>
        /// Gets the balance of the Nep-11 contract by an address.
        /// </summary>
        /// <param name="sAddressHash" example="0xed7cc6f5f2dd842d384f254bc0c2d58fb69a4761">Nep-11 ScriptHash</param>
        /// <param name="addressHash" example="0xed7cc6f5f2dd842d384f254bc0c2d58fb69a4761">Neo Address ScriptHash</param>
        /// <returns>Token Balance Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("nep-11/{scripthash:required}/balanceof/{address:required}", Name = "GetNep11TokenBalanceOf")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenBalanceModel))]
        public IActionResult GetNEP11(
            [FromRoute(Name = "scripthash")]
            UInt160 sAddressHash,
            [FromRoute(Name = "address")]
            UInt160 addressHash)
        {
            var contract = NativeContract.ContractManagement.GetContract(_neoSystem.StoreView, sAddressHash) ??
                throw new ContractNotFoundException(sAddressHash);
            if (ContractHelper.IsNep11Supported(contract) == false)
                throw new Nep11NotSupportedException(sAddressHash);
            try
            {
                var token = new NEP11Token(_neoSystem, sAddressHash);
                return Ok(new TokenBalanceModel()
                {
                    Name = token.Name,
                    ScriptHash = token.ScriptHash,
                    Symbol = token.Symbol,
                    Decimals = token.Decimals,
                    Balance = token.BalanceOf(addressHash).Value,
                    TotalSupply = token.TotalSupply().Value,
                });
            }
            catch
            {
                throw new Nep11NotSupportedException(sAddressHash);
            }
        }

        #endregion

        /// <summary>
        /// Gets every single NEP17/NEP11 on the blockchain's balance by ScriptHash
        /// </summary>
        /// <param name="addressOrScripthash"></param>
        /// <returns>Token Balance Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("balanceof/{address:required}", Name = "GetAllTokensBalanceOf")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenBalanceModel))]
        public IActionResult GetBalances(
            [FromRoute(Name = "address")]
            UInt160 addressOrScripthash)
        {
            var tokenList = NativeContract.ContractManagement.ListContracts(_neoSystem.StoreView);
            var validContracts = tokenList
                .Where(w => ContractHelper.IsNep17Supported(w) || ContractHelper.IsNep11Supported(w))
                .OrderBy(o => o.Manifest.Name);
            var listResults = new List<TokenBalanceModel>();
            foreach (var contract in validContracts)
            {
                try
                {
                    var token = new NEP17Token(_neoSystem, contract.Hash);
                    var balance = token.BalanceOf(addressOrScripthash).Value;
                    if (balance == 0)
                        continue;
                    listResults.Add(new()
                    {
                        Name = token.Name,
                        ScriptHash = token.ScriptHash,
                        Symbol = token.Symbol,
                        Decimals = token.Decimals,
                        Balance = balance,
                        TotalSupply = token.TotalSupply().Value,
                    });

                    var nft = new NEP11Token(_neoSystem, contract.Hash);
                    balance = nft.BalanceOf(addressOrScripthash).Value;
                    if (balance == 0)
                        continue;
                    listResults.Add(new()
                    {
                        Name = nft.Name,
                        ScriptHash = nft.ScriptHash,
                        Symbol = nft.Symbol,
                        Balance = balance,
                        Decimals = nft.Decimals,
                        TotalSupply = nft.TotalSupply().Value,
                    });
                }
                catch (NotSupportedException)
                {
                }
            }
            return Ok(listResults);
        }
    }
}
