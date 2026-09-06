const fs = require('fs');
const p = 'tools/verify-schema-doc.js';
let s = fs.readFileSync(p, 'utf8');
function R(o, x, tag) {
  if (s.split(o).length - 1 !== 1) throw new Error(tag + ' 앵커');
  s = s.split(o).join(x); console.log('  ' + tag);
}
R("  const norm = t => t.toLowerCase().replace(/\s*identity\(1,1\)/, '').replace(/\(.*\)/, '').trim();",
  "  // ROWVERSION 은 timestamp 의 별칭이다. sys.types 는 timestamp 로 돌려준다.\n"
+ "  const norm = t => t.toLowerCase().replace(/\s*identity\(1,1\)/, '').replace(/\(.*\)/, '').trim()\n"
+ "                     .replace(/^rowversion$/, 'timestamp');", '타입 정규화');
// §8.1.4 Default 표는 이름이 첫 칸이다. 그 형태도 잡는다.
R("  m = L[i].match(/^\|\s*`(IX_[^`]+)`\s*\|/);\n  if (m) expObj.push({ k: 'IX', n: m[1] });",
  "  m = L[i].match(/^\|\s*`(IX_[^`]+)`\s*\|/);\n  if (m) { expObj.push({ k: 'IX', n: m[1] }); continue; }\n"
+ "  // Default 표는 이름이 첫 칸이다 (§8.1.4 형태).\n"
+ "  m = L[i].match(/^\|\s*`(DF_[^`]+)`\s*\|/);\n  if (m) expObj.push({ k: 'DF', n: m[1] });", 'DF 표 파싱');
fs.writeFileSync(p, s);
