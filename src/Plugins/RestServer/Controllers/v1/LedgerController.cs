// Copyright (C) 2015-2025 The Neo Project.
//
// LedgerController.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.RestServer.Exceptions;
using Neo.Plugins.RestServer.Extensions;
using Neo.Plugins.RestServer.Models.Blockchain;
using Neo.Plugins.RestServer.Models.Error;
using Neo.Plugins.RestServer.Models.Ledger;
using Neo.SmartContract.Native;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;

namespace Neo.Plugins.RestServer.Controllers.v1
{
    [Route("/api/v{version:apiVersion}/ledger")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
    [ApiVersion("1.0")]
    [ApiController]
    public class LedgerController : ControllerBase
    {
        private readonly NeoSystem _neoSystem;

        public LedgerController()
        {
            _neoSystem = RestServerPlugin.NeoSystem ?? throw new NodeNetworkException();
        }

        #region Accounts

        /// <summary>
        /// Gets all the accounts that hold gas on the blockchain.
        /// </summary>
        /// <returns>An array of account details object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("gas/accounts", Name = "GetGasAccounts")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountDetails[]))]
        public IActionResult ShowGasAccounts()
        {
            var accounts = NativeContract.GAS.ListAccounts(_neoSystem.StoreView, _neoSystem.Settings);
            return Ok(accounts.OrderByDescending(o => o.Balance));
        }

        /// <summary>
        /// Get all the accounts that hold neo on the blockchain.
        /// </summary>
        /// <returns>An array of account details object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("neo/accounts", Name = "GetNeoAccounts")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountDetails[]))]
        public IActionResult ShowNeoAccounts()
        {
            var accounts = NativeContract.NEO.ListAccounts(_neoSystem.StoreView, _neoSystem.Settings);
            return Ok(accounts.OrderByDescending(o => o.Balance));
        }

        #endregion

        #region Blocks

        /// <summary>
        /// Get blocks from the blockchain.
        /// </summary>
        /// <param name="skip" example="1">Page</param>
        /// <param name="take" example="50">Page Size</param>
        /// <returns>An array of Block Header Objects</returns>
        /// <response code="204">No more pages.</response>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("blocks", Name = "GetBlocks")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Header[]))]
        public IActionResult GetBlocks(
            [FromQuery(Name = "page")]
            uint skip = 1,
            [FromQuery(Name = "size")]
            uint take = 50)
        {
            if (skip < 1 || take < 1 || take > RestServerSettings.Current.MaxPageSize)
                throw new InvalidParameterRangeException();
            //var start = (skip - 1) * take + startIndex;
            //var end = start + take;
            var start = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView) - (skip - 1) * take;
            var end = start - take;
            var lstOfBlocks = new List<Header>();
            for (var i = start; i > end; i--)
            {
                var block = NativeContract.Ledger.GetBlock(_neoSystem.StoreView, i);
                if (block == null)
                    break;
                lstOfBlocks.Add(block.Header);
            }
            if (lstOfBlocks.Count == 0)
                return NoContent();
            return Ok(lstOfBlocks);
        }

        /// <summary>
        /// Gets the current block header of the connected node.
        /// </summary>
        /// <returns>Full Block Header Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("blockheader/current", Name = "GetCurrnetBlockHeader")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Header))]
        public IActionResult GetCurrentBlockHeader()
        {
            var currentIndex = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView);
            var blockheader = NativeContract.Ledger.GetHeader(_neoSystem.StoreView, currentIndex);
            return Ok(blockheader);
        }

        /// <summary>
        /// Gets a block by an its index.
        /// </summary>
        /// <param name="blockIndex" example="0">Block Index</param>
        /// <returns>Full Block Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("blocks/{index:min(0)}", Name = "GetBlock")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Block))]
        public IActionResult GetBlock(
            [FromRoute(Name = "index")]
            uint blockIndex)
        {
            var block = NativeContract.Ledger.GetBlock(_neoSystem.StoreView, blockIndex);
            if (block == null)
                throw new BlockNotFoundException(blockIndex);
            return Ok(block);
        }

