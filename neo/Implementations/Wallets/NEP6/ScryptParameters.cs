using Neo.IO.Json;

namespace Neo.Implementations.Wallets.NEP6
{
    public class ScryptParameters
    {
        public static ScryptParameters Default { get; } = new ScryptParameters(16384, 8, 8);

        public readonly int N, R, P;

        public ScryptParameters(int n, int r, int p)
        {
            this.N = n;
            this.R = r;
            this.P = p;
        }

        public static ScryptParameters FromJson(JObject json)
        {
            return new ScryptParameters((int)json["n"].AsNumber(), (int)json["r"].AsNumber(), (int)json["p"].AsNumber());
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["n"] = N;
            json["r"] = R;
            json["p"] = P;
            return json;
        }
    }
}
