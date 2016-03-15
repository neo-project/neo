namespace AntShares.Core.Scripts
{
    internal enum InterfaceOp : byte
    {
        //System
        SYSTEM_NOW = 0x00,
        SYSTEM_CURRENTTX = 0x0a,

        //Account

        //Blockchain
        CHAIN_HEIGHT = 0x20,
        CHAIN_GETHEADER = 0x21,
        CHAIN_GETBLOCK = 0x22,
        CHAIN_GETTX = 0x23,

        //Header & Block
        HEADER_HASH = 0x30,
        HEADER_VERSION = 0x31,
        HEADER_PREVHASH = 0x32,
        HEADER_MERKLEROOT = 0x33,
        HEADER_TIMESTAMP = 0x34,
        HEADER_NONCE = 0x35,
        HEADER_NEXTMINER = 0x36,
        BLOCK_TXCOUNT = 0x3a,
        BLOCK_TX = 0x3b,
        BLOCK_GETTX = 0x3c,

        //Transaction & Asset & Enrollment & Vote
        TX_HASH = 0x40,
        TX_TYPE = 0x41,
        ASSET_TYPE = 0x42,
        ASSET_AMOUNT = 0x43,
        ASSET_ISSUER = 0x44,
        ASSET_ADMIN = 0x45,
        ENROLL_PUBKEY = 0x46,
        VOTE_ENROLLMENTS = 0x47,
        TX_ATTRIBUTES = 0x4a,
        TX_INPUTS = 0x4b,
        TX_OUTPUTS = 0x4c,

        //ATTRIBUTE
        ATTR_USAGE = 0x50,
        ATTR_DATA = 0x51,

        //INPUT
        TXIN_HASH = 0x60,
        TXIN_INDEX = 0x61,

        //OUTPUT
        TXOUT_ASSET = 0x70,
        TXOUT_VALUE = 0x71,
        TXOUT_SCRIPTHASH = 0x72,
    }
}
