using System.Text;

namespace Neo
{
    public static class Encoding
    {
        public static readonly UTF8Encoding StrictUTF8 = new UTF8Encoding(false, true);
    }
}
