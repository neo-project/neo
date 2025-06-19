# Neo Persistence System - Class Relationships

## Interface Hierarchy

```
IDisposable
    │
    ├── IReadOnlyStore<TKey,TValue>
    ├── IWriteStore<TKey,TValue>
    └── IStoreProvider
            │
            ▼
        IStore ◄─── IStoreSnapshot
```

## Class Structure

```
StoreFactory
    │
    ├── MemoryStoreProvider ──creates──> MemoryStore
    ├── LevelDBStore (Plugin) ──creates──> LevelDBStore
    └── RocksDBStore (Plugin) ──creates──> RocksDBStore
                                              │
                                              ▼
                                        IStoreSnapshot
                                              │
                                              ▼
                                         Cache Layer
                                              │
                                    ┌─────────┼─────────┐
                                    │         │         │
                               DataCache  StoreCache  ClonedCache
```

## Interface Definitions

### IStore
```csharp
public interface IStore : IReadOnlyStore<byte[], byte[]>, IWriteStore<byte[], byte[]>, IDisposable
{
    IStoreSnapshot GetSnapshot();
}
```

### IStoreSnapshot
```csharp
public interface IStoreSnapshot : IReadOnlyStore<byte[], byte[]>, IWriteStore<byte[], byte[]>, IDisposable
{
    IStore Store { get; }
    void Commit();
}
```

### IReadOnlyStore<TKey, TValue>
```csharp
public interface IReadOnlyStore<TKey, TValue> where TKey : class?
{
    TValue this[TKey key] { get; }
    bool TryGet(TKey key, out TValue? value);
    bool Contains(TKey key);
    IEnumerable<(TKey Key, TValue Value)> Find(TKey? key_prefix = null, SeekDirection direction = SeekDirection.Forward);
}
```

### IWriteStore<TKey, TValue>
```csharp
public interface IWriteStore<TKey, TValue>
{
    void Delete(TKey key);
    void Put(TKey key, TValue value);
    void PutSync(TKey key, TValue value) => Put(key, value);
}
```

### IStoreProvider
```csharp
public interface IStoreProvider
{
    string Name { get; }
    IStore GetStore(string path);
}
```

## Core Classes

### StoreFactory
- Static registry for storage providers
- Manages provider registration and discovery
- Creates store instances

## Cache System

### Why Three Cache Classes?

The Neo persistence system uses three cache classes to separate different responsibilities:

1. **DataCache** - Provides common caching infrastructure and change tracking
2. **StoreCache** - Connects cache to actual storage (database/memory)
3. **ClonedCache** - Creates isolated copies to prevent data corruption

### Relationships

```
DataCache (Abstract)
    │
    ├── StoreCache ──connects to──> IStore/IStoreSnapshot
    └── ClonedCache ──wraps──> Any DataCache
```

### When to Use Each

**StoreCache**:
- Direct access to storage
- When you need to read/write to database
- Base layer for other caches

**ClonedCache**:
- When you need isolated data manipulation
- Preventing accidental mutations between components
- Creating temporary working environments
- Smart contract execution (isolated from main state)
