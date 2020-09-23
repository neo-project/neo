using SysEncoding = System.Text.Encoding;

namespace Neo
{
    public static class Encoding
    {
        public static readonly SysEncoding StrictUTF8 = new System.Text.UTF8Encoding(false, true);
        public static SysEncoding UTF8 => SysEncoding.UTF8;
        public static SysEncoding ASCII => SysEncoding.ASCII;
        public static SysEncoding Default => SysEncoding.Default;
    }
}
