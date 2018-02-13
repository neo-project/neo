using Neo.IO.Json;

namespace Neo.Plugins
{
    public class NeoRpcPluginArgs
    {
        /// <summary>
        /// Handle
        /// </summary>
        public bool Handle { get; set; } = false;
        /// <summary>
        /// Params
        /// </summary>
        public JArray Params { get; set; }
        /// <summary>
        /// Method
        /// </summary>
        public string Method { get; set; }
    }
}