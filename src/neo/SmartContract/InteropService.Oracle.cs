using Neo.Oracle;
using Neo.Oracle.Protocols.Https;
using System;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Oracle
        {
            public static readonly uint Neo_Oracle_Get = Register("Neo.Oracle.Get", Oracle_Get, 0, TriggerType.Application, CallFlags.None);

            private static bool Oracle_Get(ApplicationEngine engine)
            {
                if (engine.OracleCache == null) return false;
                if (!engine.TryPop(out string urlItem) || !Uri.TryCreate(urlItem, UriKind.Absolute, out var url)) return false;
                if (!engine.TryPop(out string filter)) return false;

                OracleRequest request;
                switch (url.Scheme.ToLowerInvariant())
                {
                    case "https":
                        {
                            request = new OracleHttpsRequest()
                            {
                                Method = HttpMethod.GET,
                                URL = url,
                                Filter = filter
                            };
                            break;
                        }
                    default: return false;
                }

                if (engine.OracleCache.TryGet(request, out var response))
                {
                    engine.Push(response.ToStackItem(engine.ReferenceCounter));
                    return true;
                }

                return false;
            }
        }
    }
}
