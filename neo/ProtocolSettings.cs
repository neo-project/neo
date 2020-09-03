using Microsoft.Extensions.Configuration;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Neo
{
    public class ProtocolSettings
    {
        public uint Magic { get; }
        public byte AddressVersion { get; }
        public string[] StandbyValidators { get; }
        public string[] SeedList { get; }
        public IReadOnlyDictionary<TransactionType, Fixed8> SystemFee { get; }
        public Fixed8 LowPriorityThreshold { get; }
        public uint SecondsPerBlock { get; }

        public uint StateRootEnableIndex { get; }
        public Fixed8 MinimumNetworkFee { get; }
        static ProtocolSettings _default;

        static bool UpdateDefault(IConfiguration configuration)
        {
            var settings = new ProtocolSettings(configuration.GetSection("ProtocolConfiguration"));
            return null == Interlocked.CompareExchange(ref _default, settings, null);
        }

        public static bool Initialize(IConfiguration configuration)
        {
            return UpdateDefault(configuration);
        }

        public static ProtocolSettings Default
        {
            get
            {
                if (_default == null)
                {
                    var configuration = new ConfigurationBuilder().AddJsonFile("protocol.json", true).Build();
                    UpdateDefault(configuration);
                }

                return _default;
            }
        }

        private ProtocolSettings(IConfigurationSection section)
        {
            this.Magic = section.GetValue("Magic", 0x746E41u);
            this.AddressVersion = section.GetValue("AddressVersion", (byte)0x17);
            IConfigurationSection section_sv = section.GetSection("StandbyValidators");
            if (section_sv.Exists())
                this.StandbyValidators = section_sv.GetChildren().Select(p => p.Get<string>()).ToArray();
            else
                this.StandbyValidators = new[]
                {
                    "03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c",
                    "02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093",
                    "03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a",
                    "02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554",
                    "024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d",
                    "02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e",
                    "02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70"
                };
            IConfigurationSection section_sl = section.GetSection("SeedList");
            if (section_sl.Exists())
                this.SeedList = section_sl.GetChildren().Select(p => p.Get<string>()).ToArray();
            else
                this.SeedList = new[]
                {
                    "seed1.neo.org:10333",
                    "seed2.neo.org:10333",
                    "seed3.neo.org:10333",
                    "seed4.neo.org:10333",
                    "seed5.neo.org:10333"
                };
            Dictionary<TransactionType, Fixed8> sys_fee = new Dictionary<TransactionType, Fixed8>
            {
                [TransactionType.EnrollmentTransaction] = Fixed8.FromDecimal(1000),
                [TransactionType.IssueTransaction] = Fixed8.FromDecimal(500),
                [TransactionType.PublishTransaction] = Fixed8.FromDecimal(500),
                [TransactionType.RegisterTransaction] = Fixed8.FromDecimal(10000)
            };
            foreach (IConfigurationSection child in section.GetSection("SystemFee").GetChildren())
            {
                TransactionType key = (TransactionType)Enum.Parse(typeof(TransactionType), child.Key, true);
                sys_fee[key] = Fixed8.Parse(child.Value);
            }
            this.SystemFee = sys_fee;
            this.SecondsPerBlock = section.GetValue("SecondsPerBlock", 15u);
            this.StateRootEnableIndex = section.GetValue("StateRootEnableIndex", 0u);
            this.LowPriorityThreshold = Fixed8.Parse(section.GetValue("LowPriorityThreshold", "0.001"));
            this.MinimumNetworkFee = Fixed8.Parse(section.GetValue("MinimumNetworkFee", "0"));
        }
    }
}
