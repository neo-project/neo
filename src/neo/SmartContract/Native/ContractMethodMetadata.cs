using Neo.Persistence;
using System;
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

        public ContractMethodMetadata(MemberInfo member, ContractMethodAttribute attribute)
        {
            this.Handler = member switch
            {
                MethodInfo m => m,
                PropertyInfo p => p.GetMethod,
                _ => throw new ArgumentException(nameof(member))
            };
            this.Name = attribute.Name ?? GetDefaultMethodName(member.Name);
            ParameterInfo[] parameterInfos = this.Handler.GetParameters();
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
            return name.ToLower()[0] + name[1..];
        }
    }
}