        /// <summary>
        /// Gets a block header by block index.
        /// </summary>
        /// <param name="blockIndex" example="0">Blocks index.</param>
        /// <returns>Block Header Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("blocks/{index:min(0)}/header", Name = "GetBlockHeader")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Header))]
        public IActionResult GetBlockHeader(
            [FromRoute(Name = "index")]
            uint blockIndex)
        {
            var block = NativeContract.Ledger.GetBlock(_neoSystem.StoreView, blockIndex);
            if (block == null)
                throw new BlockNotFoundException(blockIndex);
            return Ok(block.Header);
        }

        /// <summary>
        /// Gets the witness of the block
        /// </summary>
        /// <param name="blockIndex" example="0">Block Index.</param>
        /// <returns>Witness Object</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("blocks/{index:min(0)}/witness", Name = "GetBlockWitness")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Witness))]
        public IActionResult GetBlockWitness(
            [FromRoute(Name = "index")]
            uint blockIndex)
        {
            var block = NativeContract.Ledger.GetBlock(_neoSystem.StoreView, blockIndex);
            if (block == null)
                throw new BlockNotFoundException(blockIndex);
            return Ok(block.Witness);
        }

        /// <summary>
        /// Gets the transactions of the block.
        /// </summary>
        /// <param name="blockIndex" example="0">Block Index.</param>
        /// <param name="skip">Page</param>
        /// <param name="take">Page Size</param>
        /// <returns>An array of transaction object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("blocks/{index:min(0)}/transactions", Name = "GetBlockTransactions")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Transaction[]))]
        public IActionResult GetBlockTransactions(
            [FromRoute(Name = "index")]
            uint blockIndex,
            [FromQuery(Name = "page")]
            int skip = 1,
            [FromQuery(Name = "size")]
            int take = 50)
        {
            if (skip < 1 || take < 1 || take > RestServerSettings.Current.MaxPageSize)
                throw new InvalidParameterRangeException();
            var block = NativeContract.Ledger.GetBlock(_neoSystem.StoreView, blockIndex);
            if (block == null)
                throw new BlockNotFoundException(blockIndex);
            if (block.Transactions == null || block.Transactions.Length == 0)
                return NoContent();
            return Ok(block.Transactions.Skip((skip - 1) * take).Take(take));
        }

        #endregion

        #region Transactions

        /// <summary>
        /// Gets a transaction
        /// </summary>
        /// <param name="hash" example="0xad83d993ca2d9783ca86a000b39920c20508c8ccae7b7db11806646a4832bc50">Hash256</param>
        /// <returns>Transaction object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("transactions/{hash:required}", Name = "GetTransaction")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Transaction))]
        public IActionResult GetTransaction(
            [FromRoute(Name = "hash")]
            UInt256 hash)
        {
            if (NativeContract.Ledger.ContainsTransaction(_neoSystem.StoreView, hash) == false)
                throw new TransactionNotFoundException(hash);
            var txst = NativeContract.Ledger.GetTransaction(_neoSystem.StoreView, hash);
            return Ok(txst);
        }

        /// <summary>
        /// Gets the witness of a transaction.
        /// </summary>
        /// <param name="hash" example="0xad83d993ca2d9783ca86a000b39920c20508c8ccae7b7db11806646a4832bc50">Hash256</param>
        /// <returns>An array of witness object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("transactions/{hash:required}/witnesses", Name = "GetTransactionWitnesses")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Witness[]))]
        public IActionResult GetTransactionWitnesses(
            [FromRoute( Name = "hash")]
            UInt256 hash)
        {
            if (NativeContract.Ledger.ContainsTransaction(_neoSystem.StoreView, hash) == false)
                throw new TransactionNotFoundException(hash);
            var tx = NativeContract.Ledger.GetTransaction(_neoSystem.StoreView, hash);
            return Ok(tx.Witnesses);
        }

        /// <summary>
        /// Gets the signers of a transaction.
        /// </summary>
        /// <param name="hash" example="0xad83d993ca2d9783ca86a000b39920c20508c8ccae7b7db11806646a4832bc50">Hash256</param>
        /// <returns>An array of Signer object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("transactions/{hash:required}/signers", Name = "GetTransactionSigners")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Signer[]))]
        public IActionResult GetTransactionSigners(
            [FromRoute( Name = "hash")]
            UInt256 hash)
        {
            if (NativeContract.Ledger.ContainsTransaction(_neoSystem.StoreView, hash) == false)
                throw new TransactionNotFoundException(hash);
            var tx = NativeContract.Ledger.GetTransaction(_neoSystem.StoreView, hash);
            return Ok(tx.Signers);
        }

