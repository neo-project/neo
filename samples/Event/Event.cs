using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Event;

[DisplayName("SampleEvent")]
[ManifestExtra("Author", "code-dev")]
[ManifestExtra("Description", "A sample contract that demonstrates how to use Events")]
[ManifestExtra("Email", "core@neo.org")]
[ManifestExtra("Version", "0.0.1")]
[ContractSourceCode("https://github.com/neo-project/samples")]
[ContractPermission("*", "*")]
public class SampleEvent : SmartContract
{
    [DisplayName("new_event_name")]
    public static event Action<byte[], string, BigInteger> event_name;

    public static event Action<byte[], BigInteger> event2;

    public static bool Main()
    {
        byte[] ba = new byte[] { 0x01, 0x02, 0x03 };
        event_name(ba, "oi", 10); // will Runtime.Notify: 'new_event_name', '\x01\x02\x03', 'oi', 10

        event2(ba, 50); // will Runtime.Notify: 'event2', '\x01\x02\x03', '\x32'

        return false;
    }
}
