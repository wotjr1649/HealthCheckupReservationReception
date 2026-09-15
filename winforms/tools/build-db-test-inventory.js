#!/usr/bin/env node
/*
 * DB 계약시험 목록 엑셀 생성기 — `docs/phase5/output/P5_DB시험_목록.xlsx`
 *
 * 손으로 쓰는 칸이 하나도 없다. 두 곳에서 읽어 합친다.
 *
 *   database/artifacts/logs/full_test_run_*.log (최신)
 *       --- tests/05_Patient_Write_Tests.sql      -> 구획
 *       PASS PWR-011 생년월일·성별은 계산열이 ...  -> 시험 ID · 결과 · 무엇을 재는가
 *
 *   database/tests/*.sql
 *       -- PWR-011  ... (04 §8.1.2)                -> 근거 참조
 *
 * `[!]` **설명의 단일 출처는 실행이 낸 문장이다.** 시험이 스스로 PRINT 한 것이라, 시험을
 *       고치면 설명도 함께 바뀐다. 엑셀에 따로 적으면 그 순간 값이 두 곳이 된다
 *       (ROOT AGENTS.md §6) — `build-test-inventory.js` 가 코드 주석에 대해 세운 것과
 *       같은 규칙이다.
 *
 * `[X]` **로그 없이 돌지 않는다.** 로그는 `.gitignore` 대상이라 기계마다 있고 없다 —
 *       그래서 이 생성기는 `scripts/test.sh` 에 넣지 않는다(「어디서나 같은 판정」을 깬다).
 *       `build-scenario-evidence.js` 와 같은 자리다.
 *
 * `[X]` **FAIL 이 하나라도 있으면 찍지 않는다.** 실패를 담은 목록을 산출물로 내보내면
 *       받는 사람이 그것을 완료 기록으로 읽는다. NOT RUN 은 다르다 — 세어서 그대로 싣는다
 *       (실행하지 않은 것을 통과로 읽히게 하지 않는다).
 *
 *   node tools/build-db-test-inventory.js
 */
'use strict';

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..');
const DB = path.resolve(ROOT, '..', 'database');
const LOGDIR = path.join(DB, 'artifacts', 'logs');
const TESTS = path.join(DB, 'tests');
const OUT = path.resolve(ROOT, '..', 'docs', 'phase5', 'output', 'P5_DB시험_목록.xlsx');

/*
 * 시험 ID 는 **모양이 하나가 아니다.** PWR-011 · RUL-T01 · CON-005-LOCK · V27 ·
 * G00/G01/G05 · 01_공통업무상태 가 전부 쓰인다.
 *
 * [X] **모양을 가정하면 통째로 빠진다.** 처음에는 세 자리 숫자를 가정했다가
 *     RUL-T01~T12(TGT)·N01~N12(NEX)·A01~A10(AEX) 이 전부 빠졌다 — 정책 규칙을 재는
 *     바로 그 시험들이다 (2026-09-15 실측). 그래서 **공백 없는 첫 낱말**을 ID 로 본다.
 */
const ID = '[0-9A-Za-z][^\\s]*';
const RESULT_RE = new RegExp('^(PASS|FAIL|NOT RUN|SKIP) (' + ID + ')\\s*(.*)$');
const SECTION_RE = /^--- tests\/(.+\.sql)\s*$/;

/*
 * 시험 파일 구획이 끝나는 자리. 하니스가 여기서 묶음 요약을 찍는다.
 *
 * [X] **이 줄을 안 보면 뒤따르는 게이트가 마지막 시험 파일에 딸려 들어간다** — 요약
 *     시트가 「14_Clean_Rebuild_Verify 가 V01~V28 을 잰다」고 말하게 된다.
 */
const SECTION_END_RE = /^(contract-verify|clean-rebuild|concurrency) FAILED=/;

/*
 * 하니스가 마지막에 찍는 줄. **이것이 없으면 아직 돌고 있는 로그다.**
 *
 * [X] **돌고 있는 로그를 읽어 찍은 적이 있다 (2026-09-15 실측).** 회차가 11:28 에 끝났는데
 *     11:21 에 찍어서, 410건 중 370건만 담긴 엑셀이 커밋됐다 — 빠진 40건에 **동시성
 *     CON-001~008 전부와 C# 호출 CS-001~016 이 들어 있었다.** 로그는 append 라 중간에
 *     읽어도 앞부분이 멀쩡해서, 결과물만 보면 아무 이상이 없다. 그래서 기계가 막는다.
 */
