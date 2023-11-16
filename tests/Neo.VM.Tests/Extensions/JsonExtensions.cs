using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Neo.Test.Extensions
{
    public static class JsonExtensions
    {
        private static readonly JsonSerializerSettings _settings;

        /// <summary>
        /// Static constructor
        /// </summary>
        static JsonExtensions()
        {
            _settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            _settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
        }

        /// <summary>
        /// Deserialize json to object
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="input">Json</param>
        /// <returns>Unit test</returns>
        public static T DeserializeJson<T>(this string input)
        {
            return JsonConvert.DeserializeObject<T>(input, _settings);
        }

        /// <summary>
        /// Serialize UT to json
        /// </summary>
        /// <param name="ut">Unit test</param>
        /// <returns>Json</returns>
        public static string ToJson(this object ut)
        {
            return JsonConvert.SerializeObject(ut, _settings);
        }
    }
}
