#!/usr/bin/env node
// Phase 4 문서 정합성 게이트.
// 스펙(계약의 단일 출처)과 계획(실행의 단일 출처)이 어긋나지 않는지 기계적으로 판정한다.
// 사람이 표를 보고 "반영했다"고 적는 것을 신뢰하지 않는다 — 실제 문자열을 센다.
'use strict';
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..', 'docs');
const SPEC = path.join(ROOT, 'phase4', '06_DB_Transaction_Security_Seed.md');
const PLANDIR = path.join(ROOT, 'phase4', 'plans');
const BASE04 = path.join(ROOT, 'baseline', '04_DB_Design.md');
const BASE05 = path.join(ROOT, 'baseline', '05_DB_Rule_SP_Contract.md');

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

// 기준선 04 에서 제목 문자열로 한 절을 잘라낸다. 절 번호가 아니라 제목을 키로 쓰므로
// 04 의 장 번호가 바뀌어도 같은 코드가 동작한다.
function sliceSection(src, title) {
  const m = src.match(new RegExp('^#+[^\\n]*' + title + '[^\\n]*$', 'm'));
  if (!m) return '';
  const rest = src.slice(src.indexOf(m[0]) + m[0].length);
  const nx = rest.search(/^# (?!#)/m);
  return nx < 0 ? rest : rest.slice(0, nx);
}
const base04 = read(BASE04);
const sec04Table = sliceSection(base04, '테이블 상세 정의');
const sec04Summary = sliceSection(base04, '물리 테이블 요약');
const uniq = re => new Set(sec04Table.match(re) || []).size;

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
  const RE = /\b(SCH|SED|RUL|SEL|PWR|RWR|CWR|SEC|CON|RBD|VER|SSN|PRE|RBK|OFF|RED)-[A-Z]?\d{2,3}\b/g;
  // 카탈로그는 `| `PWR` | `001`~`014` `020`~`028` | 23 | …` 형태. 범위를 전개한다.
  const catBody = (spec.split(/## 45\.2 Test ID 카탈로그/)[1] || '').split(/\n## /)[0];
  const S = new Set();
  let declaredTotal = 0;
  for (const line of catBody.split('\n')) {
    const m = line.match(/^\|\s*`(SCH|SED|RUL|SEL|PWR|RWR|CWR|SEC|CON|RBD|VER|SSN|PRE|RBK|OFF|RED)`\s*\|([^|]*)\|\s*(\d+)\s*\|/);
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
    // JS 의 \w 는 ASCII 전용이라 한글 식별자에서 fail-open 한다. @ 도 \w 에 없어
    // `DECLARE @c CURSOR` 를 원래 놓치고 있었다(실측 확인). 둘 다 닫는다.
    [/INSERT\s+(INTO\s+)?@[\w가-힣]+\s+EXEC/i, 'INSERT..EXEC — RS 2개 이상이면 Msg 213'],
    [/\bDECLARE\s+@?[\w가-힣]+\s+CURSOR\b/i, 'CURSOR — 스펙 §9.2 허용목록 밖'],
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
  // R3: 테이블명이 한글이라 \bCK_[A-Z_0-9]+ 는 0 개를 세어 fail-open 한다.
  // 04 의 "테이블 상세 정의" 절만 잘라내고 그 안의 백틱 한정 이름만 센다.
  // 백틱이 경계라 한국어 조사 흡수가 원리적으로 불가능하고,
  // 명명규칙 예시·삭제 설명이 절 밖이라 자동으로 제외된다.
  const ck = uniq(/\`CK_[^\`\n]+\`/g), df = uniq(/\`DF_[^\`\n]+\`/g);
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

// V11 객체명 — 계획이 참조하는 SP/TVF/Sequence 는 스펙에, Table 은 기준선 04 에 정의된 것이어야 한다.
// (이 검사는 실제로 필요했다: 계획이 존재하지 않는 `UFN_HC_마감시각` 을 참조한 적이 있다.)
{
  // Table 축은 주석이 약속만 하고 구현돼 있지 않았다. 그래서 R2 물리 테이블명이 남은
  // 계획 파일을 한 건도 잡지 못했다(실측 확인). Table 의 출처는 스펙이 아니라 기준선 04 다.
  const OBJ = /\b(?:USP_HC_|UFN_HC_|SEQ_HC_)[가-힣A-Za-z_0-9]+/g;
  // R2 잔존 탐지. R3 기준선에 이 형태의 테이블명이 없으므로 걸리면 전부 미정의 참조다.
  const TBLR2 = /\b(?:INFO_|MST_|HIS_)[A-Z][A-Z_0-9]*/g;
  // R3 테이블명은 한글이라 접두사로 잡을 수 없고 한글 단어를 통째로 세면 산문이 전부 걸린다.
  // dbo 스키마 한정 참조만 센다 — 계획의 SQL 이 테이블을 부르는 형태이고 산문과 섞이지 않는다.
  // SP/TVF/Sequence 는 영문으로 시작하므로 이 정규식에 걸리지 않는다.
  const TBLR3 = /(?:\bdbo\.|\[dbo\]\.)\[?([가-힣][가-힣A-Za-z_0-9]*)\]?/g;
  const known = new Set([
    ...(spec.match(OBJ) || []),
    ...[...sec04Summary.matchAll(/\`([가-힣][가-힣A-Za-z_0-9]*)\`/g)].map(x => x[1]),
  ]);
  const hits = [];
  for (const [n, s] of Object.entries(plans))
    s.split('\n').forEach((l, i) => {
      for (const o of [...(l.match(OBJ) || []), ...(l.match(TBLR2) || [])])
        if (!known.has(o)) hits.push(n + ':' + (i + 1) + ': ' + o);
      for (const m of l.matchAll(TBLR3))
        if (!known.has(m[1])) hits.push(n + ':' + (i + 1) + ': ' + m[1]);
    });
  hits.length ? F('V11', '스펙·기준선에 없는 객체명 참조 ' + hits.length + '건', [...new Set(hits)].join('\n'))
              : P('V11', '계획의 SP/TVF/Sequence/Table 참조 ' + known.size + '종 전부 스펙·기준선에 정의됨');
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
      if (/\b(SCH|SED|RUL|SEL|PWR|RWR|CWR|SEC|CON|RBD|VER|SSN|PRE|RBK|OFF|RED)\b[^\n]*?\b\d+\s*(건|개)/.test(l)
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

// V14 계획서 SQL 의 컬럼·식별자가 기준선 스키마에 실재하는가.
// C4 는 Table 축만 닫았다. E1(존재하지 않는 [IsActive] 참조 7곳)이 그 구멍으로 들어왔고
// R3 가 계획 SQL 을 다시 쓰므로 같은 드리프트가 다시 생길 수 있다.
// 알려진 집합의 출처는 배포 산출물이 아니라 기준선이다 — deploy/*.sql 은 계획의 결과물이다.
{
  const known = new Set();
  for (const m of sec04Table.matchAll(/\`([A-Za-z가-힣][A-Za-z0-9_가-힣]*)\`/g)) known.add(m[1]);   // 04 §8 컬럼·제약·인덱스
  for (const m of spec.matchAll(/\[([A-Za-z가-힣][A-Za-z0-9_가-힣]*)\]/g)) known.add(m[1]);            // 06 대괄호 식별자
  for (const m of read(BASE05).matchAll(/\`@?([A-Za-z][A-Za-z0-9_]*)\`/g)) known.add(m[1]);          // 05 반환 컬럼·Parameter
  for (const n of ['tables','columns','types','indexes','index_columns','key_constraints','foreign_keys',
                   'check_constraints','default_constraints','sequences','triggers','table_types','objects',
                   'procedures','parameters','database_principals','database_permissions',
                   'dm_exec_describe_first_result_set_for_object']) known.add(n);                       // sys 카탈로그 뷰

  const hits = [];
  for (const [n, src] of Object.entries(plans)) {
    // 같은 파일에서 정의된 파생 별칭은 실재 컬럼이 아니어도 정당하다.
    const alias = new Set();
    for (const m of src.matchAll(/\[([A-Za-z가-힣][A-Za-z0-9_가-힣]*)\]\s*=/g)) alias.add(m[1]);
    for (const m of src.matchAll(/\bAS\s+\[([A-Za-z가-힣][A-Za-z0-9_가-힣]*)\]/gi)) alias.add(m[1]);
    let inF = false, lang = '';
    src.split('\n').forEach((l, i) => {
      const f = l.match(/^\s*\`\`\`(\w*)/);
      if (f) { if (!inF) { inF = true; lang = f[1]; } else inF = false; return; }
      if (!inF || !/^sql$/i.test(lang)) return;
      if (/\b(LIKE|PATINDEX|ESCAPE)\b/i.test(l)) return;   // 패턴 문자열의 대괄호는 식별자가 아니다
      for (const m of l.matchAll(/\[([A-Za-z가-힣][A-Za-z0-9_가-힣]*)\]/g)) {
        const id = m[1];
        if (id.length <= 2 || /^(dbo|sys|INFORMATION_SCHEMA)$/i.test(id)) return;
        if (!known.has(id) && !alias.has(id)) hits.push(n + ':' + (i + 1) + ': [' + id + ']');
      }
    });
  }
  hits.length ? F('V14', '기준선에 없는 식별자를 계획 SQL 이 참조 ' + hits.length + '건', [...new Set(hits)].join('\n'))
              : P('V14', '계획 SQL 의 대괄호 식별자 전부 기준선에 실재');
}

// V15 R2 수치 사본 — 게이트가 보지 않던 부류다.
// V08 은 `CHECK n개`·`DF n` 패턴만 보므로 `DEFAULT 14행`·`Parameter 87행`·`FK 6` 처럼
// 산문에 흩어진 R2 값을 아무도 검사하지 않았고, 실제로 5건이 R3 재봉인을 통과했다
// (그중 plans/08 의 VER-005 는 실행되는 조건문이라 배포 검증을 깨뜨렸을 것이다).
// R2 값과 R3 값이 짝인 수치만 센다. 예외는 정확한 문자열로 고정한다 —
// "같은 줄에 R2 가 있으면 통과" 같은 휴리스틱은 그 자체가 우회 통로다.
{
  const R2NUM = [
    ['FK 6',          /\bFK\s*6(?![0-9])/],
    ['컬럼 55',        /(?<![0-9])55\s*(?:개\s*)?(?:컬럼|행)|컬럼\s*55(?![0-9])/],
    // ['CHECK 22'] 는 뺀다 — R2 값이자 **R3 최종값**이다(04 §10.1 실측 CK=22).
    // `검사항목` 이 사라지며 23 -> 22 로 되돌아왔다. R2/R3 짝이 아니므로 여기서 셀 수 없고,
    // CHECK 수치는 V08 이 04 §8 실측과 직접 대조하므로 보호가 사라지지도 않는다.
    ['DEFAULT 14',    /\bDF\s+14(?![0-9])|Default[^0-9\n]{0,18}?(?<![0-9])14\s*(?:개|행)|DEFAULT\s*\*{0,2}\s*14\s*행/],
    ['Parameter 87',  /Parameter\s*\*{0,2}\s*87(?![0-9])|(?<![0-9])87\s*(?:개|행)/],
    ['수검자 29',      /(?<![0-9])29\s*(?:개\s*)?컬럼|컬럼\s*29(?![0-9])|스키마\s*29(?![0-9])/],
    ['Test 234',      /(?<![0-9])234\s*건|\(234\)/],
    ['상태 3값',       /RSV\s*\/\s*RCP\s*\/\s*CNL|'RSV'\s*,\s*'RCP'\s*,\s*'CNL'/],
    ['CNL',           /\bCNL\b/],
    ['R2 테이블명',    /\b(?:INFO_|MST_|HIS_)[A-Z][A-Z_0-9]*/],
    ['R2 기준선 ID',   /HC-RSV-RCP-20260903-R2/],
    // R3 재봉인이 `검사항목` 을 흡수해 테이블이 7 -> 6 이 되었다. 그 여파의 수치 짝이다.
    // 04 뒷부분(§4·§5·§13~§17)·05 §1·06 §12·plans/01·07·08 이 이 부류로 통째로 남아 있었고
    // 어느 게이트도 보지 않았다 — V08 은 CHECK/DEFAULT 만, 위 목록은 R2 55컬럼 계열만 봤다.
    // `7개 Table` 처럼 한글 수량사 + 영문 명사가 섞인 형태를 초판이 놓쳤다(06 §11 제목).
    ['테이블 7',      /7\s*개\s*(?:테이블|Table)|(?:테이블|Table)\s*7(?![0-9])/i],
    ['컬럼 47',       /47\s*컬럼|컬럼\s*47(?![0-9])|\b47\s*행\s*전건/],
    ['PK 7',          /\bPK\s*7(?![0-9])|\bPK7\b/],
    ['FK 4',          /\bFK\s*4(?![0-9])|\bFK4\b/],
    // [!] 아래 두 패턴은 뺐다. **되돌아온 수치는 R2/R3 짝이 성립하지 않는다.**
    //   CHECK  22(R2) -> 23(R3) : CK_검사코드_CODE_FORMAT 신설로 23 이 최종값
    //   NCI     5(R2) -> 4      -> 5(R3) : IX_변경이력_TARGET 신설로 5 로 되돌아옴
    // 이 목록은 "R2 에만 있던 값" 만 셀 수 있다. 값이 한 바퀴 돌아 제자리로 오면
    // 정상 수치를 잔재로 오탐한다 — 실제로 두 번 다 그렇게 됐다.
    // 개수의 항구적 보호는 V08 이다. V08 은 04 §8 의 이름을 세어 실측과 직접 대조하므로
    // 값이 어디로 움직이든 따라간다. 아래 목록은 "이름·상태값" 같은 되돌아오지 않는 것에 쓴다.
    ['수검자 17',      /수검자[^0-9\n]{0,6}17\s*(?:컬럼|행)|17개\s*컬럼|스키마\s*17컬럼/],
  ];
  // 정당한 예외 — R2 시점을 서술하는 역사 기록과, R2/R3 와 무관하게 "만들지 않는" 테이블 이름.
  const ALLOW = [
    'MST_NATIONAL_EXAMS', 'MST_ADDITIONAL_EXAMS',
    'R2의 29개 컬럼에서',
    '55개 컬럼 중 **2개**',
    // plans/09·10 은 7 -> 6 전이 그 자체를 기록한 문서다. 전이 기록에서 옛 값을 지우면
    // 무엇이 바뀌었는지 읽을 수 없게 된다. 정확한 문자열로만 연다.
    '물리 테이블 7개의 컬럼명 47개',
    '## 3. 명명표 ― 컬럼 47개',
    'Key 컬럼이 없어졌다. NCI 5 -> 4',
    '### `T46` `수검자` 17컬럼 + `CelNumberS` 계산열 전환',
    '검사항목 테이블 삭제 (테이블 7 -> 6)',
    '테이블 7 -> 9, TVF 2개의 조인 1 -> 2',
    'SCH-001  사용자 테이블  7 -> 6',
    'SCH-004  PK  7 -> 6',
    'SCH-005  FK  4 -> 2',
  ];
  const targets = [];
  for (const f of fs.readdirSync(path.join(ROOT, 'baseline')).filter(f => /^0[0-5].*\.md$/.test(f)))
    targets.push(path.join(ROOT, 'baseline', f));
  targets.push(SPEC, ...planFiles);
  const DB = path.resolve(__dirname, '..');
  for (const f of ['Rebuild.sql', 'Deploy.sql']) targets.push(path.join(DB, f));
  // 세션이 가장 먼저 읽는 파일이므로 R2 이름이 남으면 가장 오래 오해를 만든다.
  // 실제로 §6 이 필터형 인덱스를 R2 테이블명으로 설명한 채 R3 재봉인을 통과했다.
  targets.push(path.join(DB, 'CLAUDE.md'));
  for (const d of ['deploy', 'tests'])
    for (const f of fs.readdirSync(path.join(DB, d)).filter(f => f.endsWith('.sql')))
      targets.push(path.join(DB, d, f));

  const hits = [];
  for (const f of targets) {
    const buf = fs.readFileSync(f);
    const src = buf.slice(buf[0] === 0xEF ? 3 : 0).toString('utf8');
    src.split(/\r?\n/).forEach((l, i) => {
      if (ALLOW.some(a => l.includes(a))) return;
      for (const [nm, re] of R2NUM)
        if (re.test(l)) hits.push(path.basename(f) + ':' + (i + 1) + ' [' + nm + '] ' + l.trim().slice(0, 76));
    });
  }
  // 구분자 문자열 안의 수치는 위 패턴이 보지 못한다.
  // 'INVENTORY|6|4|16|1|6|2|2|1|5|24|8|0|0|19|2' 의 6번째 칸이 Foreign Key 수인데
  // \bFK\s*6 은 그것을 FK 로 읽지 못한다 — plans/08 의 RBD-004·005 가 이 구멍으로 들어왔다.
  // 패턴 대신 위치로 짚어 기준선 04 실측과 대조한다(V08 과 같은 방식이다).
  // 유일한 가정은 "6번째 칸이 FK" 라는 위치다. 칸 수가 15 가 아니면 그 가정이 깨진 것이므로
  // 조용히 넘기지 않고 형상 자체를 FAIL 로 낸다 — fail-open 을 남기지 않는다.
  {
    // 지문 칸 순서 (tests/14_Clean_Rebuild_Verify.sql (1) 참조). FK 는 6번째(index 5)다.
    //   Table | TVF | SP | Sequence | PK | FK | UQ | UX | NCI | CHECK | DEFAULT | Trigger | TVP | Exam | Holiday
    // CK_예약접수_EXAM_PAIR 신설(2026-09-07) 때 CHECK·DEFAULT·TVP 3칸을 더해 12 -> 15 가 됐다.
    const FP_ARITY = 15;
    const fk = uniq(/\`FK_[^\`\n]+\`/g);
    for (const f of targets) {
      const buf = fs.readFileSync(f);
      const src = buf.slice(buf[0] === 0xEF ? 3 : 0).toString('utf8');
      src.split(/\r?\n/).forEach((l, i) => {
        const m = l.match(/INVENTORY((?:\|\d+)+)/);
        if (!m) return;
        const cols = m[1].slice(1).split('|');
        const at = path.basename(f) + ':' + (i + 1);
        if (cols.length !== FP_ARITY)
          hits.push(at + ' [지문 형상] 칸 ' + cols.length + '개 — ' + FP_ARITY + '개가 아니면 FK 위치 가정이 깨진다');
        else if (+cols[5] !== fk)
          hits.push(at + ' [지문 FK] 선언=' + cols[5] + ' / 기준선 04 실측=' + fk + '  ' + l.trim().slice(0, 56));
      });
    }
  }

  hits.length ? F('V15', 'R2 수치·식별자 사본 ' + hits.length + '건', hits.join('\n'))
              : P('V15', 'R2 수치·식별자 사본 0건 (' + targets.length + '개 파일)');
}

// V16 CP949 에 없는 문자를 담은 `N` 없는 리터럴 — 조용히 `?` 로 바뀐다.
// varchar 리터럴은 DB 정렬(Korean_Wansung = CP949)로 해석된다. 한글은 살아남고
// —(U+2014) 같은 기호만 사라지므로 exit code 에도 PASS 건수에도 드러나지 않는다(실측 확인).
// 실행이 잡아주지 않는 부류라서 게이트가 유일한 방어선이다.
//
// 금지문자를 손으로 나열하면 실제로 틀린다 —
// —(U+2014)·–(U+2013) 은 불가인데 ―(U+2015)··(U+00B7)·→(U+2192)·§·…·≥ 는 전부 가능이다.
// 그래서 자모집합을 하드코딩하지 않고 역산한다.
//
//   CP949 = KS X 1001 ∪ 한글 음절 11172자
//
// KS X 1001 쪽은 Node 의 TextDecoder('euc-kr') 로 2바이트 조합을 전수 디코드해 뽑는다(≈130ms).
// UHC 확장 영역이 더하는 것은 **한글 음절뿐**이라, 음절 범위를 합집합으로 얹으면 정확해진다.
// (그 디코더만 쓰면 확장 음절 8822자가 전부 오탐이 된다 — 뷁·똠 이 디코드 실패한다.)
// 이 등식은 저장소의 실제 리터럴 전건을 iconv 와 대조해 확인했다.
{
  // 주석을 걷어내고 리터럴만 남긴다. '' 는 리터럴 안의 이스케이프다.
  const literals = (sql, base) => {
    const out = []; let i = 0, line = base;
    while (i < sql.length) {
      const c = sql[i];
      if (c === '\n') { line++; i++; continue; }
      if (c === '-' && sql[i + 1] === '-') { while (i < sql.length && sql[i] !== '\n') i++; continue; }
      if (c === '/' && sql[i + 1] === '*') {
        i += 2;
        while (i < sql.length && !(sql[i] === '*' && sql[i + 1] === '/')) { if (sql[i] === '\n') line++; i++; }
        i += 2; continue;
      }
      if (c === "'") {
        const nPrefix = i > 0 && /[Nn]/.test(sql[i - 1]) && (i < 2 || !/[A-Za-z0-9_@#$]/.test(sql[i - 2]));
        const start = line; let body = ''; i++;
        while (i < sql.length) {
          if (sql[i] === "'" && sql[i + 1] === "'") { body += "'"; i += 2; continue; }
          if (sql[i] === "'") { i++; break; }
          if (sql[i] === '\n') line++;
          body += sql[i]; i++;
        }
        if (!nPrefix) out.push({ body, line: start });
        continue;
      }
      i++;
    }
    return out;
  };

  // 마크다운은 ```sql 코드펜스만, .sql 은 파일 전체가 대상이다.
  const sqlBlocks = src => {
    const out = []; let inF = false, lang = '', buf = [], start = 0;
    src.split('\n').forEach((l, i) => {
      const m = l.match(/^\s*```(\w*)/);
      if (m) {
        if (!inF) { inF = true; lang = m[1]; buf = []; start = i + 2; }
        else { if (/^sql$/i.test(lang)) out.push({ body: buf.join('\n'), line: start }); inF = false; }
        return;
      }
      if (inF) buf.push(l);
    });
    return out;
  };

  const found = new Map();                       // 문자 -> 발견 위치들
  const note = (file, lits) => lits.forEach(L => {
    for (const ch of L.body) if (ch.codePointAt(0) > 127) {
      if (!found.has(ch)) found.set(ch, []);
      found.get(ch).push(file + ':' + L.line + ': ' + L.body.trim().slice(0, 72));
    }
  });

  for (const f of [SPEC, ...planFiles])
    for (const b of sqlBlocks(read(f))) note(path.basename(f), literals(b.body, b.line));

  const DB2 = path.resolve(__dirname, '..');
  const sqlFiles = [path.join(DB2, 'Deploy.sql'), path.join(DB2, 'Rebuild.sql')];
  for (const d of ['deploy', 'tests'])
    for (const f of fs.readdirSync(path.join(DB2, d)).filter(f => f.endsWith('.sql')))
      sqlFiles.push(path.join(DB2, d, f));
  for (const f of sqlFiles) {
    const buf = fs.readFileSync(f);
    note(path.basename(f), literals(buf.slice(buf[0] === 0xEF ? 3 : 0).toString('utf8'), 1));
  }

  const chars = [...found.keys()];
  if (!chars.length) { P('V16', 'N 없는 리터럴에 비-ASCII 문자 자체가 없다'); }
  else {
    let ksx;
    try {
      const dec = new TextDecoder('euc-kr', { fatal: true });
      ksx = new Set(); const b = Buffer.alloc(2);
      for (let hi = 0x81; hi <= 0xFE; hi++) for (let lo = 0x41; lo <= 0xFE; lo++) {
        b[0] = hi; b[1] = lo;
        try { const s = dec.decode(b); if (s.length === 1) ksx.add(s); } catch (e) { /* 그 조합은 CP949 에 없다 */ }
      }
    } catch (e) { ksx = null; }

    if (!ksx || ksx.size < 8000) {
      // 판정하지 못한 검사는 통과가 아니다 (database/CLAUDE.md §10).
      F('V16', 'euc-kr 디코더를 쓸 수 없어 CP949 표현 가능성을 판정할 수 없다 — 미실행은 PASS 가 아니다');
    } else {
      const ok = ch => { const c = ch.codePointAt(0); return c < 0x80 || (c >= 0xAC00 && c <= 0xD7A3) || ksx.has(ch); };
      const hits = [];
      for (const ch of chars) {
        if (ok(ch)) continue;
        const cp = 'U+' + ch.codePointAt(0).toString(16).toUpperCase().padStart(4, '0');
        for (const w of found.get(ch)) hits.push(cp + ' ' + ch + '  ' + w);
      }
      hits.length ? F('V16', 'CP949 에 없는 문자를 담은 N 없는 리터럴 ' + hits.length + '건 — N 접두사를 붙인다', hits.join('\n'))
                  : P('V16', 'N 없는 리터럴에 CP949 밖 문자 0건 (' + chars.length + '종 검사)');
    }
  }
}


// V17 RS0 의 5컬럼 타입을 배포 SQL 에서 정적으로 대조한다.
//   sys.dm_exec_describe_first_result_set_for_object 는 sp_getapplock 을 부르는 SP 에서
//   Msg 11520 으로 실패한다 - 그 내부가 확장 프로시저 sys.xp_userlock 을 부르기 때문이다(실측).
//   Write SP 8개가 전부 그 부류라 DMV 로는 16/16 을 볼 수 없다(스펙 §36 [X 실측]).
//   verify-contract.js 는 실측 출력에서 **컬럼명**만 보므로 타입이 비어 있었다. 여기서 메꾼다.
{
  const files = fs.readdirSync('deploy').filter(f => /^0[4-7]_.*\.sql$/.test(f));
  const want = [
    [/CAST\([^()]*(?:\([^()]*\))?[^()]*AS BIT\)\s*AS Success/,               'Success BIT'],
    [/CAST\([^()]*AS INT\)\s*AS Code/,                                        'Code INT'],
    [/CAST\([^()]*AS NVARCHAR\(300\)\)\s*AS Message/,                        'Message NVARCHAR(300)'],
    [/CAST\([^()]*AS VARCHAR\(50\)\)\s*AS Field/,                            'Field VARCHAR(50)'],
    [/CAST\([^()]*AS DATETIME2\(7\)\)\s*AS ServerTime/,                      'ServerTime DATETIME2(7)'],
  ];
  const bad = []; let blocks = 0;
  for (const f of files) {
    const sql = fs.readFileSync('deploy/' + f, 'utf8');
    const lines = sql.split(/\r?\n/);
    lines.forEach((l, i) => {
      if (!/\bAS Success\b/.test(l)) return;
      blocks++;
      // RS0 는 5줄 연속이다. 여유를 두고 8줄을 본다.
      const win = lines.slice(i, i + 8).join('\n');
      for (const [re, label] of want) {
        if (!re.test(win)) bad.push(f + ':' + (i + 1) + '  ' + label + ' CAST 없음');
      }
    });
  }
  if (!blocks) F('V17', '배포 SQL 에서 RS0 블록을 하나도 찾지 못했다 - 미실행은 PASS 가 아니다');
  else if (bad.length) F('V17', 'RS0 5컬럼 CAST 누락 ' + bad.length + '건', [...new Set(bad)].join('\n'));
  else P('V17', 'RS0 블록 ' + blocks + '개 전부 5컬럼 명시 CAST (' + files.length + '개 배포 파일)');
}


// V18 RBK-008 — 실패 응답의 Result Set 개수는 RS0 1개뿐이다 (05 §3.5).
//   T-SQL 로는 셀 수 없다. INSERT … EXEC 는 RS 2개 이상에서 Msg 213 이고(스펙 §33.1a),
//   DMV 는 sp_getapplock 을 부르는 Write SP 에서 Msg 11520 이다(스펙 §36 실측).
//   그래서 tests/08 이 NOT RUN 으로 남기고 여기서 정적으로 판정한다.
//   유일한 예외는 INSERT_수검자 의 202·203 이다 - 기존 수검자·중복후보 Dialog 를 위해
//   실패에도 RS1 을 동반한다. 그 둘 말고 rs0Success = 0 인 시나리오가 RS 를 2개 이상
//   선언했다면 계약 위반이거나 기대값이 틀린 것이다.
{
  const cp = 'tools/expected-contracts.json';
  if (!fs.existsSync(cp)) F('V18', cp + ' 이 없다 - 미실행은 PASS 가 아니다');
  else {
    const j = JSON.parse(fs.readFileSync(cp, 'utf8'));
    const bad = []; let n = 0;
    for (const [k, v] of Object.entries(j)) {
      if (v.rs0Success !== 0) continue;
      n++;
      const rs = Array.isArray(v.resultSets) ? v.resultSets.length : 0;
      const exempt = v.rs0Code === 202 || v.rs0Code === 203;
      if (exempt) { if (rs !== 2) bad.push(k + '  Code=' + v.rs0Code + ' 은 RS 2개여야 한다 (관측 ' + rs + ')'); }
      else if (rs !== 1) bad.push(k + '  Code=' + v.rs0Code + ' 실패인데 RS ' + rs + '개');
    }
    if (!n) F('V18', '실패 시나리오가 하나도 없다 - 기대값 파일을 확인한다');
    else if (bad.length) F('V18', '실패 응답에 후속 Result Set 이 붙은 시나리오 ' + bad.length + '건', bad.join('\n'));
    else P('V18', 'RBK-008 실패 시나리오 ' + n + '건 전부 RS0 1개만 (202·203 은 RS1 동반)');
  }
}

console.log('\n=== verify-docs: PASS ' + pass + ' / FAIL ' + fail + ' ===');
process.exit(fail ? 1 : 0);
