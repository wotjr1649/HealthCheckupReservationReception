#!/usr/bin/env node
// Phase 4 문서 정합성 게이트.
// 스펙(계약의 단일 출처)과 계획(실행의 단일 출처)이 어긋나지 않는지 기계적으로 판정한다.
// 사람이 표를 보고 "반영했다"고 적는 것을 신뢰하지 않는다 — 실제 문자열을 센다.
'use strict';
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..', 'docs');
const SPEC = path.join(ROOT, 'phase4', '06_DB_Transaction_Security_Seed_CANDIDATE.md');
const PLANDIR = path.join(ROOT, 'phase4', 'plans');
const BASE04 = path.join(ROOT, 'baseline', '04_DB_Design.md');

let fail = 0, pass = 0;
const P = (id, msg) => { pass++; console.log('PASS ' + id + ' ' + msg); };
const F = (id, msg, detail) => {
  fail++; console.log('FAIL ' + id + ' ' + msg);
  if (detail) String(detail).split('\n').slice(0, 30).forEach(l => console.log('        ' + l));
};

const read = f => fs.readFileSync(f, 'utf8');
const planFiles = fs.readdirSync(PLANDIR).filter(f => f.endsWith('.md')).map(f => path.join(PLANDIR, f));
const spec = read(SPEC);
const plans = Object.fromEntries(planFiles.map(f => [path.basename(f), read(f)]));
const allPlans = Object.values(plans).join('\n');
const rel = f => path.basename(f);