const DONE_RE = /^=== (실패 0건|실패한 단계가 있습니다)/m;

/*
 * 근거 참조. `05 §9.11` 과 `스펙 §21.1` 둘 다 쓰인다 — `스펙` 은 `06` 의 옛 이름이고
 * 계약시험이 그 시절 표기를 그대로 갖고 있다. 읽는 쪽이 맞춘다.
 */
const REF_RE = /\b(0[0-7]|스펙)\s*§\s*(\d+(?:\.\d+)*[a-z]?)/g;

let failed = 0;
function fail(line) {
  console.log('FAIL ' + line);
  failed = 1;
}

function newestLog() {
  if (!fs.existsSync(LOGDIR)) return null;
  const files = fs.readdirSync(LOGDIR)
    .filter(function (f) { return /^full_test_run.*\.log$/.test(f); })
    .map(function (f) { return path.join(LOGDIR, f); });
  if (!files.length) return null;
  files.sort(function (a, b) { return fs.statSync(b).mtimeMs - fs.statSync(a).mtimeMs; });
  return files[0];
}

/** 시험 소스의 `-- <ID>  설명` 에서 근거 참조만 거둔다. */
function refsFromSource() {
  const map = {};
  if (!fs.existsSync(TESTS)) return map;

  for (const name of fs.readdirSync(TESTS)) {
    if (!name.endsWith('.sql')) continue;
    const text = fs.readFileSync(path.join(TESTS, name), 'utf8');
    for (const line of text.split(/\r?\n/)) {
      const m = new RegExp('^--\\s*(' + ID + ')\\s+(.*)$').exec(line.trim());
      if (!m) continue;

      const refs = [];
      let r;
      REF_RE.lastIndex = 0;
      while ((r = REF_RE.exec(m[2])) !== null) {
        const doc = r[1] === '스펙' ? '06' : r[1];
        const ref = doc + ' §' + r[2];
        if (refs.indexOf(ref) === -1) refs.push(ref);
      }

      if (refs.length && !map[m[1]]) map[m[1]] = refs.join(' · ');
    }
  }

  return map;
}

function read(logFile) {
  const text = fs.readFileSync(logFile, 'utf8');
  const refs = refsFromSource();
  const rows = [];
  let section = '(준비·게이트)';

  for (const raw of text.split(/\r?\n/)) {
    const line = raw.trim();

    const s = SECTION_RE.exec(line);
    if (s) {
      section = s[1].replace(/\.sql$/, '');
      continue;
    }

    if (SECTION_END_RE.test(line)) {
      section = '(마무리 게이트)';
      continue;
    }

    const m = RESULT_RE.exec(line);
    if (!m) continue;

    /*
     * [X] **RS 계약 대조에는 시작 표기가 없다.** 하니스가 clean-rebuild 구획 안에서
     *     이어 돌려서, 그대로 두면 `01_공통업무상태` 가 `14_Clean_Rebuild_Verify` 의
     *     시험으로 세어진다. 이름 모양(`NN_이름`)으로 갈라 세운다.
     */
    const where = /^[0-9]{2}[a-z]?_/.test(m[2]) ? '(RS 계약 대조)' : section;

    rows.push({
      section: where,
      id: m[2],
      what: m[3].trim(),
      refs: refs[m[2]] || '',
      result: m[1],
    });
  }

  return rows;
}

/* ------------------------------------------------------------------ *
 * 엑셀
 * ------------------------------------------------------------------ */

const HEAD = { bold: true, color: { argb: 'FFFFFFFF' } };
const HEAD_FILL = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF2F6FB5' } };

function head(ws, widths, titles) {
  ws.columns = widths.map(function (w) { return { width: w }; });
  const row = ws.addRow(titles);
  row.font = HEAD;
  row.eachCell(function (cell) {
    cell.fill = HEAD_FILL;
    cell.alignment = { vertical: 'middle', horizontal: 'center' };
  });
  row.height = 22;
  ws.views = [{ state: 'frozen', ySplit: 1 }];
}

function color(result) {
  if (result === 'PASS') return 'FF2E9E4F';
  if (result === 'FAIL') return 'FFD64545';
  return 'FFE8A33D';
}

