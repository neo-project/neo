namespace Neo.Cryptography.BLS12_381;

interface IMillerLoopDriver<T>
{
    public T DoublingStep(in T f);
    public T AdditionStep(in T f);
    public T Square(in T f);
    public T Conjugate(in T f);
    public T One { get; }
}
