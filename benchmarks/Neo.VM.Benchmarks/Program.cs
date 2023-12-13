using System.Reflection;
using Neo.VM;

foreach (var method in typeof(Benchmarks).GetMethods(BindingFlags.Public | BindingFlags.Static))
{
    method.CreateDelegate<Action>().Invoke();
}
