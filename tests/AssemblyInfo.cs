// Copyright (C) 2015-2025 The Neo Project.
//
// AssemblyInfo.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;

// Test projects that wish to enable parallelization should add the following in csproj:
// <PropertyGroup>
//   <DefineConstants>$(DefineConstants);DISABLE_TEST_PARALLELIZATION</DefineConstants>
// </PropertyGroup>
#if ENABLE_TEST_PARALLELIZATION
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
#else
[assembly: DoNotParallelize]
#endif
