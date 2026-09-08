#!/usr/bin/env node
// G13 (c) — 스펙 §9.2 허용목록 준수의 사람 검토 기록을 만든다.
//
// `verify-tsql-allowlist.sh` 는 **금지 블랙리스트**만 훑는다. §9.2 는 허용목록이므로
// "목록에 없는 것을 안 썼다" 는 블랙리스트로 증명되지 않는다 — 그래서 G13 (c) 는
// 자동 판정 불가이고 `REVIEWED` 다. 다만 무증거 자기선언이 되지 않도록 06 이
// 남길 항목을 지정했다. 이 스크립트가 그 항목을 실측으로 채운다.
//
// 허용목록의 단일 출처는 06 §9.2 표다 — 여기에 목록을 베끼지 않는다
// (ROOT AGENTS.md §6). 표를 고치면 이 보고서도 따라 바뀐다.
'use strict';
const fs = require('fs');
const path = require('path');

const DB = process.argv[2] || path.resolve(__dirname, '..');
const SPEC = path.resolve(DB, '..', 'docs', 'baseline', '06_DB_Transaction_Security_Seed.md');
const read = f => { const b = fs.readFileSync(f); return b.slice(b[0] === 0xEF ? 3 : 0).toString('utf8'); };

// ── 대상 파일 (06 이 지정한 범위 + tsql-allowlist 가 실제로 훑는 tests/contract)
const corpus = [];
for (const f of ['Deploy.sql', 'Rebuild.sql']) corpus.push(f);
for (const d of ['deploy', 'tests', 'tests/contract'])
  for (const f of fs.readdirSync(path.join(DB, d)).filter(f => f.endsWith('.sql')).sort())
    corpus.push(d + '/' + f);
const body = Object.fromEntries(corpus.map(f => [f, read(path.join(DB, f))]));
// 주석을 지운 뒤 검사한다. 금지 기능을 **설명하는** 문장은 위반이 아니다
// (verify-tsql-allowlist.sh 가 `sed 's/--.*$//'` 로 하는 것과 같은 처리다).
const code = Object.fromEntries(corpus.map(f =>
  [f, body[f].split(/\r?\n/).map(l => l.replace(/--.*$/, '')).join('\n')]));
const lines = f => body[f].split(/\r?\n/).length;

