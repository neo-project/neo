using Neo.Oracle.Protocols.HTTPS;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Oracle
        {
            public static readonly uint Neo_Oracle_HTTPS11_Get = Register("Oracle.HTTPS.Get", Oracle_HTTPS_Get, 0, TriggerType.Application, CallFlags.None);

            private static bool Oracle_HTTPS_Get(ApplicationEngine engine)
            {
                if (engine.OracleCache == null) return false;
                if (!engine.TryPop(out string url)) return false;
                if (!engine.TryPop(out string filter)) return false;

                var request = new OracleHTTPSRequest()
                {
                    Method = OracleHTTPSRequest.HTTPMethod.GET,
                    URL = url,
                    Filter = filter,
                    Body = null
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
