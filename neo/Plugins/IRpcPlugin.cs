using Microsoft.AspNetCore.Http;
using Neo.IO.Json;

namespace Neo.Plugins
{
    public interface IRpcPlugin
    {
        JObject OnProcess(HttpContext context, string method, JArray _params);
    }
}
