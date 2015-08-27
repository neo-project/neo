using AntShares.IO;

namespace AntShares.Core
{
    public interface ISignable : ISerializable
    {
        byte[][] Scripts { get; set; }

        void FromUnsignedArray(byte[] value);
        byte[] GetHashForSigning();
        UInt160[] GetScriptHashesForVerifying();
        byte[] ToUnsignedArray();
        VerificationResult Verify();
    }
}
