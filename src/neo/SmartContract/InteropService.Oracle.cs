using Neo.Oracle.Protocols.HTTP;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Oracle
        {
            public static readonly uint Neo_Oracle_HTTP11_Get = Register("Oracle.HTTP11.Get", Oracle_HTTP11_Get, 0, TriggerType.Application, CallFlags.None);

            private static bool Oracle_HTTP11_Get(ApplicationEngine engine)
            {
                if (engine.OracleCache == null) return false;
                if (!engine.TryPop(out string url)) return false;
                if (!engine.TryPop(out string filter)) return false;

                var request = new OracleHTTPRequest()
                {
                    Method = OracleHTTPRequest.HTTPMethod.GET,
                    URL = url,
                    Filter = filter,
                    Body = null,
                    Version = OracleHTTPRequest.HTTPVersion.v1_1
                };

                if (engine.OracleCache.TryGet(request, out var response))
                {
                    engine.CurrentContext.EvaluationStack.Push(response.ToStackItem(engine.ReferenceCounter));
                    return true;
                }

                return false;
            }
        }
    }
}
