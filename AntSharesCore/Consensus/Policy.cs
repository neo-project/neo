using AntShares.Wallets;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Consensus
{
    internal class Policy
    {
        public PolicyLevel PolicyLevel { get; private set; }
        public HashSet<UInt160> List { get; private set; }

        public static Policy Default { get; private set; }

        static Policy()
        {
            Default = new Policy();
            Default.Refresh();
        }

        public void Refresh()
        {
            if (File.Exists("policy.json"))
            {
                IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("policy.json").Build().GetSection("PolicyConfiguration");
                PolicyLevel = (PolicyLevel)Enum.Parse(typeof(PolicyLevel), section.GetSection("PolicyLevel").Value, true);
                List = new HashSet<UInt160>(section.GetSection("List").GetChildren().Select(p => Wallet.ToScriptHash(p.Value)));
            }
            else
            {
                PolicyLevel = PolicyLevel.AllowAll;
                List = new HashSet<UInt160>();
            }
        }
    }
}
