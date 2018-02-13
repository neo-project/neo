using Neo.IO.Json;

namespace Neo.Plugins
{
    public class NeoRpcPlugin : NeoPlugin
    {
        /// <summary>
        /// Execute Rpc Call
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <returns>Return restulting object</returns>
        public virtual JObject RpcCall(NeoRpcPluginArgs args)
        {
            return null;
        }
    }
}