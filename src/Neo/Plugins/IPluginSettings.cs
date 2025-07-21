// Copyright (C) 2015-2025 The Neo Project.
//
<<<<<<<< HEAD:tests/Neo.Build.Core.Tests/MSTestSettings.cs
// MSTestSettings.cs file belongs to the neo project and is free
========
// IPluginSettings.cs file belongs to the neo project and is free
>>>>>>>> neo/dev:src/Neo/Plugins/IPluginSettings.cs
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

<<<<<<<< HEAD:tests/Neo.Build.Core.Tests/MSTestSettings.cs
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
========
namespace Neo.Plugins
{
    public interface IPluginSettings
    {
        public UnhandledExceptionPolicy ExceptionPolicy { get; }
    }
}
>>>>>>>> neo/dev:src/Neo/Plugins/IPluginSettings.cs
