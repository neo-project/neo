namespace Neo.Models
{
    public interface IWitnessed : ISignable
    {
        Witness[] Witnesses { get; }
    }
}