// 코드펜스 안/밖을 나눈다. 금지패턴은 코드 안에서만 의미가 있다.
function splitFences(src) {
  const code = [], prose = [];
  let inFence = false;
  for (const line of src.split('\n')) {
    if (/^\s*```/.test(line)) { inFence = !inFence; continue; }
    (inFence ? code : prose).push(line);
  }
  return { code: code.join('\n'), prose: prose.join('\n'), balanced: !inFence };
}

// V01 코드펜스 짝
{
  const bad = [];
  for (const [n, s] of Object.entries(plans)) if (!splitFences(s).balanced) bad.push(n);
  if (!splitFences(spec).balanced) bad.push(rel(SPEC));
  bad.length ? F('V01', '코드펜스 짝이 맞지 않는 파일', bad.join('\n')) : P('V01', '코드펜스 짝 전부 일치');
}

// V02 placeholder — writing-plans 가 금지한 형태. "나머지는 알아서" 는 구현 누락을 게이트 통과시킨다.
{
  const pats = [
    /같은 패턴으로|동일 패턴으로|위 패턴을/, /그대로 옮긴다|끝까지 옮긴다/,
    /표대로\s*(작성|추가)/, /나머지\s*\d+\s*개?\s*(도|는)/, /위 두 건도/,
    /\bTBD\b|\bTODO\b/, /추후 (작성|보완)|나중에 (작성|채운다)/,
  ];
  const hits = [];
  for (const [n, s] of Object.entries(plans))
    s.split('\n').forEach((l, i) => { if (pats.some(p => p.test(l))) hits.push(n + ':' + (i + 1) + ': ' + l.trim().slice(0, 90)); });
  hits.length ? F('V02', 'placeholder ' + hits.length + '건 — 구현자가 임의 보완해야 한다', hits.join('\n')) : P('V02', 'placeholder 0건');
}

// V03 Test ID 생산↔소비 — 스펙 §45.2 카탈로그가 단일 출처. 계획 배치 집합과 양방향 대조.
{
  const RE = /\b(SCH|SED|RUL|SEL|PWR|RWR|CWR|SEC|CON|RBD|VER|SSN|PRE|RBK)-[A-Z]?\d{2,3}\b/g;
  // 카탈로그는 `| `PWR` | `001`~`014` `020`~`028` | 23 | …` 형태. 범위를 전개한다.
  const catBody = (spec.split(/## 45\.2 Test ID 카탈로그/)[1] || '').split(/\n## /)[0];
  const S = new Set();
  let declaredTotal = 0;
  for (const line of catBody.split('\n')) {
    const m = line.match(/^\|\s*`(SCH|SED|RUL|SEL|PWR|RWR|CWR|SEC|CON|RBD|VER|SSN|PRE|RBK)`\s*\|([^|]*)\|\s*(\d+)\s*\|/);
    if (!m) continue;
    const [, pre, ranges, cnt] = m;
    declaredTotal += +cnt;
    for (const r of ranges.matchAll(/`([A-Z]?)(\d{2,3})`\s*(?:~\s*`\1?(\d{2,3})`)?/g)) {
      const alpha = r[1], lo = +r[2], hi = r[3] ? +r[3] : +r[2], w = r[2].length;
      for (let i = lo; i <= hi; i++) S.add(pre + '-' + alpha + String(i).padStart(w, '0'));
    }
  }
  if (!S.size) { F('V03', '스펙 §45.2 Test ID 카탈로그를 파싱하지 못했다'); }
  else if (S.size !== declaredTotal) F('V03c', `카탈로그 범위 전개 ${S.size}건 ≠ 선언 합계 ${declaredTotal}건`);
  else P('V03c', `카탈로그 범위 전개 ${S.size}건 = 선언 합계`);
  const A = new Set(allPlans.match(RE) || []);
  const onlySpec = [...S].filter(x => !A.has(x)).sort();
  const onlyPlan = [...A].filter(x => !S.has(x)).sort();
  onlySpec.length ? F('V03a', '스펙에만 있고 계획에 없는 Test ID ' + onlySpec.length + '건', onlySpec.join(' '))
                  : P("V03a", "카탈로그 " + S.size + "건 전부 계획에 배치됨");
  onlyPlan.length ? F('V03b', '계획에만 있고 스펙에 없는 Test ID ' + onlyPlan.length + '건', onlyPlan.join(' '))
                  : P('V03b', '계획 Test ID 전부 스펙에 근거 있음');
}

// V04 Fixture ID 생산↔소비 — T017 처럼 "추가한다"고 소비처에만 적힌 것을 잡는다.
{
  const RE = /\b(?:T0\d{2}|F0\d{2}|CONC\d)\b/g;
  const seed = plans['02-seed-functions.md'] || '';
  const conc = plans['08-verification-finalization.md'] || '';
  // 소비 = ChartNo 등가비교로 개별 지목된 것. 범위 서술(F001~F019)은 생성기가 만든다.
  const used = new Set();
  for (const m of allPlans.matchAll(/ChartNo\]?\s*(?:=|LIKE)\s*N?'((?:T0\d{2}|F0\d{2}|CONC\d))'/g)) used.add(m[1]);
  const made = new Set(splitFences(seed).code.match(RE) || []);
  (splitFences(conc).code.match(/\bCONC\d\b/g) || []).forEach(x => made.add(x));
  const missing = [...used].filter(x => !made.has(x)).sort();
  missing.length ? F('V04', '소비되지만 생성 코드가 없는 Fixture ' + missing.length + '건', missing.join(' '))
                 : P('V04', 'Fixture ' + made.size + '종 전부 생성 코드 있음');
}

// V05 금지 패턴 (코드펜스 안)
{
  const bans = [
    [/INSERT\s+(INTO\s+)?@\w+\s+EXEC/i, 'INSERT..EXEC — RS 2개 이상이면 Msg 213'],
    [/\bDECLARE\s+\w+\s+CURSOR\b/i, 'CURSOR — 스펙 §9.2 허용목록 밖'],
    [/FOR\s+XML\s+PATH/i, 'FOR XML PATH — 스펙 §9.2 허용목록 밖'],
    [/\|\s*grep\s+[^|\n]*-\w*q/, 'grep -q 뒤 파이프 — SIGPIPE 로 pipefail fail-open'],
    [/\|\s*tee\b/, 'tee — 파이프라인 종료코드가 흐려진다'],
    [/iconv\s+-f\s+UTF-16LE/, 'iconv -f UTF-16LE — sqlcmd 로그는 BOM 포함이라 UTF-16'],
    [/\|\|\s*echo\s+0\b/, '|| echo 0 — grep -c 는 이미 0 을 출력한다'],
  ];
  const hits = [];
  for (const [n, s] of Object.entries(plans)) {
    const lines = s.split('\n');
    // 블록 경계를 먼저 잡는다. set -e 는 sqlcmd 를 돌리는 오케스트레이터에서만 금지다(§8.4).
    const blocks = [];
    let start = -1;
    lines.forEach((l, i) => {
      if (!/^\s*```/.test(l)) return;
      if (start < 0) start = i; else { blocks.push([start + 1, i]); start = -1; }
    });
    lines.forEach((l, i) => {
      const b = blocks.find(([a, z]) => i >= a && i < z);
      if (!b) return;
      // 금지 서술은 그 줄, 같은 펜스의 앞 3줄, 또는 펜스를 여는 제목에 있을 수 있다.
      const heading = lines.slice(Math.max(0, b[0] - 4), b[0]).join('\n');
      const near = lines.slice(Math.max(b[0], i - 3), i + 1).join('\n');
      const ctx = heading + '\n' + near;
      const isProhibition = /금지|쓰지 않는다|쓰면 안|허용목록 밖|사용하지 않는다|안 된다|폐기/.test(ctx);
      for (const [re, why] of bans) if (re.test(l) && !isProhibition) hits.push(n + ':' + (i + 1) + ': ' + why + '\n          ' + l.trim().slice(0, 88));
      if (/set\s+-euo\s+pipefail/.test(l) && /sqlcmd/.test(lines.slice(b[0], b[1]).join('\n')))
        hits.push(n + ':' + (i + 1) + ': set -e + sqlcmd — 실패한 그 줄에서 셸이 끝나 진단이 사라진다 (§8.4)\n          ' + l.trim());
    });
  }
  hits.length ? F('V05', '금지 패턴 ' + hits.length + '건', hits.join('\n')) : P('V05', '금지 패턴 0건');
}

