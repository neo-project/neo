// Copyright (C) 2015-2025 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Running;
using Neo.Benchmark;
using Neo.Benchmarks.Persistence.Benchmarks;
using Neo.SmartContract.Benchmark;

// BenchmarkRunner.Run<Benchmarks_PoCs>();
BenchmarkRunner.Run<Benchmarks_UInt160>();
BenchmarkRunner.Run<Benchmarks_Hash>();
BenchmarkRunner.Run<Benchmarks_StorageKey>();
BenchmarkRunner.Run<Bechmarks_ReadOnlyStoreView>();
BenchmarkRunner.Run<Bechmarks_LevelDB>();
