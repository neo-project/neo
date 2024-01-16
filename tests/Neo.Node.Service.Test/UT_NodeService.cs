// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NodeService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Neo.Node.Service.Test
{
    public class UT_NodeService
    {
        private static NodeService? _nodeService;

        public UT_NodeService()
        {
            var cb = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(cb);
            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton<NodeService>();
            var servicesProvider = services.BuildServiceProvider();
            _nodeService = servicesProvider.GetService<NodeService>();
        }

        [Fact]
        public void Test_PreDefinedMethodsExist()
        {
            // pre-defined command exist
            Assert.True(PipeCommand.Contains(CommandType.Exit));
        }
    }
}
