using System.Net.Http.Headers;

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
            // TODO: Filter

            output = input;
            return true;
        }

        /// <summary>
        /// Filter html
        /// </summary>
        /// <param name="input">Input</param>
        /// <param name="filter">Filter</param>
        /// <param name="output">output</param>
        /// <returns>True if was filtered</returns>
        public static bool FilterHtml(string input, string filter, out string output)
        {
            // TODO: Filter

            output = input;
            return true;
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
            // TODO: Filter

            output = input;
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
        public static bool FilterContent(MediaTypeHeaderValue contentType, string input, string filter, out string output)
        {
            if (string.IsNullOrEmpty(filter))
            {
                output = input;
                return true;
            }

            output = null;

            return (contentType.MediaType.ToLowerInvariant()) switch
            {
                "application/json" => FilterJson(input, filter, out output),
                "text/html" => FilterHtml(input, filter, out output),
                "text/plain" => FilterText(input, filter, out output),
                _ => false,
            };
        }
    }
}
