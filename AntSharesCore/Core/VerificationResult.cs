using System;

namespace AntShares.Core
{
    [Flags]
    public enum VerificationResult : ushort
    {
        OK = 0,

        AlreadyInBlockchain = 0x0001,
        Incapable = 0x0002,
        LackOfInformation = 0x0004,
        IncorrectFormat = 0x0008,
        InvalidSignature = 0x0010,
        WrongMiner = 0x0020,
        DoubleSpent = 0x0040,
        Overissue = 0x0080,
        Imbalanced = 0x0100
    }
}
