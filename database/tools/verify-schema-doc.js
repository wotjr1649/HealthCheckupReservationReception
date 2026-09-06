// 기준선 04 §8 의 테이블 정의와 실제 배포된 스키마를 양방향으로 대조한다.
// 문서가 스키마를 따라가지 못하는 드리프트는 배포로 드러나지 않는다 ― 그래서 이 검사가 있다.
// 입력: artifacts/reports/schema-actual.txt (scripts/verify-schema-doc.sh 가 만든다)
const fs = require('fs');
const BASE04 = '../docs/baseline/04_DB_Design.md';
const ACT = 'artifacts/reports/schema-actual.txt';
let fail = 0, pass = 0;
const P = (id, m) => { pass++; console.log('PASS ' + id + ' ' + m); };
const F = (id, m, d) => { fail++; console.log('FAIL ' + id + ' ' + m); if (d) console.log(d.split('\n').map(x => '        ' + x).join('\n')); };

const doc = fs.readFileSync(BASE04, 'utf8');
const NL = doc.includes('\r\n') ? '\r\n' : '\n';
const L = doc.split(NL);

// ---- 문서에서 기대 스키마를 뽑는다
const s8 = L.findIndex(l => l.startsWith('# 8. ')), e8 = L.findIndex(l => l.startsWith('# 9. '));
if (s8 < 0 || e8 < 0) { console.log('FAIL DOC-000 04 §8 구간을 찾지 못했다'); process.exit(1); }
const expCols = [], expObj = [];
let cur = null;
for (let i = s8; i < e8; i++) {
  const h = L[i].match(/^## 8\.\d+ `([^`]+)`/);
  if (h) { cur = h[1]; continue; }
  if (!cur) continue;
  // 컬럼표:  | 1 | `이름` | `타입` | X | ... |
  let m = L[i].match(/^\|\s*(\d+)\s*\|\s*`([^`]+)`\s*\|\s*`([^`]+)`\s*\|\s*([XO])\s*\|/);
  if (m) { expCols.push({ t: cur, ord: +m[1], c: m[2], ty: m[3], nul: m[4] === 'O' ? 1 : 0 }); continue; }
  // 제약표:  | PK | `이름` | 정의 |
  m = L[i].match(/^\|\s*(PK|FK|UQ|CK|DF|UX)\s*\|\s*`([^`]+)`\s*\|/);
  if (m) { expObj.push({ k: m[1], n: m[2] }); continue; }
  // 인덱스표: | `이름` | Key | INCLUDE | 목적 |
  m = L[i].match(/^\|\s*`(IX_[^`]+)`\s*\|/);
  if (m) { expObj.push({ k: "IX", n: m[1] }); continue; }
  // Default 표는 이름이 첫 칸이다 (§8.1.4 형태).
  {
    const t = L[i].trim();
    if (t.startsWith("| `DF_")) {
      const a = t.indexOf("`") + 1, b = t.indexOf("`", a);
      if (b > a) { expObj.push({ k: "DF", n: t.slice(a, b) }); continue; }
    }
  }
  if (m) expObj.push({ k: "DF", n: m[1] });
}

// ---- 실측
if (!fs.existsSync(ACT)) { console.log('FAIL DOC-000 ' + ACT + ' 가 없다. scripts/verify-schema-doc.sh 로 실행한다'); process.exit(1); }
const actCols = [], actObj = [];
for (const raw of fs.readFileSync(ACT, 'utf8').split(/\r?\n/)) {
  const l = raw.trim(); if (!l) continue;
  const p = l.split('|');
  if (p[0] === 'C' && p.length >= 6) actCols.push({ t: p[1], ord: +p[2], c: p[3], ty: p[4], nul: +p[5] });
  if (p[0] === 'O' && p.length >= 3) actObj.push({ k: p[1], n: p[2] });
}

// ---- 대조
const ckey = x => x.t + '.' + x.c;
const eC = new Set(expCols.map(ckey)), aC = new Set(actCols.map(ckey));
const onlyDoc = [...eC].filter(x => !aC.has(x)).sort();
const onlyDb  = [...aC].filter(x => !eC.has(x)).sort();
(onlyDoc.length || onlyDb.length)
  ? F('DOC-001', '컬럼 집합 불일치 (문서만 ' + onlyDoc.length + ' · DB만 ' + onlyDb.length + ')',
      [...onlyDoc.map(x => '문서만: ' + x), ...onlyDb.map(x => 'DB만  : ' + x)].join('\n'))
  : P('DOC-001', '컬럼 집합 양방향 일치 (' + eC.size + '개)');

// NULL 허용 여부
{
  const am = new Map(actCols.map(x => [ckey(x), x]));
  const bad = expCols.filter(x => am.has(ckey(x)) && am.get(ckey(x)).nul !== x.nul)
    .map(x => ckey(x) + ' 문서=' + (x.nul ? 'O' : 'X') + ' DB=' + (am.get(ckey(x)).nul ? 'O' : 'X'));
  bad.length ? F('DOC-002', 'NULL 허용 불일치 ' + bad.length + '건', bad.join('\n'))
             : P('DOC-002', 'NULL 허용 전건 일치');
}
// 타입 (문서는 BIGINT IDENTITY(1,1) 처럼 쓰므로 앞 토큰만 본다)
{
  const am = new Map(actCols.map(x => [ckey(x), x]));
  // ROWVERSION 은 timestamp 의 별칭이다. sys.types 는 timestamp 로 돌려준다.
  const norm = t => {
    let v = t.toLowerCase().trim();
    const k = v.indexOf(" identity");  if (k >= 0) v = v.slice(0, k);
    const q = v.indexOf("(");          if (q >= 0) v = v.slice(0, q);
    v = v.trim();
    return v === "rowversion" ? "timestamp" : v;   // ROWVERSION 은 timestamp 의 별칭이다
  };
  const bad = expCols.filter(x => am.has(ckey(x)) && norm(x.ty) !== norm(am.get(ckey(x)).ty))
    .map(x => ckey(x) + ' 문서=' + x.ty + ' DB=' + am.get(ckey(x)).ty);
  bad.length ? F('DOC-003', '타입 불일치 ' + bad.length + '건', bad.join('\n'))
             : P('DOC-003', '타입 전건 일치');
}
// 컬럼 순서
{
  const am = new Map(actCols.map(x => [ckey(x), x]));
  const bad = expCols.filter(x => am.has(ckey(x)) && am.get(ckey(x)).ord !== x.ord)
    .map(x => ckey(x) + ' 문서=' + x.ord + ' DB=' + am.get(ckey(x)).ord);
  bad.length ? F('DOC-004', '컬럼 순서 불일치 ' + bad.length + '건', bad.join('\n'))
             : P('DOC-004', '컬럼 순서 전건 일치 (배포 DDL 순서 = 문서 순서)');
}
// 제약·인덱스 이름
{
  const e = new Set(expObj.map(x => x.n)), a = new Set(actObj.map(x => x.n));
  const od = [...e].filter(x => !a.has(x)).sort(), ob = [...a].filter(x => !e.has(x)).sort();
  (od.length || ob.length)
    ? F('DOC-005', '제약·인덱스 이름 불일치 (문서만 ' + od.length + ' · DB만 ' + ob.length + ')',
        [...od.map(x => '문서만: ' + x), ...ob.map(x => 'DB만  : ' + x)].join('\n'))
    : P('DOC-005', '제약·인덱스 이름 양방향 일치 (' + e.size + '개)');
}
console.log('');
console.log('=== verify-schema-doc: PASS ' + pass + ' / FAIL ' + fail + ' ===');
process.exit(fail ? 1 : 0);
