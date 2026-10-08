// Independent NEF serialization and JCS/hash oracle for the native profile.
import { createHash } from 'node:crypto';
import { writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
const sha = bytes => createHash('sha256').update(bytes).digest();
const canonical = value => value === null || typeof value !== 'object' ? JSON.stringify(value)
  : Array.isArray(value) ? '[' + value.map(canonical).join(',') + ']'
  : '{' + Object.keys(value).sort().map(key => JSON.stringify(key) + ':' + canonical(value[key])).join(',') + '}';
// NEF3, empty compiler/source/tokens, zero reserved fields, [PUSHF, RET].
const body = Buffer.concat([Buffer.from('NEF3'), Buffer.alloc(64), Buffer.from([0, 0, 0, 0, 0, 2, 9, 64])]);
const nef = Buffer.concat([body, sha(sha(body)).subarray(0, 4)]);
const manifest = { name: 'BindingVector', groups: [], features: {}, supportedstandards: [],
  abi: { methods: [{ name: 'supportsComposition', parameters: [], returntype: 'Boolean', offset: 0, safe: true }], events: [] },
  permissions: [{ contract: '*', methods: '*' }], trusts: [],
  extra: { z: -0, a: { '2': 'é', '10': 0.000001 }, '😀': [true, false, null, '<>&/\u2028', 1e21, 'e\u0301'] } };
const bytes = Buffer.from(canonical(manifest));
const result = { nefBase64: nef.toString('base64'), manifest,
  canonicalManifestUtf8: bytes.toString(), codeHashWireHex: sha(Buffer.concat([nef, Buffer.from([0]), bytes])).toString('hex') };
writeFileSync(process.argv[2] ?? fileURLToPath(new URL('./smartaccount-binding-v1.json', import.meta.url)), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify({ codeHashWireHex: result.codeHashWireHex, node: process.version }));