function write(rows, logFile) {
  const { mod } = require(path.resolve(ROOT, '..', 'tools', 'docgen', 'paths.js'));
  const ExcelJS = mod('exceljs');
  const wb = new ExcelJS.Workbook();

  /* ── 시트 1 요약 ─────────────────────────────────────────── */
  const s1 = wb.addWorksheet('요약');
  const runAt = fs.statSync(logFile).mtime;
  const stamp = runAt.getFullYear() + '-'
    + String(runAt.getMonth() + 1).padStart(2, '0') + '-'
    + String(runAt.getDate()).padStart(2, '0') + ' '
    + String(runAt.getHours()).padStart(2, '0') + ':'
    + String(runAt.getMinutes()).padStart(2, '0');

  s1.addRow(['DB 계약시험 목록']).font = { bold: true, size: 14 };
  s1.addRow(['실행', stamp + '  ·  ' + path.basename(logFile)]);
  s1.addRow([]);
  head(s1, [42, 10, 10, 10, 10], ['구획', '건수', 'PASS', 'NOT RUN', 'SKIP']);

  const bySection = {};
  const order = [];
  for (const r of rows) {
    if (!bySection[r.section]) { bySection[r.section] = []; order.push(r.section); }
    bySection[r.section].push(r);
  }

  for (const name of order) {
    const list = bySection[name];
    s1.addRow([
      name,
      list.length,
      list.filter(function (r) { return r.result === 'PASS'; }).length,
      list.filter(function (r) { return r.result === 'NOT RUN'; }).length,
      list.filter(function (r) { return r.result === 'SKIP'; }).length,
    ]);
  }

  const total = s1.addRow([
    '합계',
    rows.length,
    rows.filter(function (r) { return r.result === 'PASS'; }).length,
    rows.filter(function (r) { return r.result === 'NOT RUN'; }).length,
    rows.filter(function (r) { return r.result === 'SKIP'; }).length,
  ]);
  total.font = { bold: true };

  /* ── 시트 2 목록 ─────────────────────────────────────────── */
  const s2 = wb.addWorksheet('시험 목록');
  head(s2, [34, 18, 88, 22, 10], ['구획', '시험 ID', '무엇을 재는가', '근거', '결과']);

  for (const r of rows) {
    const line = s2.addRow([r.section, r.id, r.what, r.refs, r.result]);
    line.alignment = { vertical: 'top', wrapText: true };
    line.getCell(5).font = { bold: true, color: { argb: color(r.result) } };
  }

  fs.mkdirSync(path.dirname(OUT), { recursive: true });
  return wb.xlsx.writeFile(OUT).then(function () {
    console.log('생성 ' + OUT.replace(/\\/g, '/'));
    console.log('구획 ' + order.length + ' · 시험 ' + rows.length + '건');
  });
}

function main() {
  const logFile = newestLog();
  if (!logFile) {
    fail('database/artifacts/logs/ 에 full_test_run*.log 이 없다 — database/scripts/test.sh 를 먼저 돌린다');
    return Promise.resolve();
  }

  if (!DONE_RE.test(fs.readFileSync(logFile, 'utf8'))) {
    fail('로그가 하니스의 끝맺음 줄로 끝나지 않는다 — 아직 돌고 있다: ' + path.basename(logFile));
    return Promise.resolve();
  }

  const rows = read(logFile);
  if (!rows.length) {
    fail('로그에서 시험 결과 줄을 하나도 못 읽었다 — 표기가 바뀌었다: ' + logFile);
    return Promise.resolve();
  }

  const bad = rows.filter(function (r) { return r.result === 'FAIL'; });
  if (bad.length) {
    fail('로그에 FAIL 이 ' + bad.length + '건 있다 — 실패를 담은 목록을 산출물로 내보내지 않는다');
    for (const r of bad.slice(0, 10)) console.log('       ' + r.id + ' ' + r.what);
    return Promise.resolve();
  }

  const notRun = rows.filter(function (r) { return r.result === 'NOT RUN'; }).length;
  if (notRun) {
    console.log('!! NOT RUN ' + notRun + '건 — 요약을 완료로 읽지 않는다. 엑셀에 그대로 싣는다');
  }

  console.log('PASS DBI-001 FAIL 0건');
  console.log('PASS DBI-002 시험 결과 ' + rows.length + '건을 읽었다 (' + path.basename(logFile) + ')');
  return Promise.resolve(write(rows, logFile));
}

main().then(function () {
  console.log('');
  console.log(failed ? '=== DB 시험 목록 FAIL ===' : '=== DB 시험 목록 PASS ===');
  process.exit(failed);
});
