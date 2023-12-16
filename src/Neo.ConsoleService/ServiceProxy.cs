// Copyright (C) 2016-2023 The Neo Project.
// 
// The Neo.ConsoleService is free software distributed under the MIT 
// software license, see the accompanying file LICENSE in the main directory
// of the project or http://www.opensource.org/licenses/mit-license.php 
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
            this._service = service;
        }

        protected override void OnStart(string[] args)
        {
            _service.OnStartAsync(args);
        }

        protected override void OnStop()
        {
            _service.OnStop();
        }
    }
}
