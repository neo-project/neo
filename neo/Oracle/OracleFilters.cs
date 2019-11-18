using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace Neo.Oracle
{
    public class OracleFilters
    {
        /// <summary>
        /// Filter json
        /// </summary>
        /// <param name="input">Input</param>
        /// <param name="filter">Filter</param>
        /// <param name="output">output</param>
        /// <returns>True if was filtered</returns>
        public static bool FilterJson(string input, string filter, out string output)
        {
            var array = false;
            var sb = new StringBuilder();
            var json = JObject.Parse(input);

            foreach (var token in json.SelectTokens(filter))
            {
                if (sb.Length > 0)
                {
                    array = true;
                    sb.Append(",\n");
                }
                sb.Append(token.ToString(Newtonsoft.Json.Formatting.None));
            }

            output = array ? "[" + sb.ToString() + "]" : sb.ToString();
            return true;
        }

        /// <summary>
        /// Filter XML using XPath filters
        /// </summary>
        /// <param name="input">Input</param>
        /// <param name="filter">Filter</param>
        /// <param name="output">output</param>
        /// <returns>True if was filtered</returns>
        public static bool FilterXml(string input, string filter, out string output)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings() { XmlResolver = null }))
            {
                var doc = new XPathDocument(reader);
                var nav = doc.CreateNavigator();
                var node = nav.Select(filter);

                var sb = new StringBuilder();
                while (node.MoveNext())
                {
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append(node.Current.Value);
                }

                output = sb.ToString();
                return true;
            }
        }

        /// <summary>
        /// Filter text
        /// </summary>
        /// <param name="input">Input</param>
        /// <param name="filter">Filter</param>
        /// <param name="output">output</param>
        /// <returns>True if was filtered</returns>
        public static bool FilterText(string input, string filter, out string output)
        {
            var regex = new Regex(filter, RegexOptions.ExplicitCapture | RegexOptions.Multiline);

            var sb = new StringBuilder();
            foreach (Match match in regex.Matches(input))
            {
                if (!match.Success) throw new ArgumentException(nameof(filter));
                foreach (Group group in match.Groups)
                {
                    if (!group.Success) throw new ArgumentException(nameof(filter));

                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append(group.Value);
                }
            }

            output = sb.ToString();
            return true;
        }

        /// <summary>
        /// Filter string according to the media type
        /// </summary>
        /// <param name="contentType">Content type</param>
        /// <param name="input">Input</param>
        /// <param name="filter">Filter</param>
        /// <param name="output">output</param>
        /// <returns>True if was filtered</returns>
        public static bool FilterContent(string contentType, string input, string filter, out string output)
        {
            if (string.IsNullOrEmpty(filter))
            {
                output = input;
                return true;
            }

            output = null;

            return (contentType) switch
            {
                "application/xml" => FilterXml(input, filter, out output),
                "text/xml" => FilterXml(input, filter, out output),
                "application/json" => FilterJson(input, filter, out output),
                "text/plain" => FilterText(input, filter, out output),
                "text/html" => FilterText(input, filter, out output),
                _ => false,
            };
        }
    }
}
