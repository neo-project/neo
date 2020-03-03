using Neo.Oracle.Protocols.Https;
using System;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Oracle
        {
            public static readonly uint System_Oracle_Get = Register("System.Oracle.Get", Oracle_Get, 0, TriggerType.Application, CallFlags.None);

            private static bool Oracle_Get(ApplicationEngine engine)
            {
                if (engine.OracleCache == null) return false;
                if (!engine.TryPop(out string urlItem) || !Uri.TryCreate(urlItem, UriKind.Absolute, out var url)) return false;
                if (!engine.TryPop(out string filter)) return false;
                if (url.Scheme != "https") return false;

                var request = new OracleHttpsRequest()
                {
                    Method = HttpMethod.GET,
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
