# Cache query and opcode price-table performance

## Scope

This change optimizes `DynamicPriceTable.Clone()` and the in-memory work performed
by `DataCache.Seek()`.

`DynamicPriceTable.Clone()` shares its backing array until either table is
modified. A write on a shared table copies the 256-entry array under the table
lock, then subsequent writes update the table in place. Cloning and reads can
run concurrently; mutations on one table are serialized. Delegate targets are
not cloned, matching the previous shallow-copy behavior. Opcode prices,
rounding, hardfork activation, and the public API are unchanged.

`DataCache.Seek()` captures the selected key/item pairs and shadowing keys under
the existing dictionary lock. It initializes the selected keys' lazy serialized
buffers while holding the lock, then sorts the captured array outside the lock.
The merge, deletion shadowing, ordering, skip handling, and lazy enumeration
semantics remain unchanged.

## Benchmark method

The comparison used separately built assemblies from `origin/master-n3` and the
patched branch at the same time. Both runs used the same Release configuration,
.NET 10, macOS ARM64, the same process harness, 21 samples, and median values.
`DataCache.Seek()` used an empty backing `MemoryStore` and 1,000, 10,000, or
100,000 cached entries. The concurrent case used eight workers, each performing
the same number of first-result seeks. The benchmark sources are checked in at:

- `benchmarks/Neo.Benchmarks/SmartContract/Benchmarks.DynamicPriceTable.cs`
- `benchmarks/Neo.Benchmarks/Persistence/Benchmarks.DataCacheSeek.cs`

## Results

| Case | `origin/master-n3` | Patched | Change |
| --- | ---: | ---: | ---: |
| `DynamicPriceTable.Clone` | 107.80 ns / 2,096 B | 15.40 ns / 64 B | 85.7% faster / 96.9% less allocation |
| `DynamicPriceTable.CloneThenWrite` | 93.00 ns / 2,096 B | 102.70 ns / 2,136 B | 10.4% slower / 1.9% more allocation |
| `DataCache.SeekFirst`, 1,000 entries | 271.23 us / 118,336 B | 311.37 us / 90,112 B | 24.0% less allocation |
| `DataCache.SeekFirst`, 10,000 entries | 1.149 ms / 1,115,045 B | 0.883 ms / 834,684 B | 23.2% faster / 25.1% less allocation |
| `DataCache.SeekFirst`, 100,000 entries | 15.773 ms / 10,440,368 B | 11.184 ms / 7,638,528 B | 29.1% faster / 26.8% less allocation |
| Concurrent seek, 8 workers, 1,000 entries | 22.73 ms | 7.41 ms | 67.4% faster |
| Concurrent seek, 8 workers, 10,000 entries | 84.32 ms | 36.88 ms | 56.3% faster |
| Concurrent seek, 8 workers, 100,000 entries | 234.09 ms | 88.24 ms | 62.3% faster |

The `CloneThenWrite` result includes the intentional first write copy. The
clone optimization targets the much more frequent default-engine construction
path; callers that immediately customize a cloned table pay the existing
copy cost plus the table synchronization. The concurrent seek results measure
lock contention in this in-memory workload and should not be used as node-wide
throughput projections.

## Validation

The change includes unit tests for clone isolation, concurrent clone/update
behavior, static-price fallback after a null override, layered cache merge
ordering, skip behavior, active enumerators, and concurrent cold seeks.

No data migration, configuration change, or deployment step is required.
