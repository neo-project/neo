using System.Diagnostics;

namespace Neo.VM
{
    public static class Benchmarks
    {
        public static void NeoIssue2528()
        {
            // https://github.com/neo-project/neo/issues/2528
            // L01: INITSLOT 1, 0
            // L02: NEWARRAY0
            // L03: DUP
            // L04: DUP
            // L05: PUSHINT16 2043
            // L06: STLOC 0
            // L07: PUSH1
            // L08: PACK
            // L09: LDLOC 0
            // L10: DEC
            // L11: STLOC 0
            // L12: LDLOC 0
            // L13: JMPIF_L L07
            // L14: PUSH1
            // L15: PACK
            // L16: APPEND
            // L17: PUSHINT32 38000
            // L18: STLOC 0
            // L19: PUSH0
            // L20: PICKITEM
            // L21: LDLOC 0
            // L22: DEC
            // L23: STLOC 0
            // L24: LDLOC 0
            // L25: JMPIF_L L19
            // L26: DROP
            Run(nameof(NeoIssue2528), "VwEAwkpKAfsHdwARwG8AnXcAbwAl9////xHAzwJwlAAAdwAQzm8AnXcAbwAl9////0U=");
        }

        public static void NeoVMIssue418()
        {
            // https://github.com/neo-project/neo-vm/issues/418
            // L00: NEWARRAY0
            // L01: PUSH0
            // L02: PICK
            // L03: PUSH1
            // L04: PACK
            // L05: PUSH1
            // L06: PICK
            // L07: PUSH1
            // L08: PACK
            // L09: INITSSLOT 1
            // L10: PUSHINT16 510
            // L11: DEC
            // L12: STSFLD0
            // L13: PUSH1
            // L14: PICK
            // L15: PUSH1
            // L16: PICK
            // L17: PUSH2
            // L18: PACK
            // L19: REVERSE3
            // L20: PUSH2
            // L21: PACK
            // L22: LDSFLD0
            // L23: DUP
            // L24: JMPIF L11
            // L25: DROP
            // L26: ROT
            // L27: DROP
            Run(nameof(NeoVMIssue418), "whBNEcARTRHAVgEB/gGdYBFNEU0SwFMSwFhKJPNFUUU=");
        }

        public static void NeoIssue2723()
        {
            // L00: INITSSLOT 1
            // L01: PUSHINT32 130000
            // L02: STSFLD 0
            // L03: PUSHINT32 1048576
            // L04: NEWBUFFER
            // L05: DROP
            // L06: LDSFLD 0
            // L07: DEC
            // L08: DUP
            // L09: STSFLD 0
            // L10: JMPIF L03
            Run(nameof(NeoIssue2723), "VgEC0PsBAGcAAgAAEACIRV8AnUpnACTz");
        }

        private static void Run(string name, string poc)
        {
            byte[] script = Convert.FromBase64String(poc);
            using ExecutionEngine engine = new();
            engine.LoadScript(script);
            Stopwatch stopwatch = Stopwatch.StartNew();
            engine.Execute();
            stopwatch.Stop();
            Debug.Assert(engine.State == VMState.HALT);
            Console.WriteLine($"Benchmark: {name},\tTime: {stopwatch.Elapsed}");
        }
    }
}
