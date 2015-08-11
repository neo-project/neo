using AntShares.Services;

namespace AntShares
{
    internal class MainService : ConsoleServiceBase
    {
        protected override string Prompt => "ant";

        public override string ServiceName => "AntSharesMiner";

        protected internal override void OnStart()
        {
        }

        protected internal override void OnStop()
        {
        }
    }
}