// V06 스펙 버전 참조
{
  const m = spec.match(/\*\*문서 버전:\*\*\s*(v[\d.]+)/);
  const cur = m ? m[1] : null;
  if (!cur) F('V06', '스펙에서 문서 버전을 찾지 못했다');
  else {
    const stale = [];
    for (const [n, s] of Object.entries(plans))
      s.split('\n').forEach((l, i) => {
        if (/v0\.\d/.test(l) && !l.includes(cur) && /Spec|스펙|문서 버전/.test(l))
          stale.push(n + ':' + (i + 1) + ': ' + l.trim().slice(0, 90));
      });
    stale.length ? F('V06', '현재 스펙(' + cur + ') 이 아닌 버전을 참조 ' + stale.length + '건', stale.join('\n'))
                 : P('V06', '계획의 스펙 참조가 전부 ' + cur);
  }
}

// V07 사용자 정의 오류번호 정의↔사용
{
  const RE = /\b5(?:00|10)\d{2}\b/g;
  // §20 표는 `| `50010`~`50015` |` 처럼 범위로 쓴다. 전개해서 등록으로 친다.
  const declared = new Set();
  for (const line of spec.split('\n')) {
    const m = line.match(/^\|\s*`(5\d{4})`\s*(?:~\s*`(5\d{4})`\s*)?\|/);
    if (!m) continue;
    const lo = +m[1], hi = m[2] ? +m[2] : +m[1];
    for (let i = lo; i <= hi; i++) declared.add(String(i));
  }
  const usedAll = new Set([...(spec.match(RE) || []), ...(allPlans.match(RE) || [])]);
  const undeclared = [...usedAll].filter(x => !declared.has(x)).sort();
  undeclared.length ? F('V07', '스펙 §20 표에 없는 오류번호 사용 ' + undeclared.length + '건', undeclared.join(' '))
                    : P('V07', '오류번호 ' + declared.size + '종 전부 §20 에 등록됨');
}

