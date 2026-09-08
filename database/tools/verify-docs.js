#!/usr/bin/env node
// Phase 4 문서 정합성 게이트.
// 스펙(계약의 단일 출처)과 계획(실행의 단일 출처)이 어긋나지 않는지 기계적으로 판정한다.
// 사람이 표를 보고 "반영했다"고 적는 것을 신뢰하지 않는다 — 실제 문자열을 센다.
'use strict';
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..', 'docs');
// 06 은 2026-09-08 에 docs/baseline/ 으로 입주했다 (ROOT CLAUDE.md §2.1). 봉인 대상이지만
// 여기서는 계약의 출처로 **읽기만** 한다 — plans 가 이 문서에 근거를 두는지 판정하기 위해서다.
const SPEC = path.join(ROOT, 'baseline', '06_DB_Transaction_Security_Seed.md');
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
const REPO = path.resolve(__dirname, '..', '..');
// `CLAUDE.md` 는 `@AGENTS.md` 한 줄 스텁이다. 스텁을 가리키는 검사는 아무것도 훑지 않으면서
// 초록이 되므로, 대상 목록은 파일 이름이 아니라 내용을 따라간다.
const followImport = f => {
  const m = /^@(\S+\.md)\s*$/.exec(read(f));
  return m ? path.join(path.dirname(f), m[1]) : f;
};
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
  // 전이 자체를 기록한 줄 — 그때의 기대값을 지금 값으로 고치면 기록이 거짓이 된다.
  // V15 의 ALLOW 와 같은 원칙이다: 휴리스틱이 아니라 **정확한 문자열**로만 연다.
  // 살아 있는 선언에는 예외가 없다. 이 목록에 넣기 전에 "이 줄이 지금을 말하는가,
  // 그때를 말하는가" 를 먼저 답해야 한다.
  const HISTORY = [
    '기대값도 테이블 6 · NCI 5 · SP 16 · CHECK 24 로 바뀌었다.',
  ];
  const claims = [];
  const scan = (name, src) => src.split('\n').forEach((l, i) => {
    let m;
    if (HISTORY.some(h => l.includes(h))) return;
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
      // 개명표의 '현재' 칸은 사라진 이름을 적을 수밖에 없다. 같은 줄이 살아 있는 이름을
      // 함께 적고 있으면 그것은 개명 한 행이지 매달린 참조가 아니다.
      // 홀로 있는 미정의 이름은 그대로 걸린다 — 게이트가 약해지지 않는다.
      const renameRow = (l.match(OBJ) || []).some(o => known.has(o));
      for (const o of [...(l.match(OBJ) || []), ...(l.match(TBLR2) || [])])
        if (!known.has(o) && !renameRow) hits.push(n + ':' + (i + 1) + ': ' + o);
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
  for (const m of read(BASE05).matchAll(/\`@?([A-Za-z가-힣][A-Za-z0-9_가-힣]*)\`/g)) known.add(m[1]); // 05 반환 컬럼·Parameter (R4 로 한글이 되었다 — ASCII 만 모으면 fail-open 한다)
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
    // [!] ['수검자 17'] 을 뺐다. **세 번째 되돌아온 수치다.**
    //     R2 17 -> R3 16 -> R7 17 (행버전 신설). 바로 위 두 주석이 예고한 그대로
    //     정상값을 R2 잔재로 오탐했다. 컬럼 수의 항구적 보호는 DOC-001 이다 —
    //     04 §8 의 컬럼 집합과 실제 DB 를 양방향 대조하므로 값이 어디로 움직여도 따라간다.
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
  // 이름이 아니라 **내용**을 따라간다 — 2026-09-08 에 본문이 CLAUDE.md 에서 AGENTS.md 로
  // 옮겨가자 이 줄이 11바이트 `@AGENTS.md` 스텁을 훑으며 조용히 PASS 했다. 초록인 채 눈이 멀었다.
  targets.push(followImport(path.join(DB, 'CLAUDE.md')));
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
  // [X] 여기만 상대경로였다 — 저장소 루트에서 돌리면 ENOENT 로 죽었다. 다른 검사는 전부 절대경로다.
  const DBV17 = path.resolve(__dirname, '..');
  const files = fs.readdirSync(path.join(DBV17, 'deploy')).filter(f => /^0[4-7]_.*\.sql$/.test(f));
  // R4 한글화 뒤 RS0 은 [성공여부]·[결과코드]·[결과메시지]·[오류항목]·[서버시각] 이다.
  // [오류항목] 은 담기는 값이 한글 Parameter 이름이 되었으므로 NVARCHAR(50) 이다 (05 §3.1).
  const want = [
    [/CAST\([^()]*(?:\([^()]*\))?[^()]*AS BIT\)\s*AS \[성공여부\]/,          '성공여부 BIT'],
    [/CAST\([^()]*AS INT\)\s*AS \[결과코드\]/,                                '결과코드 INT'],
    [/CAST\([^()]*AS NVARCHAR\(300\)\)\s*AS \[결과메시지\]/,                  '결과메시지 NVARCHAR(300)'],
    [/CAST\([^()]*AS NVARCHAR\(50\)\)\s*AS \[오류항목\]/,                     '오류항목 NVARCHAR(50)'],
    [/CAST\([^()]*AS DATETIME2\(7\)\)\s*AS \[서버시각\]/,                     '서버시각 DATETIME2(7)'],
  ];
  const bad = []; let blocks = 0;
  for (const f of files) {
    const sql = fs.readFileSync(path.join(DBV17, 'deploy', f), 'utf8');
    const lines = sql.split(/\r?\n/);
    lines.forEach((l, i) => {
      if (!/AS \[성공여부\]/.test(l)) return;
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


// V19 감사 INSERT 가 RS1 보다 **앞**인가.
// [X] 감사를 RS1 뒤에 두면 RS0 만 읽고 끊은 호출에서 업무는 커밋됐는데 감사만 없는 상태가 된다
//     (§43-19). 순서는 눈에 안 띄게 되돌아갈 수 있어 정적으로 고정한다.
//     각 Write SP 본문에서  [6] Result Set  <  [7] 감사 기록  <  [6b] RS1  이어야 한다.
{
  const hits = [];
  let bodies = 0;
  for (const f of ['05_Procedures_Patient_Write.sql', '06_Procedures_Reservation_Write.sql',
                   '07_Procedures_Reception_Write.sql']) {
    const txt = fs.readFileSync(path.join(path.resolve(__dirname, '..'), 'deploy', f), 'utf8');
    for (const body of txt.split(/CREATE OR ALTER PROCEDURE/).slice(1)) {
      const a = body.indexOf('-- [6] ');   // 문구가 SP 마다 다르다 — 번호만 본다
      const b = body.indexOf('-- [7] 감사 기록');
      const c = body.indexOf('-- [6b] RS1');
      if (a < 0 || b < 0 || c < 0) { hits.push(f + ' 표지 누락 [6]=' + a + ' [7]=' + b + ' [6b]=' + c); continue; }
      bodies++;
      if (!(a < b && b < c)) hits.push(f + ' 순서 어긋남 [6]=' + a + ' [7]=' + b + ' [6b]=' + c);
    }
  }
  if (bodies !== 8) hits.push('Write SP 본문 ' + bodies + '개 (기대 8)');
  hits.length ? F('V19', '감사/RS1 순서 위반 ' + hits.length + '건', hits.join(String.fromCharCode(10)))
              : P('V19', '감사 INSERT 가 RS1 보다 앞 (Write SP ' + bodies + '개)');
}

// V18 RBK-008 — 실패 응답의 Result Set 개수는 RS0 1개뿐이다 (05 §3.5).
//   T-SQL 로는 셀 수 없다. INSERT … EXEC 는 RS 2개 이상에서 Msg 213 이고(스펙 §33.1a),
//   DMV 는 sp_getapplock 을 부르는 Write SP 에서 Msg 11520 이다(스펙 §36 실측).
//   그래서 tests/08 이 NOT RUN 으로 남기고 여기서 정적으로 판정한다.
//   유일한 예외는 INSERT_수검자 의 202·203 이다 - 기존 수검자·중복후보 Dialog 를 위해
//   실패에도 RS1 을 동반한다. 그 둘 말고 rs0Success = 0 인 시나리오가 RS 를 2개 이상
//   선언했다면 계약 위반이거나 기대값이 틀린 것이다.
{
  const cp = path.join(__dirname, 'expected-contracts.json');
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

// V20 화면·C# 이름 ↔ DB 계약 이름의 다리 (05 §16.5).
// R4 가 04·05 를 한글로 바꾸면서 00·01·03 의 영문 호출계약과 이름이 갈렸다. 두 계층이
// 공존하는 것은 §16.2 의 결정이지만, **대응표가 없으면 00→05 를 따라 읽는 사람이 끊긴다.**
// 실제로 04 가 "와이어프레임의 PatientId" 를 한글로 잘못 바꿔 인용이 거짓이 된 적이 있다.
// 표가 썩는 두 방향을 다 본다 — 화면 이름이 00·01·03 에 없거나, DB 이름이 05 에 없거나.
{
  const ui = ['00_Project_Policy.md', '01_Process_Definition.md', '03_Wireframe_Definition.md']
    .map(f => read(path.join(ROOT, 'baseline', f))).join('\n');
  const sec = sliceSection(read(BASE05), '화면·C# 이름 ↔ DB 계약 이름 대응');
  const rows = [...sec.matchAll(/^\|\s*`([A-Za-z][A-Za-z0-9_]*)`\s*\|\s*`([^`]+)`\s*\|/gm)];
  const base05 = read(BASE05);
  const bad = [];
  for (const [, en, ko] of rows) {
    if (!new RegExp('(?<![A-Za-z0-9_])' + en + '(?![A-Za-z0-9_])').test(ui))
      bad.push('화면 이름 `' + en + '` 이 00·01·03 에 없다 — 죽은 행이다');
    if (!base05.includes('`' + ko + '`') && !base05.includes('@' + ko))
      bad.push('DB 이름 `' + ko + '` 이 05 에 없다 — 지어낸 이름이다');
  }
  if (!rows.length) F('V20', '05 §16.5 대응표를 찾지 못했다 - 미실행은 PASS 가 아니다');
  else if (bad.length) F('V20', '이름 대응표가 어긋난다 ' + bad.length + '건', bad.join('\n'));
  else P('V20', '화면·C# ↔ DB 이름 대응 ' + rows.length + '종 양방향 실재 (05 §16.5)');
}

// V21 지침 §참조가 실재하는 절을 가리키는가.
// 절 번호는 저장소 안에서 92곳이 이름으로 부르는 사실상의 공개 API 이고, 그중 봉인 문서
// (04·06)에서 나온 것이 있어 고치려면 재봉인이다. 그런데 그 참조를 보는 검사가 없었다 —
// V15 가 스텁을 훑으며 초록이던 것과 같은 구조의 구멍이었다. 절을 지우거나 번호를 옮기면
// 여기서 FAIL 이 난다. 그때 고칠 것은 참조가 아니라 절을 움직인 쪽이다.
//
// 해석 규칙(실측 92건 전부와 일치):
//   `database/` 접두 -> database/AGENTS.md      `ROOT `·`루트 ` 접두 -> ROOT AGENTS.md
//   접두 없는 CLAUDE.md -> database/AGENTS.md   (절을 가진 CLAUDE.md 는 그쪽뿐이었다)
//   접두 없는 AGENTS.md -> ROOT AGENTS.md       (모호하면 FAIL 이 나서 수식을 강제한다)
// 옛 기록이 `CLAUDE.md §N` 이라 부르는 것은 그때 그 이름이었기 때문이고 절 번호는 같다
// (database/AGENTS.md 머리말). 기록을 고쳐 쓰지 않으므로 검사 쪽이 이름을 정규화한다.
{
  const SECRE = /^#{2,6}[ ]+(\d+(?:\.\d+)?)\.?[ ]/gm;
  const secsOf = f => new Set([...read(f).matchAll(SECRE)].map(m => m[1]));
  const DOC = { root: path.join(REPO, 'AGENTS.md'), db: path.join(REPO, 'database', 'AGENTS.md') };
  const SEC = { root: secsOf(DOC.root), db: secsOf(DOC.db) };

  const files = [];
  (function walk(d) {
    for (const e of fs.readdirSync(d, { withFileTypes: true })) {
      if (/^(node_modules|\.git|\.claude|output)$/.test(e.name)) continue;
      const p = path.join(d, e.name);
      if (e.isDirectory()) walk(p);
      else if (/\.(md|sh|js|sql|json)$/.test(e.name)) files.push(p);
    }
  })(REPO);

  const REFRE = /(ROOT|루트)?[ ]*`?(database\/)?(AGENTS|CLAUDE)\.md`?[ ]*§(\d+(?:\.\d+)?)/g;
  let n = 0; const bad = [];
  for (const f of files) {
    const buf = fs.readFileSync(f);
    buf.slice(buf[0] === 0xEF ? 3 : 0).toString('utf8').split(/\r?\n/).forEach((l, i) => {
      for (const m of l.matchAll(REFRE)) {
        n++;
        const db = !!m[2] || (!m[1] && m[3] === 'CLAUDE');
        if (SEC[db ? 'db' : 'root'].has(m[4])) continue;
        bad.push(path.relative(REPO, f).split(path.sep).join('/') + ':' + (i + 1) +
                 '  `' + m[0].trim() + '` -> ' + (db ? 'database/' : '') + 'AGENTS.md 에 §' + m[4] + ' 가 없다');
      }
    });
  }
  if (!n) F('V21', '지침 §참조를 하나도 찾지 못했다 - 미실행은 PASS 가 아니다');
  else if (bad.length) F('V21', '죽은 지침 §참조 ' + bad.length + '건', bad.join('\n'));
  else P('V21', '지침 §참조 ' + n + '건 전부 실재하는 절 (ROOT ' + SEC.root.size + '절 · database ' + SEC.db.size + '절)');
}

// V22 `.sql` 의 UTF-8 BOM. 없으면 sqlcmd 가 한글 객체명을 깨뜨려 Msg 105/102 가 난다
// (database/AGENTS.md §5). §5 의 다른 절반인 `N` 접두사는 V16 이 지켜 왔지만 BOM 쪽은
// 관행만이 지키고 있었다 — 27개 파일이 우연히 전부 갖고 있었을 뿐 어떤 게이트도 보지 않았다.
{
  const DB3 = path.resolve(__dirname, '..');
  const files = [path.join(DB3, 'Deploy.sql'), path.join(DB3, 'Rebuild.sql')];
  for (const d of ['deploy', 'tests', 'scripts'])
    for (const f of fs.readdirSync(path.join(DB3, d)).filter(f => f.endsWith('.sql')))
      files.push(path.join(DB3, d, f));
  const bad = files.filter(f => {
    const b = fs.readFileSync(f);
    return !(b[0] === 0xEF && b[1] === 0xBB && b[2] === 0xBF);
  }).map(f => path.relative(DB3, f).split(path.sep).join('/'));
  if (!files.length) F('V22', '.sql 을 하나도 찾지 못했다 - 미실행은 PASS 가 아니다');
  else if (bad.length) F('V22', 'UTF-8 BOM 이 없는 .sql ' + bad.length + '건 — sqlcmd 가 한글 객체명을 깨뜨린다', bad.join('\n'));
  else P('V22', '.sql ' + files.length + '개 전부 UTF-8 BOM (§5)');
}

// V23 살아 있는 문서가 가리키던 파일이 사라졌는가.
// V21 이 §번호를 지키듯 이것은 **경로**를 지킨다. 실제로 `06` 이 docs/phase4 -> docs/baseline 으로
// 입주한 뒤에도 database/README.md 가 옛 경로를 가리킨 채 두 회차를 통과했다(실측).
//
// 대상은 **지시로 읽히는 문서**뿐이다. 기록(docs/phase4/plans · database/docs)은 뺀다 —
// 그때 그 파일이 있었다는 것이 사실이므로 기록을 고쳐 쓰지 않는다.
//
// 판정은 "없다" 가 아니라 **"있었는데 사라졌다"** 다. 아직 안 만든 파일(`07`)이나 폐기한 설계
// (`tests/13_Security_Tests.sql`)를 예외 목록으로 관리하면 그 목록이 또 손으로 맞춰야 하는 값이 된다.
// 이력이 대신 판정하므로 목록이 필요 없다.
{
  const LIVE = [];
  const addLive = p => { if (fs.existsSync(path.join(REPO, p))) LIVE.push(p); };
  addLive('AGENTS.md'); addLive('CLAUDE.md'); addLive('README.md');
  addLive('database/AGENTS.md'); addLive('database/README.md');
  addLive('docs/phase4/reseal-history.md');
  for (const d of ['docs/baseline', 'docs/phase5', 'docs/redesign'])
    if (fs.existsSync(path.join(REPO, d)))
      for (const f of fs.readdirSync(path.join(REPO, d)))
        if (f.endsWith('.md')) addLive(d + '/' + f);

  // [X] `git log --diff-filter=A` 로는 부족하다 — 이름이 바뀌어 들어온 파일이 `A` 로 안 잡힌다.
  //     `06` 이 `_CANDIDATE.md` 에서 개명된 탓에 실제 낡은 참조 하나를 놓쳤다(실측). 트리 전수를 쓴다.
  let ever;
  try {
    ever = new Set(require('child_process')
      .execSync('git rev-list --objects --all', { cwd: REPO, maxBuffer: 1e8 })
      .toString().split('\n').map(l => l.slice(41).trim()).filter(Boolean));
  } catch (e) { ever = null; }

  if (!ever) F('V23', 'git 이력을 읽지 못해 경로 실재를 판정할 수 없다 - 미실행은 PASS 가 아니다');
  else {
    const EXT = 'md|sql|js|sh|json|xlsx|txt|sln|csproj|cs';
    // 줄번호 접미(`파일.md:1903`)와 **경로 없는 파일명**도 받는다 — 문서가 이웃 파일을 이름만으로
    // 부르는 일이 흔하고 그 이름이 사라진 것도 썩은 참조다. 오탐은 아래 이력 조회가 막는다.
    const PRE = new RegExp('`([A-Za-z0-9_./\\-]+\\.(?:' + EXT + '))(?::[0-9]+)?`', 'g');
    // 문서는 경로를 자기 기준으로도 쓴다 — `06` 은 database/ 기준, docgen 문서는 tools/docgen 기준이다.
    const ROOTS = ['.', 'database', 'tools/docgen'];
    const hits = [];
    let seen = 0;
    for (const rel of LIVE) {
      const buf = fs.readFileSync(path.join(REPO, rel));
      const src = buf.slice(buf[0] === 0xEF ? 3 : 0).toString('utf8');
      const roots = [...ROOTS.map(r => path.join(REPO, r)), path.dirname(path.join(REPO, rel))];
      src.split(/\r?\n/).forEach((line, i) => {
        // 사라졌다는 것을 **설명하는** 문장은 위반이 아니다 — verify-tsql-allowlist.sh 가 주석을
        // 지우고 검사하는 것과 같은 처리다. `06` §32 의 "08_Security.sql 은 파일째 삭제했고" 가 그 예다.
        if (/삭제|지웠|없앴|폐기/.test(line)) return;
        for (const m of line.matchAll(PRE)) {
          const t = m[1];
          if (/[*<>?]/.test(t)) continue;              // 와일드카드·자리표시자
          if (/^~|^[A-Za-z]:/.test(t)) continue;       // 홈·절대경로 (저장소 밖)
          seen++;
          if (roots.some(r => fs.existsSync(path.resolve(r, t)))) continue;
          const cands = roots.map(r => path.relative(REPO, path.resolve(r, t)).split(path.sep).join('/'));
          const gone = cands.find(c => ever.has(c));
          if (!gone) continue;                         // 한 번도 없던 파일 = 계획·폐기. 썩은 것이 아니다
          hits.push(rel + ':' + (i + 1) + '  `' + t + '` -> ' + gone + ' 이 사라졌다');
        }
      });
    }
    hits.length ? F('V23', '가리키던 파일이 사라진 참조 ' + hits.length + '건', hits.join('\n'))
                : P('V23', '살아 있는 문서 ' + LIVE.length + '개의 경로 참조 ' + seen + '건 전부 실재');
  }
}

// V24 하드코딩 생성기가 기준선보다 뒤처졌는가.
//
// `tools/docgen` 의 생성기는 두 부류다(실측).
//   파싱형   build_02(JSZip 로 xlsx) · build_04 · build_05 · erd_04 — 실행 시점에 기준선을 읽는다.
//            기준선이 바뀌면 **다시 돌리기만** 하면 산출물이 따라온다. 검사할 것이 없다.
//   하드코딩  build_00(<-00) · proc/slides/*(<-01) · wireframe/screens/*(<-03) — 본문을 품고 있다.
//            기준선이 바뀌어도 **따라오지 않는다.** 사람이 함께 열어야 한다 (database/AGENTS.md §3).
//
// 그 규칙은 지금까지 문장으로만 있었다. git 이력이 그것을 판정할 수 있다 —
// 기준선의 마지막 커밋이 생성기의 마지막 커밋의 조상이면(또는 같으면) 생성기가 뒤처지지 않았다.
// 값을 저장하지 않으므로 손으로 맞출 것이 없다 (ROOT AGENTS.md §6).
{
  const cp = require('child_process');
  const git = a => cp.execSync('git ' + a, { cwd: REPO, maxBuffer: 1e8 }).toString().trim();
  // 기준선 -> 그 본문을 품은 생성기. 파싱형은 여기 없다.
  const MIRROR = {
    'docs/baseline/00_Project_Policy.md': 'tools/docgen/xlsx/build_00.js',
    'docs/baseline/01_Process_Definition.md': 'tools/docgen/proc',
    'docs/baseline/03_Wireframe_Definition.md': 'tools/docgen/wireframe',
  };
  const bad = [], missing = [];
  let n = 0;
  try {
    // 작업트리에서 고쳐진 것도 본다 — 재봉인 커밋을 만들기 **전에** 알려주는 쪽이 낫다.
    // `git()` 을 쓰지 않는다 — 그 헬퍼는 출력 전체를 trim 하고, porcelain 첫 줄이
    // 수정된 추적 파일(` M path`)이면 선행 공백이 잘려 slice(3) 이 경로를 한 글자 먹는다.
    // 첫 줄이 `?? path`(공백 없음)일 때만 우연히 맞았다 (실측).
    const dirty = new Set(cp.execSync('git status --porcelain', { cwd: REPO, maxBuffer: 1e8 })
      .toString().split('\n').map(l => l.slice(3).trim()).filter(Boolean));
    const touched = p => [...dirty].some(d => d === p || d.startsWith(p + '/'));

    for (const [base, gen] of Object.entries(MIRROR)) {
      if (!fs.existsSync(path.join(REPO, base)) || !fs.existsSync(path.join(REPO, gen))) {
        missing.push(base + ' 또는 ' + gen + ' 이 없다'); continue;
      }
      n++;
      if (touched(base) && !touched(gen)) {
        bad.push(gen + ' — 기준선 ' + path.basename(base) + ' 이 작업트리에서 바뀌었는데 생성기는 그대로다');
        continue;
      }
      const bc = git('log -1 --format=%H -- "' + base + '"');
      const gc = git('log -1 --format=%H -- "' + gen + '"');
      if (!bc || !gc) { bad.push(gen + ' 또는 ' + base + ' 에 커밋 이력이 없다'); continue; }
      if (bc === gc) continue;
      let anc = false;
      try { cp.execSync('git merge-base --is-ancestor ' + bc + ' ' + gc, { cwd: REPO }); anc = true; } catch (e) { anc = false; }
      if (!anc) bad.push(gen + ' — 기준선은 ' + bc.slice(0, 7) + ' 에서 바뀌었는데 생성기의 마지막 커밋은 ' + gc.slice(0, 7) + ' 이다');
    }
  } catch (e) {
    F('V24', 'git 이력을 읽지 못해 생성기 뒤처짐을 판정할 수 없다 - 미실행은 PASS 가 아니다: ' + e.message.split('\n')[0]);
    n = -1;
  }
  if (n === 0) F('V24', '기준선-생성기 대응을 하나도 확인하지 못했다 - 미실행은 PASS 가 아니다', missing.join('\n'));
  else if (n > 0) (bad.length || missing.length)
    ? F('V24', '기준선보다 뒤처진 하드코딩 생성기 ' + (bad.length + missing.length) + '건', [...bad, ...missing].join('\n'))
    : P('V24', '하드코딩 생성기 ' + n + '개가 기준선보다 뒤처지지 않았다 (파싱형은 다시 돌리면 따라오므로 대상 아님)');
}

console.log('\n=== verify-docs: PASS ' + pass + ' / FAIL ' + fail + ' ===');
process.exit(fail ? 1 : 0);
