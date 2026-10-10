// Deterministic independent ECMAScript binary64 oracle. No network or credentials.
import { writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
const bits = new Set([
  0n, 0x8000000000000000n, 1n, 0x8000000000000001n,
  0x7fefffffffffffffn, 0xffefffffffffffffn,
  0x4340000000000000n, 0xc340000000000000n, 0x4430000000000000n,
  0x44b52d02c7e14af5n, 0x44b52d02c7e14af6n, 0x44b52d02c7e14af7n,
  0x444b1ae4d6e2ef4en, 0x444b1ae4d6e2ef4fn, 0x444b1ae4d6e2ef50n,
  0x3eb0c6f7a0b5ed8cn, 0x3eb0c6f7a0b5ed8dn,
  0x41b3de4355555553n, 0x41b3de4355555554n, 0x41b3de4355555555n,
  0x41b3de4355555556n, 0x41b3de4355555557n, 0xbecbf647612f3696n,
  0x43143ff3c1cb0959n,
]);
const buffer = Buffer.alloc(8);
for (const number of [1e-7, 1e-6, 1e20, 1e21, 0.1, 1, 1.23, 10, 1000000000000000128]) {
  buffer.writeDoubleBE(number); const center = buffer.readBigUInt64BE();
  for (const offset of [-1n, 0n, 1n]) {
    bits.add(center + offset); bits.add((center + offset) | (1n << 63n));
  }
}
let seed = 0x736d6172746163636en;
const count = Number(process.argv[3] ?? 1024);
if (!Number.isSafeInteger(count) || count < 1 || count > 2_000_000) throw Error('Invalid vector count');
for (let i = 0; i < count; i++) {
  seed = BigInt.asUintN(64, seed * 6364136223846793005n + 1442695040888963407n);
  bits.add(seed);
}
const rows = [];
for (const raw of bits) {
  buffer.writeBigUInt64BE(raw); const value = buffer.readDoubleBE();
  if (Number.isFinite(value)) rows.push([raw.toString(16).padStart(16, '0'), JSON.stringify(value)]);
}
const output = process.argv[2] ?? fileURLToPath(new URL('./smartaccount-jcs-numbers.json', import.meta.url));
writeFileSync(output, JSON.stringify(rows, null, 2) + '\n');
console.log(JSON.stringify({ vectors: rows.length, node: process.version, v8: process.versions.v8 }));