// V08 CHECK / DEFAULT 수치 vs 기준선 실측
{
  const b = read(BASE04);
  const uniq = re => new Set(b.match(re) || []).size;
  const ck = uniq(/\bCK_[A-Z_0-9]+/g), df = uniq(/\bDF_[A-Z_0-9]+/g);
  const claims = [];
  const scan = (name, src) => src.split('\n').forEach((l, i) => {
    let m;
    if ((m = l.match(/CHECK[^0-9\n]{0,14}?(\d+)\s*개/))) claims.push([name, i + 1, 'CK', +m[1], ck, l.trim()]);
    else if ((m = l.match(/CHECK\s+(\d+)\b/)))          claims.push([name, i + 1, 'CK', +m[1], ck, l.trim()]);
    if ((m = l.match(/Default[^0-9\n]{0,16}?(\d+)\s*개/))) claims.push([name, i + 1, 'DF', +m[1], df, l.trim()]);
    else if ((m = l.match(/\bDF\s+(\d+)\b/)))           claims.push([name, i + 1, 'DF', +m[1], df, l.trim()]);
  });
  scan(rel(SPEC), spec);
  for (const [n, s] of Object.entries(plans)) scan(n, s);
  const bad = claims.filter(c => c[3] !== c[4]).map(c => c[0] + ':' + c[1] + ' ' + c[2] + ' 선언=' + c[3] + ' 실측=' + c[4] + '\n          ' + c[5].slice(0, 88));
  bad.length ? F('V08', '기준선 04 실측과 다른 수치 ' + bad.length + '건 (실측 CK=' + ck + ' DF=' + df + ')', bad.join('\n'))
             : P('V08', '제약 수치 일치 (CK=' + ck + ' DF=' + df + ')');
}

