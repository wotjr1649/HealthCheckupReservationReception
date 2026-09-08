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
const expCols = [], expObj = [], expDef = {};
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
  if (m) {
    expObj.push({ k: m[1], n: m[2] });
    // 세 번째 칸이 **정의**다. 이름만 쓰던 DOC-005 는 그대로 두고 정의를 따로 담는다.
    const d = L[i].match(/^\|\s*(?:PK|FK|UQ|CK|DF|UX)\s*\|\s*`[^`]+`\s*\|\s*(.*?)\s*\|\s*$/);
    expDef[m[2]] = { k: m[1], t: cur, def: d ? d[1] : null };
    continue;
  }
  // 인덱스표: | `이름` | Key | INCLUDE | 목적 |
  m = L[i].match(/^\|\s*`(IX_[^`]+)`\s*\|/);
  if (m) {
    expObj.push({ k: "IX", n: m[1] });
    const d = L[i].match(/^\|\s*`IX_[^`]+`\s*\|\s*(.*?)\s*\|\s*(.*?)\s*\|/);
    expDef[m[1]] = { k: 'IX', t: cur, key: d ? d[1] : null, inc: d ? d[2] : null };
    continue;
  }
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
const actCols = [], actObj = [], actFk = {}, actIdx = {}, actFilter = {}, actCon = {};
for (const raw of fs.readFileSync(ACT, 'utf8').split(/\r?\n/)) {
  const l = raw.trim(); if (!l) continue;
  const p = l.split('|');
  if (p[0] === 'C' && p.length >= 6) actCols.push({ t: p[1], ord: +p[2], c: p[3], ty: p[4], nul: +p[5] });
  if (p[0] === 'O' && p.length >= 3) actObj.push({ k: p[1], n: p[2] });
  // F|이름|순번|부모컬럼|참조테이블|참조컬럼|DELETE동작|UPDATE동작
  if (p[0] === 'F' && p.length >= 8) {
    const f = actFk[p[1]] = actFk[p[1]] || { cols: [], ref: null, del: p[6], upd: p[7] };
    f.cols.push(p[3]); f.ref = p[4] + '.' + p[5];
  }
  // K|인덱스|INCLUDE여부|순번|컬럼|내림차순
  if (p[0] === 'K' && p.length >= 6) {
    const x = actIdx[p[1]] = actIdx[p[1]] || { key: [], inc: [] };
    (p[2] === '1' ? x.inc : x.key).push(p[4] + (p[5] === '1' ? ' DESC' : ''));
  }
  // W|인덱스|필터식
  if (p[0] === 'W' && p.length >= 3) actFilter[p[1]] = p.slice(2).join('|');
  // X|테이블|제약|정의
  if (p[0] === 'X' && p.length >= 4) actCon[p[2]] = { t: p[1], def: p.slice(3).join('|') };
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
// ---- 여기부터는 이름이 아니라 **정의**를 본다.
// DOC-001~005 는 이름·타입·순서까지만 봤다. 이름이 같고 정의가 갈라진 상태는 초록이었다.
const cols = s => (s || '').replace(/[`()]/g, '').split(',').map(x => x.trim()).filter(Boolean);
const tok = s => [...(s || '').matchAll(/`([^`]+)`/g)].map(m => m[1]);

// DOC-006 FK 대상·동작. 지금까지 sys.foreign_keys 는 COUNT(*) 로만 세어졌다.
{
  const bad = [];
  for (const [n, d] of Object.entries(expDef)) {
    if (d.k !== 'FK') continue;
    const a = actFk[n];
    if (!a) { bad.push(n + ' 이 DB 에 없다'); continue; }
    const t = tok(d.def);                       // [부모컬럼, 참조테이블.참조컬럼]
    // 동작은 백틱 밖 꼬리에 적는다 — `…`, NO ACTION
    const act = (d.def.split('`').pop() || '').replace(/[^A-Za-z ]/g, '').trim().toUpperCase().replace(/\s+/g, '_');
    const eC = cols(t[0]).join(','), aC = a.cols.join(',');
    if (eC !== aC) bad.push(n + ' 부모컬럼 문서=' + eC + ' DB=' + aC);
    if (t[1] !== a.ref) bad.push(n + ' 참조 문서=' + t[1] + ' DB=' + a.ref);
    if (act && (act !== a.del || act !== a.upd))
      bad.push(n + ' 동작 문서=' + act + ' DB=DELETE ' + a.del + ' / UPDATE ' + a.upd);
  }
  const nFk = Object.values(expDef).filter(d => d.k === 'FK').length;
  if (!nFk) F('DOC-006', '04 §8 에서 FK 행을 찾지 못했다 - 미실행은 PASS 가 아니다');
  else bad.length ? F('DOC-006', 'FK 대상·동작 불일치 ' + bad.length + '건', bad.join('\n'))
                  : P('DOC-006', 'FK ' + nFk + '개의 부모컬럼·참조·동작 일치');
}

