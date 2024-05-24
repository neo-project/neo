// Copyright (C) 2015-2024 The Neo Project.
//
// ServiceProxy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.ServiceProcess;

namespace Neo.ConsoleService
{
    internal class ServiceProxy : ServiceBase
    {
        private readonly ConsoleServiceBase _service;

        public ServiceProxy(ConsoleServiceBase service)
        {
            _service = service;
        }

        protected override void OnStart(string[] args)
        {
            _service.OnStart(args);
        }

        protected override void OnStop()
        {
            _service.OnStop();
        }
    }
}
