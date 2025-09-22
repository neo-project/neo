// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngine.Fees.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.SmartContract
{
    public readonly struct ResourceCost
    {
        public long CpuUnits { get; }
        public long MemoryBytes { get; }
        public long StorageBytes { get; }

        public static ResourceCost Zero => default;

        public ResourceCost(long cpuUnits, long memoryBytes, long storageBytes)
        {
            if (cpuUnits < 0) throw new ArgumentOutOfRangeException(nameof(cpuUnits));
            if (memoryBytes < 0) throw new ArgumentOutOfRangeException(nameof(memoryBytes));
            if (storageBytes < 0) throw new ArgumentOutOfRangeException(nameof(storageBytes));

            CpuUnits = cpuUnits;
            MemoryBytes = memoryBytes;
            StorageBytes = storageBytes;
        }

        public ResourceCost Add(ResourceCost other)
        {
            checked
            {
                return new ResourceCost(
                    CpuUnits + other.CpuUnits,
                    MemoryBytes + other.MemoryBytes,
                    StorageBytes + other.StorageBytes);
            }
        }

        public bool IsZero => CpuUnits == 0 && MemoryBytes == 0 && StorageBytes == 0;

        public static ResourceCost FromCpu(long cpuUnits) => new(cpuUnits, 0, 0);

        public static ResourceCost FromMemory(long memoryBytes) => new(0, memoryBytes, 0);

        public static ResourceCost FromStorage(long storageBytes) => new(0, 0, storageBytes);
    }

    partial class ApplicationEngine
    {
        protected internal void AddResourceCost(ResourceCost resourceCost)
        {
            if (resourceCost.IsZero)
                return;

            checked
            {
                long datoshi = resourceCost.CpuUnits * ExecFeeFactor
                                + resourceCost.MemoryBytes * MemoryFeeFactor
                                + resourceCost.StorageBytes * StoragePrice;
                if (datoshi != 0)
                    AddFee(datoshi);
            }
        }

        protected internal void ChargeCpu(long cpuUnits)
        {
            if (cpuUnits < 0) throw new ArgumentOutOfRangeException(nameof(cpuUnits));
            if (cpuUnits == 0) return;
            AddResourceCost(ResourceCost.FromCpu(cpuUnits));
        }

        protected internal void ChargeMemory(long memoryBytes)
        {
            if (memoryBytes < 0) throw new ArgumentOutOfRangeException(nameof(memoryBytes));
            if (memoryBytes == 0) return;
            AddResourceCost(ResourceCost.FromMemory(memoryBytes));
        }

        protected internal void ChargeStorage(long storageBytes)
        {
            if (storageBytes < 0) throw new ArgumentOutOfRangeException(nameof(storageBytes));
            if (storageBytes == 0) return;
            AddResourceCost(ResourceCost.FromStorage(storageBytes));
        }
    }
}