// ── §9.2 표 파싱. 허용/금지 두 표를 절 안에서 잘라낸다.
const spec = read(SPEC);
const s92 = spec.slice(spec.indexOf('## 9.2 작성 규칙'));
const sec = s92.slice(0, s92.search(/^#{1,2} (?!9\.2)/m) < 0 ? undefined : s92.search(/^#{1,2} (?!9\.2)/m));
const rows = [...sec.matchAll(/^\|\s*(.+?)\s*\|\s*(.*?)\s*\|\s*$/gm)]
  .map(m => ({ item: m[1], why: m[2] }))
  .filter(r => !/^-+$/.test(r.item) && !/^허용 \(|^금지 \(근거\)$|^\*\*허용/.test(r.item));

// 금지표는 두 번째 열이 없다 — 파싱 결과에서 why 가 빈 행이 금지 쪽이다.
const allow = rows.filter(r => r.why && !/^-+$/.test(r.why));
const deny = rows.filter(r => !r.why || /^-+$/.test(r.why));

// ── 사용/미사용 판정. 행의 백틱 토큰을 근거로 삼는다.
const esc = s => s.replace(/[.*+?^${}()|[\]\\]/g, m => '\\' + m);
// 항목 칸에 백틱 토큰이 없으면(`Filtered Index` 처럼 서술형) **용도 칸**의 토큰으로 판정한다.
// §9.2 는 그런 행의 용도 칸에 실제 객체명을 적어 둔다 — 단일 출처는 여전히 §9.2 다.
function probe(row) {
  const item = /`/.test(row.item) ? row.item : (row.item + ' ' + row.why);
  const toks = [...item.matchAll(/`([^`]+)`/g)].map(m => m[1])
    .map(t => t.replace(/…|\s*\(…\)|\(…, RESEED, n\)/g, '').trim())
    .filter(t => t && !/^Msg /.test(t));
  if (!toks.length) return { toks: [], hits: null };
  const hits = toks.map(t => {
    const core = t.split(/[ ,]/)[0].replace(/\(.*$/, '');
    const re = new RegExp(esc(core).replace(/_/g, '_'), 'i');
    return { t, n: corpus.filter(f => re.test(code[f])).length };
  });
  return { toks, hits };
}

// ── 허용목록 밖 스캔. 블랙리스트 게이트가 보지 않는 둘을 여기서 본다.
const gap = [
  ['TVP (CREATE TYPE … AS TABLE)', /CREATE\s+TYPE[\s\S]{0,80}?AS\s+TABLE/i],
  ['업무 Trigger (CREATE TRIGGER)', /CREATE\s+(OR\s+ALTER\s+)?TRIGGER/i],
  ['FK Cascade (ON DELETE/UPDATE CASCADE)', /ON\s+(DELETE|UPDATE)\s+CASCADE/i],
  ['CURSOR 선언', /DECLARE\s+[A-Za-z_@][\w@]*\s+CURSOR/i],
  ['FOR XML', /FOR\s+XML/i],
  ['동적 SQL (EXEC( / sp_executesql)', /EXEC\s*\(|sp_executesql/i],
];
const gapHits = [];
for (const [name, re] of gap)
  for (const f of corpus) {
    code[f].split(/\r?\n/).forEach((l, i) => { if (re.test(l)) gapHits.push(name + ' — ' + f + ':' + (i + 1) + '  ' + l.trim().slice(0, 70)); });
  }

// ── 출력
const now = new Date();
const ts = now.getFullYear() + '-' + String(now.getMonth() + 1).padStart(2, '0') + '-' +
  String(now.getDate()).padStart(2, '0') + ' ' + String(now.getHours()).padStart(2, '0') + ':' +
  String(now.getMinutes()).padStart(2, '0');
const out = [];
const W = out.push.bind(out);

W('# G13 (c) — 스펙 §9.2 허용목록 준수 검토');
W('');
W('`verify-tsql-allowlist.sh` 는 **금지 블랙리스트**만 본다. §9.2 는 허용목록이므로 "목록에 없는 것을');
W('쓰지 않았다" 는 그 스캔으로 증명되지 않는다 — 그래서 G13 `(c)` 는 자동 판정 불가이고 `REVIEWED` 다.');
W('이 파일은 그 `REVIEWED` 가 무증거 자기선언이 되지 않게 하는 근거이며, `node tools/allowlist-review.js`');
W('로 다시 만든다. 허용목록 자체는 06 §9.2 가 단일 출처이고 여기에 베끼지 않는다.');
W('');
W('## 1. 대상 파일');
W('');
W('| 파일 | 줄 |');
W('|---|---:|');
for (const f of corpus) W('| `' + f + '` | ' + lines(f) + ' |');
W('| **합계 ' + corpus.length + '개** | **' + corpus.reduce((a, f) => a + lines(f), 0) + '** |');
W('');
W('## 2. §9.2 허용목록 대조');
W('');
W('행은 06 §9.2 허용 표에서 그대로 읽었다. `사용` 은 그 행의 백틱 토큰이 대상 파일에 나타난 파일 수다.');
W('');
W('| §9.2 허용 항목 | 판정 | 근거 |');
W('|---|---|---|');
let used = 0, unused = 0, manual = 0;
for (const r of allow) {
  const p = probe(r);
  let verdict, ev;
  if (!p.toks.length) { verdict = '**수동**'; ev = '백틱 토큰이 없는 서술 항목 — 아래 §4 에서 사람이 판단'; manual++; }
  else {
    const on = p.hits.filter(h => h.n > 0);
    if (on.length) { verdict = '사용'; used++; ev = on.map(h => '`' + h.t + '` ' + h.n + '개 파일').join(' · '); }
    else { verdict = '미사용'; unused++; ev = p.hits.map(h => '`' + h.t + '`').join(' · ') + ' — 0건'; }
  }
  W('| ' + r.item.replace(/\|/g, '\\|') + ' | ' + verdict + ' | ' + ev + ' |');
}
W('');
W('사용 ' + used + ' · 미사용 ' + unused + ' · 수동판단 ' + manual + ' (허용 행 ' + allow.length + ')');
W('');
W('`[I]` **미사용은 위반이 아니다.** 허용목록은 "써도 되는 것" 이지 "써야 하는 것" 이 아니다.');
W('');
W('## 3. 허용목록 밖 발견');
W('');
W('블랙리스트 게이트가 보지 않는 항목을 여기서 직접 훑는다 — `verify-tsql-allowlist.sh` 의 BAN 에');
W('**TVP 와 Trigger 가 없다**(실측). 그 구멍을 이 검토가 메운다.');
W('');
if (!gapHits.length) {
  W('| 항목 | 결과 |');
  W('|---|---|');
  for (const [name] of gap) W('| ' + name + ' | **0건** |');
} else {
  W('```text');
  gapHits.forEach(h => W(h));
  W('```');
}
W('');
W('## 4. 수동 판단이 필요한 행');
W('');
W('백틱 토큰이 없어 기계가 판정하지 못한 행이다. 사람이 읽고 아래에 판정을 적는다.');
W('');
for (const r of allow) if (!probe(r).toks.length) W('- ' + r.item + ' — ');
W('');
W('## 5. 금지 목록');
W('');
W('06 §9.2 금지 표는 ' + deny.length + '행이다. 자동 스캔은 `verify-tsql-allowlist.sh` 가,');
W('그 BAN 에 없는 TVP·Trigger·FK Cascade 는 위 §3 이 본다.');
W('');
W('## 6. 검토자·시각');
W('');
W('| 항목 | 값 |');
W('|---|---|');
W('| 검토자 | Claude Opus 5 (세션 실행) |');
W('| 시각 | ' + ts + ' KST |');
W('| 생성 | `node tools/allowlist-review.js` |');
W('| 대상 커밋 | (커밋 직전 트리) |');
W('');

const dest = path.join(DB, 'artifacts', 'reports', 'allowlist-review.md');
fs.writeFileSync(dest, out.join('\n'), 'utf8');
console.log('wrote ' + dest);
console.log('대상 ' + corpus.length + '개 파일 · 허용행 ' + allow.length + ' (사용 ' + used + ' 미사용 ' + unused + ' 수동 ' + manual + ') · 목록 밖 ' + gapHits.length + '건');
if (gapHits.length) process.exit(1);
