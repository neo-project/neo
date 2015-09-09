namespace AntShares.Network
{
    public enum InventoryType : uint
    {
        TX = 0x01,
        Block = 0x02,

        ConsRequest = 0xe1,
        ConsResponse = 0xe2
    }
}
