// using Neo.IO;
namespace Neo.Models
{
    public abstract class TransactionAttribute 
    {
        public abstract bool AllowMultiple { get; }
    }
}
