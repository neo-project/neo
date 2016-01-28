using AntShares.Miner;

namespace AntShares
{
    static class Program
    {
        static void Main(string[] args)
        {
            new MinerService().Run(args);
        }
    }
}
