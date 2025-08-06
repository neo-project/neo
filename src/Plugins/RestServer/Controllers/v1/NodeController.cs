// Copyright (C) 2015-2025 The Neo Project.
//
// NodeController.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neo.Network.P2P;
using Neo.Plugins.RestServer.Extensions;
using Neo.Plugins.RestServer.Models.Error;
using Neo.Plugins.RestServer.Models.Node;
using System;
using System.Linq;
using System.Net.Mime;

namespace Neo.Plugins.RestServer.Controllers.v1
{
    [Route("/api/v{version:apiVersion}/node")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
    [ApiVersion("1.0")]
    [ApiController]
    public class NodeController : ControllerBase
    {
        private readonly LocalNode _neoLocalNode;
        private readonly NeoSystem _neoSystem;

        public NodeController()
        {
            _neoLocalNode = RestServerPlugin.LocalNode ?? throw new InvalidOperationException();
            _neoSystem = RestServerPlugin.NeoSystem ?? throw new InvalidOperationException();
        }

        /// <summary>
        /// Gets the connected remote nodes.
        /// </summary>
        /// <returns>An array of the Remote Node Objects.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("peers", Name = "GetPeers")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RemoteNodeModel[]))]
        public IActionResult GetPeers()
        {
            var rNodes = _neoLocalNode
                .GetRemoteNodes()
                .OrderByDescending(o => o.LastBlockIndex)
                .ToArray();

            return Ok(rNodes.Select(s => s.ToModel()));
        }

        /// <summary>
        /// Gets all the loaded plugins of the current connected node.
        /// </summary>
        /// <returns>An array of the Plugin objects.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("plugins", Name = "GetPlugins")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PluginModel[]))]
        public IActionResult GetPlugins() =>
            Ok(Plugin.Plugins.Select(s =>
                new PluginModel()
                {
                    Name = s.Name,
                    Version = s.Version.ToString(3),
                    Description = s.Description,
                }));

        /// <summary>
        /// Gets the ProtocolSettings of the currently connected node.
        /// </summary>
        /// <returns>Protocol Settings Object.</returns>
        /// <response code="200">Successful</response>
        /// <response code="400">An error occurred. See Response for details.</response>
        [HttpGet("settings", Name = "GetProtocolSettings")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProtocolSettingsModel))]
        public IActionResult GetSettings() =>
            Ok(_neoSystem.Settings.ToModel());
    }
}
