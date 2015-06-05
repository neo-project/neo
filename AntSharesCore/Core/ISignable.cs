namespace AntShares.Core
{
    internal interface ISignable
    {
        byte[] GetHashForSigning();

        UInt160[] GetScriptHashesForVerifying();

        byte[][] GetScriptsForVerifying();
    }
}
