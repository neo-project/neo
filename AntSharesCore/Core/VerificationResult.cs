using System;

namespace AntShares.Core
{
    [Flags]
    public enum VerificationResult : byte
    {
        OK = 0,

        IncorrectFormat = 0x01,
        Incapable = 0x02,
        LackOfInformation = 0x04,
        DoubleSpent = 0x08,
        Overissue = 0x10,
        InvalidSignature = 0x20,
        Imbalanced = 0x40,
        WrongMiner = 0x80
    }
}
