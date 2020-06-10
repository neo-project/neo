using Neo.Persistence;
using System.Linq;
using System.Reflection;

namespace Neo.SmartContract.Native
{
    internal class ContractMethodMetadata
    {
        public string Name { get; }
        public MethodInfo Handler { get; }
        public InteropParameterDescriptor[] Parameters { get; }
        public bool NeedApplicationEngine { get; }
        public bool NeedSnapshot { get; }
        public long Price { get; }
        public CallFlags RequiredCallFlags { get; }

        public ContractMethodMetadata(MethodInfo handler, ContractMethodAttribute attribute)
        {
            this.Name = attribute.Name ?? GetDefaultMethodName(handler.Name);
            this.Handler = handler;
            ParameterInfo[] parameterInfos = handler.GetParameters();
            if (parameterInfos.Length > 0)
            {
                NeedApplicationEngine = parameterInfos[0].ParameterType.IsAssignableFrom(typeof(ApplicationEngine));
                NeedSnapshot = parameterInfos[0].ParameterType.IsAssignableFrom(typeof(StoreView));
            }
            if (NeedApplicationEngine || NeedSnapshot)
                this.Parameters = parameterInfos.Skip(1).Select(p => new InteropParameterDescriptor(p)).ToArray();
            else
                this.Parameters = parameterInfos.Select(p => new InteropParameterDescriptor(p)).ToArray();
            this.Price = attribute.Price;
            this.RequiredCallFlags = attribute.RequiredCallFlags;
        }

        private static string GetDefaultMethodName(string name)
        {
            if (name.StartsWith("get_")) name = name[4..];
            return name.ToLower()[0] + name[1..];
        }
    }
}
