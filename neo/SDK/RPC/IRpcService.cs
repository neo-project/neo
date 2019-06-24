using Neo.IO.Json;
using System.Threading.Tasks;

namespace Neo.SDK.RPC
{
    public interface IRpcService
    {
        Task<JObject> SendAsync(object request);

        JObject Send(object request);
    }
}
