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
        public void Test_2_Seconds()
        {
            ProtocolSettings.Default.SecondsPerBlock.Should().Be(2);
        }

        [TestMethod]
        public void Test_Standby_Validators()
        {
            // Using Random values
            
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
        }



    }
}
