namespace Neo.Test.Types
{
    public enum VMUTStackItemType
    {
        /// <summary>
        /// Null
        /// </summary>
        Null,

        /// <summary>
        /// An address of function
        /// </summary>
        Pointer,

        /// <summary>
        /// Boolean (true,false)
        /// </summary>
        Boolean,

        /// <summary>
        /// ByteString
        /// </summary>
        ByteString,

        /// <summary>	
        /// ByteString as UTF8 string	
        /// </summary>
        String,

        /// <summary>
        /// Mutable byte array
        /// </summary>
        Buffer,

        /// <summary>
        /// InteropInterface
        /// </summary>
        Interop,

        /// <summary>
        /// BigInteger
        /// </summary>
        Integer,

        /// <summary>
        /// Array
        /// </summary>
        Array,

        /// <summary>
        /// Struct
        /// </summary>
        Struct,

        /// <summary>
        /// Map
        /// </summary>
        Map
    }
}
