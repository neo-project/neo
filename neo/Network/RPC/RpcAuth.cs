using System;

namespace Neo.Network.RPC
{
    public class RpcAuth
    {
        public enum AuthType { Basic };
        public readonly AuthType Type;
        private readonly string rpcuser;
        private readonly string rpcpassword;

        public RpcAuth(RPC.RpcAuth.AuthType type, string user, string password)
        {
            this.rpcuser = user;
            this.rpcpassword = password;
            this.Type = type;
        }

        public bool CheckBasicAuth(string authentication)
        {
            if (string.IsNullOrEmpty(this.rpcuser) || string.IsNullOrEmpty(this.rpcpassword))
                return false;
            string authstring = null;
            try
            {
                authstring = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(authentication.Replace("Basic ", "").Trim()));
            }
            catch
            {
                return false;
            }
            string[] authvalues = authstring.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (authvalues.Length < 2)
                return false;

            return authvalues[0] == this.rpcuser && authvalues[1] == this.rpcpassword;   
        }
    }
}