        /// <summary>
        /// Gets the transaction attributes of a transaction.
        /// </summary>
        /// <param name="hash" example="0xad83d993ca2d9783ca86a000b39920c20508c8ccae7b7db11806646a4832bc50">Hash256</param>
        /// <returns>An array of the transaction attributes object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("transactions/{hash:required}/attributes", Name = "GetTransactionAttributes")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TransactionAttribute[]))]
        public IActionResult GetTransactionAttributes(
            [FromRoute( Name = "hash")]
            UInt256 hash)
        {
            if (NativeContract.Ledger.ContainsTransaction(_neoSystem.StoreView, hash) == false)
                throw new TransactionNotFoundException(hash);
            var tx = NativeContract.Ledger.GetTransaction(_neoSystem.StoreView, hash);
            return Ok(tx.Attributes);
        }

        #endregion

        #region Memory Pool

        /// <summary>
        /// Gets memory pool.
        /// </summary>
        /// <param name="skip" example="1">Page</param>
        /// <param name="take" example="50">Page Size.</param>
        /// <returns>An array of the Transaction object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("memorypool", Name = "GetMemoryPoolTransactions")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Transaction[]))]
        public IActionResult GetMemoryPool(
            [FromQuery(Name = "page")]
            int skip = 1,
            [FromQuery(Name = "size")]
            int take = 50)
        {
            if (skip < 0 || take < 0 || take > RestServerSettings.Current.MaxPageSize)
                throw new InvalidParameterRangeException();
            return Ok(_neoSystem.MemPool.Skip((skip - 1) * take).Take(take));
        }

        /// <summary>
        /// Gets the count of the memory pool.
        /// </summary>
        /// <returns>Memory Pool Count Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("memorypool/count", Name = "GetMemoryPoolCount")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MemoryPoolCountModel))]
        public IActionResult GetMemoryPoolCount() =>
            Ok(new MemoryPoolCountModel()
            {
                Count = _neoSystem.MemPool.Count,
                UnVerifiedCount = _neoSystem.MemPool.UnVerifiedCount,
                VerifiedCount = _neoSystem.MemPool.VerifiedCount,
            });

        /// <summary>
        /// Gets verified memory pool.
        /// </summary>
        /// <param name="skip" example="1">Page</param>
        /// <param name="take" example="50">Page Size.</param>
        /// <returns>An array of the Transaction object.</returns>
        /// <response code="204">No more pages.</response>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("memorypool/verified", Name = "GetMemoryPoolVeridiedTransactions")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Transaction[]))]
        public IActionResult GetMemoryPoolVerified(
            [FromQuery(Name = "page")]
            int skip = 1,
            [FromQuery(Name = "size")]
            int take = 50)
        {
            if (skip < 0 || take < 0 || take > RestServerSettings.Current.MaxPageSize)
                throw new InvalidParameterRangeException();
            if (_neoSystem.MemPool.Any() == false)
                return NoContent();
            var vTx = _neoSystem.MemPool.GetVerifiedTransactions();
            return Ok(vTx.Skip((skip - 1) * take).Take(take));
        }

        /// <summary>
        /// Gets unverified memory pool.
        /// </summary>
        /// <param name="skip" example="1">Page</param>
        /// <param name="take" example="50">Page Size.</param>
        /// <returns>An array of the Transaction object.</returns>
        /// <response code="204">No more pages.</response>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("memorypool/unverified", Name = "GetMemoryPoolUnveridiedTransactions")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Transaction[]))]
        public IActionResult GetMemoryPoolUnVerified(
            [FromQuery(Name = "page")]
            int skip = 1,
            [FromQuery(Name = "size")]
            int take = 50)
        {
            if (skip < 0 || take < 0 || take > RestServerSettings.Current.MaxPageSize)
                throw new InvalidParameterRangeException();
            if (_neoSystem.MemPool.Any() == false)
                return NoContent();
            _neoSystem.MemPool.GetVerifiedAndUnverifiedTransactions(out _, out var unVerifiedTransactions);
            return Ok(unVerifiedTransactions.Skip((skip - 1) * take).Take(take));
        }

        #endregion
    }
}
