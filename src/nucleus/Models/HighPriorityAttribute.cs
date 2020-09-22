namespace Neo.Models
{
    public class HighPriorityAttribute : TransactionAttribute
    {
        public override bool AllowMultiple => false;
    }
}
