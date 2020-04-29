namespace Neo.SmartContract.NNS
{
    public enum RecordType : byte
    {
        A = 0x00,        // contract/account address
        CNAME = 0x01,    // domain redirection
        TXT = 0x02,      // domain additional information
        NS = 0x03,       // specify the subdomain interpreter for this domain name (wildcard matching)
        ERROR = 0x04
    }
}
