using Neo.IO.Json;
using System;

namespace Neo.SmartContract
{
    public class ScriptHeader
    {
        public enum ScriptEngine : byte
        {
            NeoVM = 0x01
        }

        /// <summary>
        /// Compiler
        /// </summary>
        public string Compiler { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Engine
        /// </summary>
        public ScriptEngine Engine { get; set; }

        /// <summary>
        /// Parse ScriptHeader from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ScriptHeader</returns>
        public static ScriptHeader FromJson(JObject json)
        {
            return new ScriptHeader
            {
                Compiler = json["compiler"].AsString(),
                Version = Version.Parse(json["version"].AsString()),
                Engine = (ScriptEngine)Enum.Parse(typeof(ScriptEngine), json["engine"].AsString())
            };
        }

        /// <summary
        /// To json
        /// </summary>
        public JObject ToJson()
        {
            var json = new JObject();
            json["version"] = Version.ToString();
            json["compiler"] = Compiler;
            json["engine"] = Engine.ToString();
            return json;
        }
    }
}
