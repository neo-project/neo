using Neo.VM;
using System.Reflection;

foreach (var method in typeof(Benchmarks).GetMethods(BindingFlags.Public | BindingFlags.Static))
{
    method.CreateDelegate<Action>().Invoke();
}
