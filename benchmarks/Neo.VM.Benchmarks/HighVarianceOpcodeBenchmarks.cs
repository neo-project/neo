// Copyright (C) 2015-2025 The Neo Project.
//
// HighVarianceOpcodeBenchmarks.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using NeoStackItemType = Neo.VM.Types.StackItemType;

namespace Neo.VM.Benchmark
{
    /// <summary>
    /// Focused benchmark suite covering opcodes whose performance characteristics
    /// change significantly with input size (arrays, maps, buffers, conversions, splice operations).
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 8)]
    public class HighVarianceOpcodeBenchmarks
    {
        public sealed record OpcodeCase(string Name, byte[] Script, Func<ExecutionEngine>? EngineFactory = null)
        {
            public override string ToString() => Name;
        }

        private OpcodeCase[] _packCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _unpackArrayCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _packMapCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _unpackMapCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _packStructCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _newArrayCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _newArrayTypedCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _newStructCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _newBufferCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _convertCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _catCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _substrCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _leftCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _rightCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _memcpyCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _reverseItemsCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _appendCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _setItemCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _pickItemCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _keysCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _valuesCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _hasKeyCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _removeCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _popItemCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _clearItemsCases = Array.Empty<OpcodeCase>();

        [GlobalSetup]
        public void Setup()
        {
            _packCases = BuildPackCases();
            _unpackArrayCases = BuildUnpackArrayCases();
            _packMapCases = BuildPackMapCases();
            _unpackMapCases = BuildUnpackMapCases();
            _packStructCases = BuildPackStructCases();
            _newArrayCases = BuildNewArrayCases();
            _newArrayTypedCases = BuildNewArrayTypedCases();
            _newStructCases = BuildNewStructCases();
            _newBufferCases = BuildNewBufferCases();
            _convertCases = BuildConvertCases();
            _catCases = BuildCatCases();
            _substrCases = BuildSubstrCases();
            _leftCases = BuildLeftCases();
            _rightCases = BuildRightCases();
            _memcpyCases = BuildMemcpyCases();
            _reverseItemsCases = BuildReverseItemsCases();
            _appendCases = BuildAppendCases();
            _setItemCases = BuildSetItemCases();
            _pickItemCases = BuildPickItemCases();
            _keysCases = BuildKeysCases();
            _valuesCases = BuildValuesCases();
            _hasKeyCases = BuildHasKeyCases();
            _removeCases = BuildRemoveCases();
            _popItemCases = BuildPopItemCases();
            _clearItemsCases = BuildClearItemsCases();
        }

        #region Benchmark entry points

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.PACK))]
        [ArgumentsSource(nameof(PackCases))]
        public void Pack(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.UNPACK))]
        [ArgumentsSource(nameof(UnpackArrayCases))]
        public void UnpackArray(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.PACKMAP))]
        [ArgumentsSource(nameof(PackMapCases))]
        public void PackMap(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.UNPACK))]
        [ArgumentsSource(nameof(UnpackMapCases))]
        public void UnpackMap(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.PACKSTRUCT))]
        [ArgumentsSource(nameof(PackStructCases))]
        public void PackStruct(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.NEWARRAY))]
        [ArgumentsSource(nameof(NewArrayCases))]
        public void NewArray(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.NEWARRAY_T))]
        [ArgumentsSource(nameof(NewArrayTypedCases))]
        public void NewArrayTyped(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.NEWSTRUCT))]
        [ArgumentsSource(nameof(NewStructCases))]
        public void NewStruct(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.NEWBUFFER))]
        [ArgumentsSource(nameof(NewBufferCases))]
        public void NewBuffer(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.CONVERT))]
        [ArgumentsSource(nameof(ConvertCases))]
        public void Convert(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.CAT))]
        [ArgumentsSource(nameof(CatCases))]
        public void Cat(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.SUBSTR))]
        [ArgumentsSource(nameof(SubstrCases))]
        public void Substr(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.LEFT))]
        [ArgumentsSource(nameof(LeftCases))]
        public void Left(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.RIGHT))]
        [ArgumentsSource(nameof(RightCases))]
        public void Right(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.MEMCPY))]
        [ArgumentsSource(nameof(MemcpyCases))]
        public void Memcpy(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.REVERSEITEMS))]
        [ArgumentsSource(nameof(ReverseItemsCases))]
        public void ReverseItems(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.APPEND))]
        [ArgumentsSource(nameof(AppendCases))]
        public void Append(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.SETITEM))]
        [ArgumentsSource(nameof(SetItemCases))]
        public void SetItem(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.PICKITEM))]
        [ArgumentsSource(nameof(PickItemCases))]
        public void PickItem(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.KEYS))]
        [ArgumentsSource(nameof(KeysCases))]
        public void Keys(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.VALUES))]
        [ArgumentsSource(nameof(ValuesCases))]
        public void Values(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.HASKEY))]
        [ArgumentsSource(nameof(HasKeyCases))]
        public void HasKey(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.REMOVE))]
        [ArgumentsSource(nameof(RemoveCases))]
        public void Remove(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.POPITEM))]
        [ArgumentsSource(nameof(PopItemCases))]
        public void PopItem(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.CLEARITEMS))]
        [ArgumentsSource(nameof(ClearItemsCases))]
        public void ClearItems(OpcodeCase @case) => ExecuteCase(@case);

        public IEnumerable<OpcodeCase> PackCases() => _packCases;
        public IEnumerable<OpcodeCase> UnpackArrayCases() => _unpackArrayCases;
        public IEnumerable<OpcodeCase> PackMapCases() => _packMapCases;
        public IEnumerable<OpcodeCase> UnpackMapCases() => _unpackMapCases;
        public IEnumerable<OpcodeCase> PackStructCases() => _packStructCases;
        public IEnumerable<OpcodeCase> NewArrayCases() => _newArrayCases;
        public IEnumerable<OpcodeCase> NewArrayTypedCases() => _newArrayTypedCases;
        public IEnumerable<OpcodeCase> NewStructCases() => _newStructCases;
        public IEnumerable<OpcodeCase> NewBufferCases() => _newBufferCases;
        public IEnumerable<OpcodeCase> ConvertCases() => _convertCases;
        public IEnumerable<OpcodeCase> CatCases() => _catCases;
        public IEnumerable<OpcodeCase> SubstrCases() => _substrCases;
        public IEnumerable<OpcodeCase> LeftCases() => _leftCases;
        public IEnumerable<OpcodeCase> RightCases() => _rightCases;
        public IEnumerable<OpcodeCase> MemcpyCases() => _memcpyCases;
        public IEnumerable<OpcodeCase> ReverseItemsCases() => _reverseItemsCases;
        public IEnumerable<OpcodeCase> AppendCases() => _appendCases;
        public IEnumerable<OpcodeCase> SetItemCases() => _setItemCases;
        public IEnumerable<OpcodeCase> PickItemCases() => _pickItemCases;
        public IEnumerable<OpcodeCase> KeysCases() => _keysCases;
        public IEnumerable<OpcodeCase> ValuesCases() => _valuesCases;
        public IEnumerable<OpcodeCase> HasKeyCases() => _hasKeyCases;
        public IEnumerable<OpcodeCase> RemoveCases() => _removeCases;
        public IEnumerable<OpcodeCase> PopItemCases() => _popItemCases;
        public IEnumerable<OpcodeCase> ClearItemsCases() => _clearItemsCases;

        #endregion

        #region Case builders

        private static OpcodeCase[] BuildPackCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateSequentialInts(count);
                    return new OpcodeCase($"PACK_{count}", BuildPackScript(data));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildUnpackArrayCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateSequentialInts(count);
                    return new OpcodeCase($"UNPACK_ARRAY_{count}", BuildUnpackArrayScript(data));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildPackMapCases()
        {
            var sizes = new[] { 16, 64, 128 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateMap(count);
                    return new OpcodeCase($"PACKMAP_{count}", BuildPackMapScript(data));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildUnpackMapCases()
        {
            var sizes = new[] { 16, 64 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateMap(count);
                    return new OpcodeCase($"UNPACK_MAP_{count}", BuildUnpackMapScript(data));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildPackStructCases()
        {
            var sizes = new[] { 16, 128 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateSequentialInts(count);
                    return new OpcodeCase($"PACKSTRUCT_{count}", BuildPackStructScript(data));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildNewArrayCases()
        {
            var sizes = new[] { 16, 256, 1024 };
            return sizes
                .Select(count => new OpcodeCase($"NEWARRAY_{count}", BuildNewArrayScript(count)))
                .ToArray();
        }

        private static OpcodeCase[] BuildNewArrayTypedCases()
        {
            var sizes = new[] { 16, 256, 1024 };
            return sizes
                .Select(count => new OpcodeCase($"NEWARRAY_T_{count}", BuildNewArrayTypedScript(count, NeoStackItemType.ByteString)))
                .ToArray();
        }

        private static OpcodeCase[] BuildNewStructCases()
        {
            var sizes = new[] { 16, 128, 256 };
            return sizes
                .Select(count => new OpcodeCase($"NEWSTRUCT_{count}", BuildNewStructScript(count)))
                .ToArray();
        }

        private static OpcodeCase[] BuildNewBufferCases()
        {
            var sizes = new[] { 32, 512, 4096 };
            return sizes
                .Select(count => new OpcodeCase($"NEWBUFFER_{count}", BuildNewBufferScript(count)))
                .ToArray();
        }

        private static OpcodeCase[] BuildConvertCases()
        {
            var sizes = new[] { 32, 512, 4096 };
            return sizes
                .Select(count => new OpcodeCase($"CONVERT_BUFFER_{count}", BuildConvertScript(count, NeoStackItemType.Buffer)))
                .ToArray();
        }

        private static OpcodeCase[] BuildCatCases()
        {
            var sizes = new[] { 32, 256, 1024 };
            return sizes
                .Select(size => new OpcodeCase($"CAT_{size}", BuildCatScript(size)))
                .ToArray();
        }

        private static OpcodeCase[] BuildSubstrCases()
        {
            var options = new (int Total, int Extract)[]
            {
                (256, 64),
                (1024, 256),
                (4096, 1024)
            };
            return options
                .Select(opt => new OpcodeCase($"SUBSTR_{opt.Total}_{opt.Extract}", BuildSubstrScript(opt.Total, 0, opt.Extract)))
                .ToArray();
        }

        private static OpcodeCase[] BuildLeftCases()
        {
            var options = new (int Total, int Count)[]
            {
                (256, 64),
                (1024, 256),
                (4096, 1024)
            };
            return options
                .Select(opt => new OpcodeCase($"LEFT_{opt.Total}_{opt.Count}", BuildLeftScript(opt.Total, opt.Count)))
                .ToArray();
        }

        private static OpcodeCase[] BuildRightCases()
        {
            var options = new (int Total, int Count)[]
            {
                (256, 64),
                (1024, 256),
                (4096, 1024)
            };
            return options
                .Select(opt => new OpcodeCase($"RIGHT_{opt.Total}_{opt.Count}", BuildRightScript(opt.Total, opt.Count)))
                .ToArray();
        }

        private static OpcodeCase[] BuildMemcpyCases()
        {
            var options = new (int Buffer, int Count)[]
            {
                (256, 128),
                (2048, 512),
                (4096, 2048)
            };
            return options
                .Select(opt => new OpcodeCase($"MEMCPY_{opt.Buffer}_{opt.Count}", BuildMemcpyScript(opt.Buffer, opt.Count)))
                .ToArray();
        }

        private static OpcodeCase[] BuildReverseItemsCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateSequentialInts(count);
                    return new OpcodeCase($"REVERSEITEMS_{count}", BuildReverseItemsScript(data));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildAppendCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateSequentialInts(count);
                    return new OpcodeCase($"APPEND_{count}", BuildAppendScript(data, count));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildSetItemCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateSequentialInts(count);
                    var index = count / 2;
                    return new OpcodeCase($"SETITEM_{count}", BuildSetItemScript(data, index, count * 10));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildPickItemCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateSequentialInts(count);
                    var index = count - 1;
                    return new OpcodeCase($"PICKITEM_{count}", BuildPickItemScript(data, index));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildKeysCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateMap(count);
                    return new OpcodeCase($"KEYS_{count}", BuildKeysScript(data));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildValuesCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateMap(count);
                    return new OpcodeCase($"VALUES_{count}", BuildValuesScript(data));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildHasKeyCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateMap(count);
                    return new OpcodeCase($"HASKEY_{count}", BuildHasKeyScript(data, count / 2));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildRemoveCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateMap(count);
                    return new OpcodeCase($"REMOVE_{count}", BuildRemoveScript(data, count / 2));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildPopItemCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateSequentialInts(count);
                    return new OpcodeCase($"POPITEM_{count}", BuildPopItemScript(data));
                })
                .ToArray();
        }

        private static OpcodeCase[] BuildClearItemsCases()
        {
            var sizes = new[] { 16, 128, 512 };
            return sizes
                .Select(count =>
                {
                    var data = GenerateSequentialInts(count);
                    return new OpcodeCase($"CLEARITEMS_{count}", BuildClearItemsScript(data));
                })
                .ToArray();
        }

        #endregion

        #region Script builders

        private static byte[] BuildPackScript(IReadOnlyList<int> values) =>
            BuildScript(builder =>
            {
                EmitArrayLiteral(builder, values);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildUnpackArrayScript(IReadOnlyList<int> values) =>
            BuildScript(builder =>
            {
                EmitArrayLiteral(builder, values);
                builder.Emit(OpCode.UNPACK);
                DropMany(builder, values.Count + 1);
            });

        private static byte[] BuildPackMapScript(IReadOnlyList<KeyValuePair<int, int>> map) =>
            BuildScript(builder =>
            {
                EmitMapLiteral(builder, map);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildUnpackMapScript(IReadOnlyList<KeyValuePair<int, int>> map) =>
            BuildScript(builder =>
            {
                EmitMapLiteral(builder, map);
                builder.Emit(OpCode.UNPACK);
                DropMany(builder, map.Count * 2 + 1);
            });

        private static byte[] BuildPackStructScript(IReadOnlyList<int> values) =>
            BuildScript(builder =>
            {
                EmitStructLiteral(builder, values);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildNewArrayScript(int count) =>
            BuildScript(builder =>
            {
                builder.EmitPush(count);
                builder.Emit(OpCode.NEWARRAY);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildNewArrayTypedScript(int count, NeoStackItemType type) =>
            BuildScript(builder =>
            {
                builder.EmitPush(count);
                builder.Emit(OpCode.NEWARRAY_T, new[] { (byte)type });
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildNewStructScript(int count) =>
            BuildScript(builder =>
            {
                builder.EmitPush(count);
                builder.Emit(OpCode.NEWSTRUCT);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildNewBufferScript(int size) =>
            BuildScript(builder =>
            {
                builder.EmitPush(size);
                builder.Emit(OpCode.NEWBUFFER);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildConvertScript(int size, NeoStackItemType targetType)
        {
            var data = GenerateByteArray(size);
            return BuildScript(builder =>
            {
                builder.EmitPush(data);
                builder.Emit(OpCode.CONVERT, new[] { (byte)targetType });
                builder.Emit(OpCode.DROP);
            });
        }

        private static byte[] BuildCatScript(int segmentSize)
        {
            var left = GenerateByteArray(segmentSize);
            var right = GenerateByteArray(segmentSize);
            return BuildScript(builder =>
            {
                builder.EmitPush(left);
                builder.EmitPush(right);
                builder.Emit(OpCode.CAT);
                builder.Emit(OpCode.DROP);
            });
        }

        private static byte[] BuildSubstrScript(int totalSize, int start, int count)
        {
            var data = GenerateByteArray(totalSize);
            return BuildScript(builder =>
            {
                builder.EmitPush(data);
                builder.EmitPush(start);
                builder.EmitPush(count);
                builder.Emit(OpCode.SUBSTR);
                builder.Emit(OpCode.DROP);
            });
        }

        private static byte[] BuildLeftScript(int totalSize, int count)
        {
            var data = GenerateByteArray(totalSize);
            return BuildScript(builder =>
            {
                builder.EmitPush(data);
                builder.EmitPush(count);
                builder.Emit(OpCode.LEFT);
                builder.Emit(OpCode.DROP);
            });
        }

        private static byte[] BuildRightScript(int totalSize, int count)
        {
            var data = GenerateByteArray(totalSize);
            return BuildScript(builder =>
            {
                builder.EmitPush(data);
                builder.EmitPush(count);
                builder.Emit(OpCode.RIGHT);
                builder.Emit(OpCode.DROP);
            });
        }

        private static byte[] BuildMemcpyScript(int bufferSize, int copyLength)
        {
            if (copyLength > bufferSize)
                throw new ArgumentOutOfRangeException(nameof(copyLength), "copy length must not exceed buffer size.");

            var source = GenerateByteArray(bufferSize);
            return BuildScript(builder =>
            {
                builder.EmitPush(bufferSize);
                builder.Emit(OpCode.NEWBUFFER);   // destination buffer
                builder.EmitPush(0);               // destination index
                builder.EmitPush(source);          // source buffer (ByteString)
                builder.EmitPush(0);               // source index
                builder.EmitPush(copyLength);      // count
                builder.Emit(OpCode.MEMCPY);
            });
        }

        private static byte[] BuildReverseItemsScript(IReadOnlyList<int> values) =>
            BuildScript(builder =>
            {
                EmitArrayLiteral(builder, values);
                builder.Emit(OpCode.DUP);
                builder.Emit(OpCode.REVERSEITEMS);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildAppendScript(IReadOnlyList<int> values, int newItem) =>
            BuildScript(builder =>
            {
                EmitArrayLiteral(builder, values);
                builder.Emit(OpCode.DUP);
                builder.EmitPush(newItem);
                builder.Emit(OpCode.APPEND);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildSetItemScript(IReadOnlyList<int> values, int index, int newValue) =>
            BuildScript(builder =>
            {
                EmitArrayLiteral(builder, values);
                builder.Emit(OpCode.DUP);
                builder.EmitPush(index);
                builder.EmitPush(newValue);
                builder.Emit(OpCode.SETITEM);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildPickItemScript(IReadOnlyList<int> values, int index) =>
            BuildScript(builder =>
            {
                EmitArrayLiteral(builder, values);
                builder.EmitPush(index);
                builder.Emit(OpCode.PICKITEM);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildKeysScript(IReadOnlyList<KeyValuePair<int, int>> map) =>
            BuildScript(builder =>
            {
                EmitMapLiteral(builder, map);
                builder.Emit(OpCode.KEYS);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildValuesScript(IReadOnlyList<KeyValuePair<int, int>> map) =>
            BuildScript(builder =>
            {
                EmitMapLiteral(builder, map);
                builder.Emit(OpCode.VALUES);
                builder.Emit(OpCode.DROP);
            });

        private static byte[] BuildHasKeyScript(IReadOnlyList<KeyValuePair<int, int>> map, int key)
        {
            return BuildScript(builder =>
            {
                EmitMapLiteral(builder, map);
                builder.EmitPush(key);
                builder.Emit(OpCode.HASKEY);
                builder.Emit(OpCode.DROP);
            });
        }

        private static byte[] BuildRemoveScript(IReadOnlyList<KeyValuePair<int, int>> map, int key)
        {
            return BuildScript(builder =>
            {
                EmitMapLiteral(builder, map);
                builder.EmitPush(key);
                builder.Emit(OpCode.REMOVE);
            });
        }

        private static byte[] BuildPopItemScript(IReadOnlyList<int> values)
        {
            return BuildScript(builder =>
            {
                EmitArrayLiteral(builder, values);
                builder.Emit(OpCode.POPITEM);
                builder.Emit(OpCode.DROP);
            });
        }

        private static byte[] BuildClearItemsScript(IReadOnlyList<int> values)
        {
            return BuildScript(builder =>
            {
                EmitArrayLiteral(builder, values);
                builder.Emit(OpCode.CLEARITEMS);
            });
        }

        #endregion

        #region Helpers

        private static void ExecuteCase(OpcodeCase @case)
        {
            using var engine = @case.EngineFactory?.Invoke() ?? new ExecutionEngine();
            engine.LoadScript(@case.Script);
            var state = engine.Execute();
            if (state != VMState.HALT)
                throw new InvalidOperationException($"Benchmark case '{@case.Name}' ended with VM state {state}.");
        }

        private static byte[] BuildScript(Action<ScriptBuilder> emitter)
        {
            using var builder = new ScriptBuilder();
            emitter(builder);
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static void DropMany(ScriptBuilder builder, int count)
        {
            for (int i = 0; i < count; i++)
                builder.Emit(OpCode.DROP);
        }

        private static void EmitArrayLiteral(ScriptBuilder builder, IReadOnlyList<int> values)
        {
            if (values.Count == 0)
            {
                builder.Emit(OpCode.NEWARRAY0);
                return;
            }

            for (int i = values.Count - 1; i >= 0; i--)
                builder.EmitPush(values[i]);
            builder.EmitPush(values.Count);
            builder.Emit(OpCode.PACK);
        }

        private static void EmitStructLiteral(ScriptBuilder builder, IReadOnlyList<int> values)
        {
            if (values.Count == 0)
            {
                builder.Emit(OpCode.NEWSTRUCT0);
                return;
            }

            for (int i = values.Count - 1; i >= 0; i--)
                builder.EmitPush(values[i]);
            builder.EmitPush(values.Count);
            builder.Emit(OpCode.PACKSTRUCT);
        }

        private static void EmitMapLiteral(ScriptBuilder builder, IReadOnlyList<KeyValuePair<int, int>> entries)
        {
            if (entries.Count == 0)
            {
                builder.Emit(OpCode.NEWMAP);
                return;
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var (key, value) = entries[i];
                builder.EmitPush(value);
                builder.EmitPush(key);
            }
            builder.EmitPush(entries.Count);
            builder.Emit(OpCode.PACKMAP);
        }

        private static int[] GenerateSequentialInts(int count) =>
            Enumerable.Range(0, count).ToArray();

        private static KeyValuePair<int, int>[] GenerateMap(int count) =>
            Enumerable.Range(0, count)
                .Select(i => new KeyValuePair<int, int>(i, i * 2))
                .ToArray();

        private static byte[] GenerateByteArray(int length)
        {
            var data = new byte[length];
            for (int i = 0; i < length; i++)
                data[i] = (byte)(i % 251);
            return data;
        }

        #endregion
    }
}
