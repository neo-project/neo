using System;

namespace AntShares.Network
{
    internal class Mission : IComparable<Mission>
    {
        public UInt256 Hash;
        public InventoryType Type;
        public int LaunchTimes;

        public int CompareTo(Mission other)
        {
            return this.LaunchTimes.CompareTo(other.LaunchTimes);
        }
    }
}
