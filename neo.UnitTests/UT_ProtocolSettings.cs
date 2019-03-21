using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
//using Settings = Neo.Plugins.Settings;
using ProtocolSettings = Neo.ProtocolSettings;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ProtocolSettings
    {
        [TestInitialize]
        public void TestSetup()
        {
        }

        [TestMethod]
        public void Test_BlockTime_Seconds()
        {
            ProtocolSettings.Default.SecondsPerBlock.Should().Be(TestBlockchain.BlockTime);
        }

        [TestMethod]
        public void Test_Standby_Validators()
        {
            // Using Random values
            /*
            // priv 0: 385efdf6370aa6a3b46b4eb3d7ef93a597ef5a48fbba02a8f105d06af941ef3b
            ProtocolSettings.Default.StandbyValidators[0].Should().Be("038ceb4aae65fe246f8b731813243b850a2b53b8cdd428db1751cda7d01327af96");
            // priv 1: 990486a42fdcb36a2636f1aff1b2da271996a9bbb945bc3fd327ac6f98685455
            ProtocolSettings.Default.StandbyValidators[1].Should().Be("03e1c04a83b9853ac8c3a30995140ecf22a50c3ef0b67867e7e87c4e55506d36fe");
            // priv 2: 4ea657deebbe9301f3ea93dfb5cca1a73b6281528cbda30bd6ae319f6fdd9f65
            ProtocolSettings.Default.StandbyValidators[2].Should().Be("028614297a9bf736037fd7b5089ea4a4fb2c733c4b9b72c18006a7d63c5e797689");
            // priv 3: a7492d68306ec3036b229c201d3c1e89ac7b46a4fdcf3c2f6d3c318cc043cddb
            ProtocolSettings.Default.StandbyValidators[3].Should().Be("026d3f83b89e4fc38bb0353fdb082e9884154dcd623fdd18dc30830dd9d6bbab47");
            // priv 4: 5f8756cf349b768a7de779ea2a22c04157e62bedb8fbf7a99173c7ca6fb029ef
            ProtocolSettings.Default.StandbyValidators[4].Should().Be("03efe5fee450da2a242beb5d9aa293f0bb58ecedb1ce1d72fa00ff11529033c60a");
            // priv 5: 29655b428da03469f0d26793d70005b83cef5a7e0cfd26d1e389472be58829db
            ProtocolSettings.Default.StandbyValidators[5].Should().Be("032ba2bc755fbad5cf2d6d5e1d718d0adb4f6cda1ade2693885c2d1e4c5c761074");
            // priv 6: 3d8425c5dca18dab21c52543cd2dda18a9583b44a27dc16d902c9ca030fbf9eb
            ProtocolSettings.Default.StandbyValidators[6].Should().Be("03ed0d17d68d6427b2eab3997ed6936a9c859bdb7a9480e062f363cc118e7b6b5b");
            */

            /* CURRENT VALUES (ALREADY RANDOM?) */
            /*
"03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c",
"02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093",
"03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a",
"02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554",
"024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d",
"02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e",
"02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70"
            */
        }



    }
}
