using Neo.Oracle.Protocols.HTTP1;

namespace Neo.SmartContract
{
    static partial class InteropService
    {
        public static readonly uint Neo_Oracle_HTTP1_Get = Register("Oracle.HTTP1.Get", Oracle_HTTP1_Get, 0, TriggerType.Application);
        public static readonly uint Neo_Oracle_HTTP1_Post = Register("Oracle.HTTP1.Post", Oracle_HTTP1_Post, 0, TriggerType.Application);
        public static readonly uint Neo_Oracle_HTTP1_Delete = Register("Oracle.HTTP1.Delete", Oracle_HTTP1_Delete, 0, TriggerType.Application);
        public static readonly uint Neo_Oracle_HTTP1_Put = Register("Oracle.HTTP1.Put", Oracle_HTTP1_Put, 0, TriggerType.Application);

        private static bool Oracle_HTTP1_Get(ApplicationEngine engine)
        {
            var filter = engine.CurrentContext.EvaluationStack.Pop().GetString();
            var url = engine.CurrentContext.EvaluationStack.Pop().GetString();

            return Oracle_HTTP1(engine, OracleHTTP1Method.GET, url, filter, null);
        }

        private static bool Oracle_HTTP1_Post(ApplicationEngine engine)
        {
            var body = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            var filter = engine.CurrentContext.EvaluationStack.Pop().GetString();
            var url = engine.CurrentContext.EvaluationStack.Pop().GetString();

            return Oracle_HTTP1(engine, OracleHTTP1Method.POST, url, filter, body);
        }

        private static bool Oracle_HTTP1_Delete(ApplicationEngine engine)
        {
            var filter = engine.CurrentContext.EvaluationStack.Pop().GetString();
            var url = engine.CurrentContext.EvaluationStack.Pop().GetString();

            return Oracle_HTTP1(engine, OracleHTTP1Method.DELETE, url, filter, null);
        }

        private static bool Oracle_HTTP1_Put(ApplicationEngine engine)
        {
            var body = engine.CurrentContext.EvaluationStack.Pop().GetByteArray();
            var filter = engine.CurrentContext.EvaluationStack.Pop().GetString();
            var url = engine.CurrentContext.EvaluationStack.Pop().GetString();

            return Oracle_HTTP1(engine, OracleHTTP1Method.PUT, url, filter, body);
        }

        private static bool Oracle_HTTP1(ApplicationEngine engine, OracleHTTP1Method method, string url, string filter, byte[] body)
        {
            var request = new OracleHTTP1Request()
            {
                Method = method,
                URL = url,
                Filter = filter,
                Body = body
            };

            // Extract from cache

            if (engine.OracleCache != null &&
                engine.OracleCache.TryGet(request, out var response))
            {
                engine.CurrentContext.EvaluationStack.Push(response.ToStackItem());
                return true;
            }

            return false;
        }
    }
}
