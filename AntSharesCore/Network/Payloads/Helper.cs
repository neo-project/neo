namespace AntShares.Network.Payloads
{
    internal static class Helper
    {
        //要保证返回的字符串经UTF8编码后，长度小于或等于12个字节
        public static string GetCommandName(this InventoryType type)
        {
            //TODO: 暂时返回枚举名称，未来可能会有枚举名称与命令名称不一致的情况，需要单独处理
            return type.ToString().ToLower();
        }
    }
}
