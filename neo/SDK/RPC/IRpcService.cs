using System.Threading.Tasks;

namespace Neo.SDK.RPC
{
    public interface IRpcService
    {
        Task<T> SendAsync<T>(object request);

        T Send<T>(object request);
    }
}
