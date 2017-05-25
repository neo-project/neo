namespace AntShares.Core
{
    public interface ICloneable<T>
    {
        T Clone();
        void FromReplica(T replica);
    }
}