// DOC-007 인덱스 Key/INCLUDE 구성. PK·UQ·UX·IX 는 전부 인덱스가 받친다.
// tests/14 가 index_columns 를 덤프하지만 RBD-005 는 **Rebuild 2회의 재현성**만 비교한다 —
// 문서와 대조하는 것은 여기가 처음이다. 04 §8 이 `기록일시 DESC` 처럼 방향까지 적으므로 방향도 본다.
{
  const bad = [];
  let n = 0;
  for (const [name, d] of Object.entries(expDef)) {
    if (!['PK', 'UQ', 'UX', 'IX'].includes(d.k)) continue;
    const a = actIdx[name];
    if (!a) { bad.push(name + ' 이 DB 인덱스에 없다'); continue; }
    n++;
    // IX 는 Key·INCLUDE 가 별도 칸, 나머지는 정의 칸의 첫 백틱 토큰이 Key 다.
    const ekey = d.k === 'IX' ? cols(d.key) : cols(tok(d.def)[0]);
    const einc = d.k === 'IX' ? (String(d.inc).trim() === '-' ? [] : cols(d.inc)) : [];
    const norm = xs => xs.map(x => x.replace(/\s+/g, ' ').trim()).join(',');
    if (norm(ekey) !== norm(a.key)) bad.push(name + ' Key 문서=' + norm(ekey) + ' DB=' + norm(a.key));
    if (norm(einc) !== norm(a.inc)) bad.push(name + ' INCLUDE 문서=' + norm(einc) + ' DB=' + norm(a.inc));
  }
  if (!n) F('DOC-007', '04 §8 에서 인덱스 행을 찾지 못했다 - 미실행은 PASS 가 아니다');
  else bad.length ? F('DOC-007', '인덱스 Key/INCLUDE 구성 불일치 ' + bad.length + '건', bad.join('\n'))
                  : P('DOC-007', '인덱스 ' + n + '개의 Key·INCLUDE 구성 일치 (정렬 방향 포함)');
}