// V09 산출물: 계획이 만드는 파일 = 스펙 §7 트리
{
  const after = spec.split(/최종 Database 파일구조/)[1] || '';
  const tree = splitFences(after.split(/\n# /)[0]).code;
  const created = new Set();
  for (const s of Object.values(plans))
    for (const m of s.matchAll(/(?:Create|Modify):\s*`([^`]+)`/g)) {
      const f = m[1].trim();
      if (/^(deploy|tests|tools|scripts)\//.test(f) || /^(Deploy|Rebuild)\.sql$/.test(f)) created.add(f);
    }
  const missing = [...created].filter(f => !tree.includes(path.basename(f)) && !tree.includes(path.dirname(f) + '/')).sort();
  missing.length ? F('V09', '계획이 만들지만 스펙 §7 트리에 없는 산출물 ' + missing.length + '건', missing.join('\n'))
                 : P('V09', '산출물 ' + created.size + '종 전부 §7 트리에 등재');
}

// V10 개인정보 로깅 — §41: 로그에 실제 개인정보를 남기지 않는다. 잠금 자원명에 ChartNo/PatientId 가 들어간다.
{
  const hits = [];
  const scan = (n, s) => s.split('\n').forEach((l, i) => {
    // 결함을 서술하며 인용한 줄은 위반이 아니다 (§44.7 결함표 등).
    if (/위반|평문|금지|출력하지|찍지 않는다|안 된다/.test(l)) return;
    if (/PRINT[^\n]*applock[^\n]*\+\s*@?Res\b/i.test(l) || /PRINT[^\n]*'\s*res\s*='/i.test(l))
      hits.push(n + ':' + (i + 1) + ': ' + l.trim().slice(0, 90));
  });
  scan(rel(SPEC), spec);
  for (const [n, s] of Object.entries(plans)) scan(n, s);
  hits.length ? F('V10', '잠금 자원명(ChartNo/PatientId 포함)을 로그에 출력 ' + hits.length + '건', hits.join('\n'))
              : P('V10', '로그에 개인정보 자원명 출력 0건');
}

// V11 객체명 — 계획이 참조하는 SP/TVF/Table 은 스펙에 정의된 것이어야 한다.
// (이 검사는 실제로 필요했다: 계획이 존재하지 않는 `UFN_HC_마감시각` 을 참조한 적이 있다.)
{
  const RE = /\b(?:USP_HC_|UFN_HC_|SEQ_HC_)[가-힣A-Za-z_0-9]+/g;
  const known = new Set(spec.match(RE) || []);
  const hits = [];
  for (const [n, s] of Object.entries(plans))
    s.split('\n').forEach((l, i) => {
      for (const o of l.match(RE) || [])
        if (!known.has(o)) hits.push(n + ':' + (i + 1) + ': ' + o);
    });
  hits.length ? F('V11', '스펙에 없는 객체명 참조 ' + hits.length + '건', [...new Set(hits)].join('\n'))
              : P('V11', '계획의 SP/TVF/Sequence 참조 ' + known.size + '종 전부 스펙에 정의됨');
}

// V12 완료조건이 Test 건수를 다시 적으면 안 된다 — 그것이 드리프트의 발생원이다.
// SED 10↔11, SEC 10↔11, CON 7↔8, SCH 16↔18 이 전부 이 형태로 어긋났다.
// 건수는 스펙 §45.2 카탈로그 한 곳에만 둔다.
{
  const hits = [];
  for (const [n, s] of Object.entries(plans))
    s.split('\n').forEach((l, i) => {
      if (!/^\*\*완료조건:\*\*/.test(l)) return;
      if (/§45\.2/.test(l)) return;   // 카탈로그를 참조하면 그 줄 자체가 단일 출처를 가리킨다
      // Test ID prefix 와 함께 개수를 적은 경우만 잡는다. "Msg 1205 0건" 같은 건 아니다.
      if (/\b(SCH|SED|RUL|SEL|PWR|RWR|CWR|SEC|CON|RBD|VER|SSN|PRE|RBK)\b[^\n]*?\b\d+\s*(건|개)/.test(l)
          || /\b\d+\s*(건|개)\s*PASS/.test(l) || /PASS\s*\d+\s*(건|개)/.test(l))
        hits.push(n + ':' + (i + 1) + ': ' + l.trim().slice(0, 100));
    });
  hits.length ? F('V12', '완료조건이 Test 건수를 재기술 ' + hits.length + '건 — 스펙 §45.2 를 참조하게 바꾼다', hits.join('\n'))
              : P('V12', '완료조건이 건수를 재기술하지 않음 (§45.2 단일 출처)');
}

// V13 412 ExamDuplicate 는 EX012 로만 발생한다 (스펙 §17.2a).
// NEX-05 술어가 Gender='F' AND Age IN (54,60,66) 이므로 그 프로필이 아니면 412 를 관측할 수 없고
// 테스트가 0 을 받고 조용히 통과한다. 412 를 기대하는 블록은 EX012 보유 Fixture 를 지목해야 한다.
{
  const OK = /T011|T012|T013|T018|T020/;   // 여 54/60/66 또는 경계 프로필
  const hits = [];
  for (const [n, s] of Object.entries(plans)) {
    const lines = s.split('\n');
    lines.forEach((l, i) => {
      // 412 만 단독으로 기대하는 줄이 대상이다. 410·411 이 함께 있으면 범위·Catalog 표기다.
      if (!/\b412\b/.test(l) || /\b41[01]\b/.test(l)) return;
      if (/§17\.2a|도달 가능|관측할 수 없다|EX012 로만|일반건강검진에 포함/.test(l)) return;
      if (/THEN\s+412/.test(l)) return;   // TVF 구현부(412 를 만드는 쪽)는 대상이 아니다
      const ctx = lines.slice(Math.max(0, i - 14), i + 22).join('\n');
      if (!OK.test(ctx)) hits.push(n + ':' + (i + 1) + ': ' + l.trim().slice(0, 88));
    });
  }
  hits.length ? F('V13', '412 를 기대하는데 EX012 보유 프로필(여 54/60/66)이 문맥에 없다 ' + hits.length + '건', hits.join('\n'))
              : P('V13', '412 기대 테스트가 전부 EX012 보유 프로필을 쓴다 (§17.2a)');
}

console.log('\n=== verify-docs: PASS ' + pass + ' / FAIL ' + fail + ' ===');
process.exit(fail ? 1 : 0);
