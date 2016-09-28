namespace AntShares.Core.Scripts
{
    public interface IApiService
    {
        bool Invoke(string method, ScriptEngine engine);
    }
}
