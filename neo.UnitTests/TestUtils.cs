namespace Neo.UnitTests
{
    public static class TestUtils
    {
        public static byte[] GetByteArray(int length, byte firstByte)
        {            
            byte[] array = new byte[length];
            array[0] = firstByte;
            for (int i = 1; i < length; i++)
            {
                array[i] = 0x20;
            }
            return array;
        }
    }
}