// DOC-008 필터형 인덱스의 필터식. 지금까지 아무 게이트도 보지 않았다.
// 문서는 `WHERE 추가검사코드 IS NOT NULL`, DB 는 ([추가검사코드] IS NOT NULL) 로 적는다.
{
  const fnorm = s => (s || '').replace(/^\s*where\s+/i, '').replace(/[`\[\]\s()]/g, '').toLowerCase();
  const bad = [];
  const docFiltered = Object.entries(expDef)
    .filter(([, d]) => d.def && /where/i.test(d.def)).map(([n]) => n);
  for (const name of new Set([...docFiltered, ...Object.keys(actFilter)])) {
    const e = docFiltered.includes(name)
      ? tok(expDef[name].def).find(t => /where/i.test(t)) : null;
    const a = actFilter[name];
    if (e && !a) bad.push(name + ' 문서는 필터가 있는데 DB 인덱스에 없다');
    else if (!e && a) bad.push(name + ' DB 는 필터 인덱스인데 문서에 필터가 없다: ' + a);
    else if (fnorm(e) !== fnorm(a)) bad.push(name + ' 필터식 문서=' + fnorm(e) + ' DB=' + fnorm(a));
  }
  if (!docFiltered.length && !Object.keys(actFilter).length)
    F('DOC-008', '필터형 인덱스를 양쪽에서 하나도 찾지 못했다 - 미실행은 PASS 가 아니다');
  else bad.length ? F('DOC-008', '필터식 불일치 ' + bad.length + '건', bad.join('\n'))
                  : P('DOC-008', '필터형 인덱스 ' + Object.keys(actFilter).length + '개의 필터식 일치');
}

// DOC-009 **가드로 보존되는 테이블**의 CHECK/DEFAULT 정의 (배포 DDL ↔ DB).
//
// 왜 전체 테이블이 아닌가: test.sh 는 첫 줄이 rebuild.sh 라 회귀가 보는 DB 는 **방금 그 DDL 로
// 만든 것**이다. 01_Schema.sql 이 다섯 테이블을 DROP -> CREATE 하므로 그것들의 정의는 다를 수가
// 없다 — 대조해 봐야 항진명제다. 드리프트가 물리적으로 가능한 곳은 `IF OBJECT_ID … IS NULL`
// 가드로 보존되어 옛 구조가 남을 수 있는 테이블뿐이다 (database/AGENTS.md §8).
// 대상을 여기 적지 않고 DDL 에서 가드를 찾아 유도한다 — 가드가 늘면 자동으로 따라온다.
//
// 왜 04 가 아니라 DDL 과 대조하는가: 04 §8 의 정의 칸은 SQL 이 아니라 서술이다
// ("검사항목코드 공백 불가"). 문자열 대조가 성립하지 않는다(실측 32건 중 4건만 일치).
{
  const fsx = require('fs');
  const raw = fsx.readFileSync('deploy/01_Schema.sql');
  const ddl = raw.slice(raw[0] === 0xEF ? 3 : 0).toString('utf8');
  const balanced = (s, at) => { let d = 0; for (let i = at; i < s.length; i++) { if (s[i] === '(') d++; else if (s[i] === ')') { d--; if (!d) return s.slice(at + 1, i); } } return null; };

  const guarded = [...ddl.matchAll(/IF\s+OBJECT_ID\(\s*N'\[dbo\]\.\[([^\]]+)\]'/g)].map(m => m[1]);
  const ddlDef = {};
  for (const t of guarded) {
    const at = ddl.indexOf('CREATE TABLE [dbo].[' + t + ']');
    if (at < 0) continue;
    const body = balanced(ddl, ddl.indexOf('(', at));
    if (body === null) continue;
    for (const m of body.matchAll(/CONSTRAINT\s+\[([^\]]+)\]\s+(CHECK|DEFAULT)\s*\(/gi)) {
      const d = balanced(body, m.index + m[0].length - 1);
      if (d !== null) ddlDef[m[1]] = { t, def: d };
    }
  }

  // SQL Server 는 CHECK 식을 결정적으로 재작성해 저장한다. 실측한 세 가지를 양쪽에서 같은 꼴로 만든다.
  //   X IN (a,b)  ->  X=a or X=b     X NOT LIKE p  ->  not X like p     X BETWEEN a AND b  ->  X>=a and X<=b
  // [!] 천장이 있다. AND 로 묶인 항 안의 OR 은 괄호를 지우면 접두사가 첫 항에 붙어 정렬이 통하지 않는다
  //     (실측: CK_수검자_SOCIAL_FORMAT). 잡으려면 불리언 식 트리 파서가 필요하다. 그 테이블은
  //     clean-create 라 여기 대상이 아니지만, 가드 테이블에 같은 형태가 생기면 이 검사가 FAIL 한다.
  //     조용히 통과시키지 않고 FAIL 로 드러내는 쪽을 고른다.
  const leftOperand = (s, opIdx) => {
    let i = opIdx - 1;
    while (i >= 0 && /\s/.test(s[i])) i--;
    if (s[i] === ')') {
      let d = 0; for (; i >= 0; i--) { if (s[i] === ')') d++; else if (s[i] === '(') { d--; if (!d) break; } }
      while (i > 0 && /[\w가-힣_.]/.test(s[i - 1])) i--;
      return { start: i, text: s.slice(i, opIdx).trim() };
    }
    const e = i + 1; while (i >= 0 && /[\w가-힣_.'\[\]]/.test(s[i])) i--;
    return { start: i + 1, text: s.slice(i + 1, e).trim() };
  };
  const expand = (t, re, build) => {
    for (;;) { const m = re.exec(t); if (!m) return t;
      const Lo = leftOperand(t, m.index);
      t = t.slice(0, Lo.start) + build(Lo.text, m) + t.slice(m.index + m[0].length); re.lastIndex = 0; }
  };
  const canon = s => {
    let t = (s || '').replace(/[\[\]]/g, '').replace(/\s+/g, ' ').trim().toLowerCase();
    t = expand(t, /\s+not\s+like\s+/g, Lo => ' not ' + Lo + ' like ');
    t = expand(t, /\s+between\s+(\S+)\s+and\s+(\S+)/g, (Lo, m) => Lo + '>=' + m[1] + ' and ' + Lo + '<=' + m[2]);
    t = expand(t, /\s+in\s*\(([^()]*)\)/g, (Lo, m) => '(' + m[1].split(',').map(v => Lo + '=' + v.trim()).join(' or ') + ')');
    return t.replace(/[\s()]/g, '');
  };
  const bagOf = s => canon(s).split(/or(?=[^\s])/).filter(Boolean).sort().join('~');

  const bad = [];
  let n = 0;
  for (const [name, d] of Object.entries(ddlDef)) {
    const a = actCon[name];
    if (!a) { bad.push(name + ' 이 DB 에 없다'); continue; }
    n++;
    if (canon(d.def) === canon(a.def) || bagOf(d.def) === bagOf(a.def)) continue;
    bad.push(name + '\n     DDL ' + canon(d.def).slice(0, 110) + '\n     DB  ' + canon(a.def).slice(0, 110));
  }
  // DB 에만 있는 제약도 잡는다 — 가드 테이블에 손으로 붙인 제약이 그것이다.
  for (const [name, a] of Object.entries(actCon))
    if (guarded.includes(a.t) && !ddlDef[name]) bad.push(name + ' 이 DB 에만 있다 (' + a.t + ')');

  if (!guarded.length) F('DOC-009', '01_Schema.sql 에서 가드 테이블을 찾지 못했다 - 미실행은 PASS 가 아니다');
  else if (!n) F('DOC-009', '가드 테이블(' + guarded.join(',') + ')의 제약 정의를 하나도 대조하지 못했다');
  else bad.length ? F('DOC-009', '가드 테이블 제약 정의 불일치 ' + bad.length + '건', bad.join('\n'))
                  : P('DOC-009', '가드 테이블 ' + guarded.join('·') + ' 의 제약 정의 ' + n + '건 일치 (배포 DDL ↔ DB)');
}

console.log('');
console.log('=== verify-schema-doc: PASS ' + pass + ' / FAIL ' + fail + ' ===');
process.exit(fail ? 1 : 0);
