namespace AntShares.Core
{
    public interface ISignable
    {
        void FromUnsignedArray(byte[] value);

        byte[] GetHashForSigning();

        UInt160[] GetScriptHashesForVerifying();

        byte[][] GetScriptsForVerifying();

        byte[] ToUnsignedArray();
    }
}
