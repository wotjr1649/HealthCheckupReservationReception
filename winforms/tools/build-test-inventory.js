#!/usr/bin/env node
/*
 * 단위시험 목록 엑셀 생성기 — `docs/phase5/output/P5_단위시험_목록.xlsx`
 *
 * 손으로 쓰는 칸이 하나도 없다. 세 곳에서 읽어 합친다.
 *
 *   tests/**\/*.cs   [TestMethod] 바로 위 주석  -> 이유
 *                    그 주석 안의 `05 §9.12`    -> 근거
 *                    클래스 주석의 `WF-RSV-01`  -> 화면/영역
 *   TestResults/*.trx (최신)                    -> 결과 · 케이스 수 · ms
 *
 * `[!]` **이유의 단일 출처는 코드 주석이다.** 엑셀에 이유를 따로 적으면 그 순간 값이
 *       두 곳이 되고, 둘이 되면 한쪽만 고쳐지는 날이 온다 (ROOT AGENTS.md §6).
 *       고칠 일이 생기면 주석을 고쳐 다시 돌린다 (ROOT AGENTS.md §3 과 같은 규칙).
 *
 * `[!]` **공개본 6종에 합류하지 않는다.** `tools/docgen/verify_output.js` 가
 *       `PASS`·`FAIL`·`실측`·`게이트`·`회귀` 를 금지 어휘로 잡는다 — 시험 결과 엑셀은
 *       결과 칸의 단어 자체가 걸려 구조적으로 들어갈 수 없다. 그래서 층을 나눠
 *       `docs/phase5/output/` 에 둔다.
 *
 * 모드
 *   node tools/build-test-inventory.js            판정 + 엑셀 생성
 *   node tools/build-test-inventory.js --check    판정만 (엑셀을 쓰지 않는다)
 *   node tools/build-test-inventory.js selftest   게이트가 정말 red 를 내는가
 *
 * `[!]` **`--check` 는 stdlib 만으로 돈다.** `scripts/test.sh` 가 부르는 자리이고,
 *       그 회귀는 DB 도 MSBuild 도 없는 곳에서 같은 판정을 내야 한다. exceljs 는
 *       엑셀을 실제로 쓸 때만 부른다.
 */
'use strict';

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..');
const TESTS = path.join(ROOT, 'tests', 'HealthCheckupReservationReception.Tests');
const TRXDIR = path.join(ROOT, 'TestResults');
const OUTDIR = path.resolve(ROOT, '..', 'docs', 'phase5', 'output');
const OUT = path.join(OUTDIR, 'P5_단위시험_목록.xlsx');

/*
 * 폴더가 곧 층이다 (킷의 「Folders equal namespaces」와 같은 줄).
 * 빈 문자열은 저장소 루트 — 지금 거기 있는 것은 `MainFormTests.cs` 하나이고 MainForm
 * 시험이므로 View 다. 모르는 폴더가 생기면 조용히 넘기지 않고 FAIL 한다.
 */
const LAYER = {
  '': 'View',
  'Common': '공통',
  'Services': 'Service',
  'Presenters': 'Presenter',
  'Views': 'View',
  'Visual': '시각 증거',
  'Integration': '실물 DB',
};

/* 층을 엑셀 요약 시트에 내보내는 순서. 읽는 사람이 바깥에서 안쪽으로 따라간다. */
const LAYER_ORDER = ['공통', 'Service', 'Presenter', 'View', '시각 증거', '실물 DB'];

/* 화면 ID — 03 §2 의 표기. `WF-00` 처럼 가운데가 없는 것도 있다. */
const SCREEN_RE = /\b(?:WF|DLG|CNF)-(?:[A-Z]{3}-)?\d{2}\b/;

/* 근거 참조 — `05 §9.12` · `00 §7.2.2` · `04 §14`. 기준선 문서는 00~07 뿐이다. */
const REF_RE = /\b(0[0-7])\s*§\s*(\d+(?:\.\d+)*[a-z]?)/g;

/* ------------------------------------------------------------------ *
 * C# 읽기
 * ------------------------------------------------------------------ */

function walk(dir, out) {
  for (const name of fs.readdirSync(dir)) {
    const p = path.join(dir, name);
    const st = fs.statSync(p);
    if (st.isDirectory()) {
      if (name === 'obj' || name === 'bin') continue;
      walk(p, out);
    } else if (name.endsWith('.cs')) {
      out.push(p);
    }
  }
  return out;
}

/*
 * 주석 한 덩어리를 사람이 읽는 한 줄로 만든다.
 *
 * `///` 와 `//` 를 가리지 않는다 — 108건이 이미 `//` 로 적혀 있고, 그것을 XML 주석으로
 * 옮기는 일은 같은 문장을 다시 쓰는 것뿐이다. 읽는 쪽이 맞추면 된다.
 */
function cleanComment(lines) {
  const text = lines
    .map(function (l) { return l.trim().replace(/^\/\/\/?/, '').trim(); })
    .join(' ')
    .replace(/<\/?summary>/g, ' ')
    .replace(/<see\s+cref="[^"]*?([A-Za-z0-9_]+)"\s*\/>/g, '$1')
    .replace(/<\/?(para|remarks|c|code)>/g, ' ')
    .replace(/`/g, '')
    .replace(/\*\*/g, '')
    /*
     * 저장소 라벨(`[!]`·`[X]`·`[I]`·`[B]`·`[D]`)을 걷는다. 읽는 쪽은 이 저장소의
     * 라벨 규약을 모르고, 뜻 없는 기호는 문장을 흐린다. 날짜·회차 표기는 남긴다.
     */
    .replace(/\[(?:!|X|I|B|D)(?:\s+[^\]]*)?\]/g, ' ')
    .replace(/^[─\s]*/, '')
    .replace(/\s+/g, ' ')
    .trim();
  return text;
}

/*
 * 주석 한 덩어리를 **대상 · 목적 · 확인** 셋으로 가른다.
 *
 * `[!]` **셋은 서로 다른 질문이다** (2026-09-15 사용자 지시).
 *       대상 — 무엇을 테스트하는가. 시험 이름만으로는 이것이 안 보인다.
 *       목적 — 왜 이 시험이 있는가. 없으면 무엇이 깨지는가.
 *       확인 — 이 시험이 참이라고 주장하는 것이 무엇인가. 기대 결과다.
 *       한 칸에 뭉쳐 두면 「동작을 다시 적은 문장」이 되고, 읽는 사람은 시험 이름에서
 *       얻지 못한 것을 여기서도 얻지 못한다.
 *
 * 표기는 시험 위 주석의 `대상:` · `목적:` · `확인:` 세 머리말이다. 빠진 것이 있으면
 * `TI-001` 이 잡는다.
 */
function splitPurpose(text) {
  const pick = function (head, nexts) {
    const stop = nexts.length ? '(?:' + nexts.join('|') + ')\\s*:' : '$';
    const re = new RegExp(head + '\\s*:\\s*([\\s\\S]*?)\\s*(?:' + stop + '|$)');
    const m = re.exec(text);
    return m ? (m[1] || '').trim() : '';
  };
  return {
    subject: pick('대상', ['목적', '확인']),
    purpose: pick('목적', ['확인']),
    verify: pick('확인', []),
  };
}

function refsOf(text) {
  const out = [];
  let m;
  REF_RE.lastIndex = 0;
  while ((m = REF_RE.exec(text)) !== null) {
    const ref = m[1] + ' §' + m[2];
    if (out.indexOf(ref) === -1) out.push(ref);
  }
  return out.join(' · ');
}

/* `rel` 은 selftest 가 임시 디렉터리 파일을 쓰기 위한 자리다 — 층은 경로에서 나온다. */
function parseFile(file, relPath) {
  const src = fs.readFileSync(file, 'utf8').replace(/^﻿/, '');
  const lines = src.split(/\r?\n/);

  const rel = relPath || path.relative(TESTS, file).replace(/\\/g, '/');
  const folder = rel.indexOf('/') === -1 ? '' : rel.slice(0, rel.indexOf('/'));
  if (!Object.prototype.hasOwnProperty.call(LAYER, folder)) {
    return { error: rel + ' : 모르는 폴더 「' + folder + '」 — LAYER 에 층을 정하고 다시 돌린다' };
  }

  /* 위로 올라가며 붙어 있는 주석 덩어리를 모은다. 속성줄([DataRow] 등)은 건너뛴다. */
  function commentAbove(i) {
    let j = i - 1;
    while (j >= 0 && /^\s*\[/.test(lines[j])) j--;
    const block = [];
    while (j >= 0 && /^\s*\/\//.test(lines[j])) { block.unshift(lines[j]); j--; }
    return block;
  }

  /* 시그니처 줄 `sig` 다음의 여는 중괄호 바로 아래에 붙어 있는 주석 덩어리. */
  function commentInBody(sig) {
    let j = sig + 1;
    if (/\{\s*$/.test(lines[sig])) j = sig + 1;
    else if (/^\s*\{\s*$/.test(lines[j] || '')) j = j + 1;
    else return [];
    const block = [];
    while (j < lines.length && /^\s*\/\//.test(lines[j])) { block.push(lines[j]); j++; }
    return block;
  }

  let className = null;
  let classDoc = '';
  const methods = [];

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    const cls = /^\s*(?:public|internal)\s+(?:sealed\s+)?(?:partial\s+)?class\s+([A-Za-z0-9_]+)/.exec(line);
    if (cls && className === null && /\[TestClass\]/.test(lines.slice(Math.max(0, i - 4), i).join('\n'))) {
      className = cls[1];
      classDoc = cleanComment(commentAbove(i));
      continue;
    }

    if (!/^\s*\[(?:Data)?TestMethod\]/.test(line)) continue;

    /* 이름은 속성줄을 지난 다음의 시그니처에서 나온다. */
    let k = i + 1;
    while (k < lines.length && /^\s*\[/.test(lines[k])) k++;
    const sig = /\b(?:void|Task)\s+([A-Za-z0-9_가-힣]+)\s*\(/.exec(lines[k] || '');
    if (!sig) {
      return {
        error: rel + ':' + (k + 1) + ' : [TestMethod] 다음 줄에서 시험 이름을 읽지 못했다' +
          ' — 이유 주석은 속성 아래가 아니라 [TestMethod] 위에 둔다 (C# 이 그 자리를 문서 주석으로 읽는다)',
      };
    }

    /*
     * 이유는 [TestMethod] 위가 기본이고, 없으면 **본문 첫 줄**을 본다. 이 저장소에는
     * 짧은 한 줄을 여는 중괄호 바로 아래에 적어 둔 시험이 있고 그것도 이유다 —
     * 같은 문장을 위로 옮겨 적으면 값이 두 곳이 된다 (ROOT AGENTS.md §6).
     */
    let reason = cleanComment(commentAbove(i));
    if (!reason) reason = cleanComment(commentInBody(k));
    const split = splitPurpose(reason);
    methods.push({
      file: rel,
      line: i + 1,
      layer: LAYER[folder],
      className: className,
      name: sig[1],
      /* `_` 는 C# 식별자에서 띄어쓰기 자리다. 읽는 사람에게는 문장으로 보여 준다. */
      title: sig[1].replace(/_/g, ' '),
      reason: reason,
      subject: split.subject,
      purpose: split.purpose,
      verify: split.verify,
      refs: refsOf(reason),
    });
  }

  if (className === null) return { methods: [] };

  const screen = SCREEN_RE.exec(classDoc);
  const area = screen ? screen[0] : className.replace(/Tests$/, '');
  for (const m of methods) m.area = area;

  return { methods: methods };
}

/* ------------------------------------------------------------------ *
 * trx 읽기
 * ------------------------------------------------------------------ */

function latestTrx() {
  if (!fs.existsSync(TRXDIR)) return null;
  const files = fs.readdirSync(TRXDIR)
    .filter(function (f) { return f.endsWith('.trx'); })
    .map(function (f) { return path.join(TRXDIR, f); });
  if (!files.length) return null;
  files.sort(function (a, b) { return fs.statSync(b).mtimeMs - fs.statSync(a).mtimeMs; });
  return files[0];
}

function attr(tag, name) {
  const m = new RegExp(name + '="([^"]*)"').exec(tag);
  return m ? m[1] : null;
}

/* `메서드명(인자,인자)` -> `메서드명`. DataRow 는 메서드 한 행으로 접는다. */
function baseName(n) {
  const i = n.indexOf('(');
  return (i === -1 ? n : n.slice(0, i)).trim();
}

function readTrx(file) {
  const xml = fs.readFileSync(file, 'utf8');

  /* testId -> 'Namespace.Class.Method' */
  const byId = {};
  const unitRe = /<UnitTest\b[^>]*>([\s\S]*?)<\/UnitTest>/g;
  let u;
  while ((u = unitRe.exec(xml)) !== null) {
    const id = attr(u[0].slice(0, u[0].indexOf('>') + 1), 'id');
    const tm = /<TestMethod\b[^>]*\/>/.exec(u[1]);
    if (!id || !tm) continue;
    const cls = attr(tm[0], 'className');
    const nm = attr(tm[0], 'name');
    if (!cls || !nm) continue;
    byId[id] = cls.split('.').pop() + '.' + baseName(nm);
  }

  const map = {};
  const resRe = /<UnitTestResult\b[^>]*>/g;
  let r;
  while ((r = resRe.exec(xml)) !== null) {
    const key = byId[attr(r[0], 'testId')];
    if (!key) continue;
    const outcome = attr(r[0], 'outcome');
    const dur = attr(r[0], 'duration') || '00:00:00';
    const parts = dur.split(':');
    const ms = Math.round((Number(parts[0]) * 3600 + Number(parts[1]) * 60 + Number(parts[2])) * 1000);

    if (!map[key]) map[key] = { cases: 0, pass: 0, fail: 0, other: 0, ms: 0 };
    const e = map[key];
    e.cases++;
    e.ms += ms;
    if (outcome === 'Passed') e.pass++;
    else if (outcome === 'Failed') e.fail++;
    else e.other++;
  }
  return map;
}

/* ------------------------------------------------------------------ *
 * 판정
 * ------------------------------------------------------------------ */

function judge(methods, trxFile, trxMap, files, quiet, checkOnly) {
  const problems = [];
  const ok = function (line) { if (!quiet) console.log(line); };

  /*
   * TI-001 — `목적:` 과 `확인:` 이 **둘 다** 있어야 목록에 실린다.
   *
   * 이름만으로는 무엇을 재는지 알 수 없고, 목적만 있으면 「왜 하는가」는 알아도
   * 「무엇이 참이어야 하는가」를 모른다. 빈칸은 읽는 사람에게 「이 시험은 목적이 없다」로
   * 읽히므로, 빈칸을 내보내지 않고 여기서 막는다 (2026-09-15 사용자 지시).
   */
  const bare = methods.filter(function (m) { return !m.subject || !m.purpose || !m.verify; });
  if (bare.length) {
    problems.push('FAIL TI-001 대상·목적·확인이 갖춰지지 않은 시험 ' + bare.length + '건');
    for (const m of bare.slice(0, 300)) {
      const miss = ['대상', '목적', '확인']
        .filter(function (t, n) { return !([m.subject, m.purpose, m.verify][n]); })
        .join('·') + ' 없음';
      problems.push('       ' + m.file + ':' + m.line + '  [' + miss + ']  ' + m.name);
    }
    if (bare.length > 300) problems.push('       … 그 밖 ' + (bare.length - 300) + '건');
  } else {
    ok('PASS TI-001 대상·목적·확인 전건 있음 (' + methods.length + '건)');
  }

  if (!trxFile) {
    ok(checkOnly
      ? 'SKIP TI-002 · TI-003 — 결과 파일은 엑셀을 찍을 때 판정한다 (기계마다 있고 없다)'
      : 'SKIP TI-002 · TI-003 — TestResults/*.trx 가 없다. 결과 칸을 채우려면 먼저 시험을 돌린다');
    return { problems: problems, trxUsable: false };
  }

  /*
   * TI-002 — trx 에 있는데 코드에 없는 시험은 **옛 trx 로 찍고 있다**는 뜻이다.
   * 반대 방향(코드에 있는데 trx 에 없다)은 「미실행」이 정상이다 — Integration 이
   * 운영시간 밖에서 그렇게 된다.
   */
  const known = {};
  for (const m of methods) known[m.className + '.' + m.name] = true;
  const ghosts = Object.keys(trxMap).filter(function (k) { return !known[k]; });
  if (ghosts.length) {
    problems.push('FAIL TI-002 trx 에만 있고 코드에 없는 시험 ' + ghosts.length + '건 — trx 가 뒤처졌다');
    for (const g of ghosts.slice(0, 20)) problems.push('       ' + g);
  } else {
    ok('PASS TI-002 trx ↔ 코드 시험 목록 (trx ' + Object.keys(trxMap).length + '건)');
  }

  /*
   * TI-003 — 시험 소스가 trx 보다 나중이면 그 결과는 지금 코드의 것이 아니다.
   * 사용자가 「테스트가 최신화 되어있는지 확인한다」고 한 자리가 여기다.
   */
  const trxTime = fs.statSync(trxFile).mtimeMs;
  const newer = files.filter(function (f) { return fs.statSync(f).mtimeMs > trxTime; });
  if (newer.length) {
    problems.push('FAIL TI-003 trx 보다 나중에 고친 시험 소스 ' + newer.length + '건 — 다시 돌린 뒤 찍는다');
    for (const f of newer.slice(0, 10)) {
      problems.push('       ' + path.relative(TESTS, f).replace(/\\/g, '/'));
    }
  } else {
    ok('PASS TI-003 trx 가 시험 소스보다 나중이다 (' + path.basename(trxFile) + ')');
  }

  return { problems: problems, trxUsable: true };
}

/* ------------------------------------------------------------------ *
 * 엑셀
 * ------------------------------------------------------------------ */

function resultOf(e) {
  if (!e) return '미실행';
  if (e.fail) return 'FAIL';
  if (e.other) return '미판정';
  return 'PASS';
}

function write(methods, trxFile, trxMap) {
  const { mod } = require(path.resolve(ROOT, '..', 'tools', 'docgen', 'paths.js'));
  const ExcelJS = mod('exceljs');
  const wb = new ExcelJS.Workbook();

  const rows = methods.map(function (m) {
    const e = trxMap[m.className + '.' + m.name];
    return {
      layer: m.layer,
      area: m.area,
      name: m.name,
      title: m.title,
      subject: m.subject,
      purpose: m.purpose,
      verify: m.verify,
      refs: m.refs,
      cases: e ? e.cases : 0,
      result: resultOf(e),
      ms: e ? e.ms : null,
      file: m.file,
    };
  });

  rows.sort(function (a, b) {
    const la = LAYER_ORDER.indexOf(a.layer), lb = LAYER_ORDER.indexOf(b.layer);
    if (la !== lb) return la - lb;
    if (a.area !== b.area) return a.area < b.area ? -1 : 1;
    return a.name < b.name ? -1 : 1;
  });

  /* ── 시트 1 요약 ─────────────────────────────────────────── */
  const s1 = wb.addWorksheet('요약');
  const runAt = fs.statSync(trxFile).mtime;
  const stamp = runAt.getFullYear() + '-' +
    String(runAt.getMonth() + 1).padStart(2, '0') + '-' +
    String(runAt.getDate()).padStart(2, '0') + ' ' +
    String(runAt.getHours()).padStart(2, '0') + ':' +
    String(runAt.getMinutes()).padStart(2, '0');

  s1.addRow(['검진 예약·접수 관리 프로그램 — 단위시험 목록']);
  s1.addRow([]);
  s1.addRow(['실행시각', stamp]);
  s1.addRow(['결과파일', path.basename(trxFile)]);
  s1.addRow(['대상', 'HealthCheckupReservationReception.Tests']);
  s1.addRow([]);
  s1.addRow(['층', '시험', '실행 케이스', 'PASS', 'FAIL', '미실행 시험']);

  const byLayer = {};
  for (const r of rows) {
    if (!byLayer[r.layer]) byLayer[r.layer] = { n: 0, cases: 0, pass: 0, fail: 0, none: 0 };
    const e = byLayer[r.layer];
    e.n++;
    e.cases += r.cases;
    if (r.result === 'PASS') e.pass += r.cases;
    else if (r.result === 'FAIL') e.fail += r.cases;
    else e.none++;
  }
  let tot = { n: 0, cases: 0, pass: 0, fail: 0, none: 0 };
  for (const layer of LAYER_ORDER) {
    const e = byLayer[layer];
    if (!e) continue;
    s1.addRow([layer, e.n, e.cases, e.pass, e.fail, e.none]);
    tot.n += e.n; tot.cases += e.cases; tot.pass += e.pass; tot.fail += e.fail; tot.none += e.none;
  }
  s1.addRow(['합계', tot.n, tot.cases, tot.pass, tot.fail, tot.none]);
  s1.addRow([]);
  s1.addRow(['PASS · FAIL', '케이스 수다. 「미실행 시험」만 시험 수이며, 돌지 않아 케이스를 셀 수 없다.']);
  s1.addRow(['미실행', '그 시각에 판정하지 못한 것이다. 판정하지 못한 검사를 PASS 로 세지 않는다.']);
  s1.addRow(['', '실물 DB 계열은 운영시간 밖이거나 마감 뒤에는 예약·접수가 막혀 판정이 나오지 않는다. 그 창의 값은 DB 의 [운영기준] 이 갖는다.']);
  s1.addRow(['케이스', 'DataRow 로 값을 바꿔 가며 도는 시험은 한 행에 접고 케이스 수를 적었다.']);

  s1.getRow(1).font = { bold: true, size: 14 };
  s1.getRow(7).font = { bold: true };
  s1.getRow(7 + Object.keys(byLayer).length + 1).font = { bold: true };
  s1.columns = [{ width: 14 }, { width: 12 }, { width: 10 }, { width: 10 }, { width: 10 }, { width: 10 }];

  /* ── 시트 2 전건 ─────────────────────────────────────────── */
  const s2 = wb.addWorksheet('전건');
  s2.addRow([
    '층', '화면/영역', '시험 항목',
    '대상 — 무엇을 테스트하는가',
    '목적 — 왜 이 시험이 있는가',
    '확인 내용 — 무엇이 참이어야 하는가',
    '근거', '케이스', '결과', 'ms', '파일',
  ]);
  for (const r of rows) {
    s2.addRow([r.layer, r.area, r.title, r.subject, r.purpose, r.verify,
      r.refs, r.cases, r.result, r.ms, r.file]);
  }
  s2.getRow(1).font = { bold: true };
  s2.getRow(1).alignment = { wrapText: true, vertical: 'middle' };
  s2.views = [{ state: 'frozen', ySplit: 1, xSplit: 3 }];
  s2.autoFilter = { from: 'A1', to: 'K1' };
  s2.columns = [
    { width: 11 }, { width: 15 }, { width: 40 }, { width: 44 },
    { width: 60 }, { width: 60 },
    { width: 17 }, { width: 8 }, { width: 8 }, { width: 8 }, { width: 42 },
  ];
  s2.eachRow(function (row, i) {
    if (i === 1) return;
    for (const c of [3, 4, 5, 6]) {
      row.getCell(c).alignment = { wrapText: true, vertical: 'top' };
    }
  });

  if (!fs.existsSync(OUTDIR)) fs.mkdirSync(OUTDIR, { recursive: true });
  return wb.xlsx.writeFile(OUT).then(function () {
    console.log('생성 ' + path.relative(path.resolve(ROOT, '..'), OUT).replace(/\\/g, '/') +
      ' — 시험 ' + rows.length + '행 · 케이스 ' + tot.cases + '건');
  });
}

/* ------------------------------------------------------------------ *
 * selftest — 게이트가 정말 red 를 내는가
 * ------------------------------------------------------------------ */

function selftest() {
  let fail = 0;
  const say = function (ok, id, what) {
    console.log((ok ? 'PASS ' : 'FAIL ') + id + ' ' + what);
    if (!ok) fail++;
  };

  const tmp = fs.mkdtempSync(path.join(require('os').tmpdir(), 'ti-'));
  const mk = function (body) {
    const f = path.join(tmp, 'X.cs');
    fs.writeFileSync(f, body, 'utf8');
    return f;
  };
  const parse = function (body) { return parseFile(mk(body), 'Presenters/X.cs'); };

  /* 목적과 확인을 두 칸으로 가른다 */
  let r = parse([
    '[TestClass]',
    'public class XTests',
    '{',
    '    // 대상: ReservationPresenter — 저장 버튼 활성화',
    '    // 목적: 05 §9.12 — 저장가능은 DB 것이다. 화면이 다시 세면 판정이 두 곳이 된다.',
    '    // 확인: 저장가능=0 이면 저장 버튼이 닫히고 차단메시지가 화면에 뜬다.',
    '    [TestMethod]',
    '    public void 무엇을_잰다() { }',
    '}',
  ].join('\n'));
  say(r.methods && r.methods.length === 1 &&
    /^ReservationPresenter — 저장 버튼 활성화$/.test(r.methods[0].subject) &&
    /^05 §9.12 — 저장가능은 DB 것이다.*두 곳이 된다\.$/.test(r.methods[0].purpose) &&
    /^저장가능=0 이면/.test(r.methods[0].verify),
    'TI-SELFTEST', '대상·목적·확인을 세 칸으로 가른다');
  say(r.methods && r.methods[0].title === '무엇을 잰다',
    'TI-SELFTEST', '시험 이름의 _ 를 띄어쓰기로 읽는다');
  say(r.methods && r.methods[0].refs === '05 §9.12', 'TI-SELFTEST', '주석에서 근거를 뽑는다');

  /* 확인만 빠져도 잡는다 — 목적만 있으면 무엇이 참이어야 하는지를 모른다 */
  r = parse([
    '[TestClass]',
    'public class XTests',
    '{',
    '    // 대상: 무엇인가',
    '    // 목적: 왜 하는지만 적었다.',
    '    [TestMethod]',
    '    public void 확인이_없다() { }',
    '}',
  ].join('\n'));
  say(judge(r.methods, null, {}, [], true).problems.some(function (p) { return /확인 없음/.test(p); }),
    'TI-SELFTEST', '목적만 있고 확인이 없으면 잡는다');

  /* 위가 비면 본문 첫 줄을 읽는다 */
  r = parse([
    '[TestClass]',
    'public class XTests',
    '{',
    '    [TestMethod]',
    '    public void 본문에_적었다()',
    '    {',
    '        // 대상: 무엇인가',
    '        // 목적: 지어내지 않는다',
    '        // 확인: 숫자만 남는다',
    '        Assert.AreEqual(1, 1);',
    '    }',
    '}',
  ].join('\n'));
  say(r.methods && r.methods.length === 1 && r.methods[0].verify === '숫자만 남는다',
    'TI-SELFTEST', '본문 첫 줄 주석도 읽는다');

  /* 저장소 라벨과 굵게 표기는 공개용 문장에서 걷는다 */
  r = parse([
    '[TestClass]',
    'public class XTests',
    '{',
    '    // 대상: 무엇인가',
    '    // 목적: [X] **막힌 이유**를 적는다',
    '    // 확인: 그대로다',
    '    [TestMethod]',
    '    public void 라벨을_건다() { }',
    '}',
  ].join('\n'));
  say(r.methods && r.methods[0].purpose === '막힌 이유를 적는다',
    'TI-SELFTEST', '[X] 와 ** 를 걷는다');

  /* 이유가 없으면 TI-001 이 잡는다 */
  r = parse([
    '[TestClass]',
    'public class XTests',
    '{',
    '    [TestMethod]',
    '    public void 이유가_없다() { }',
    '}',
  ].join('\n'));
  const j = judge(r.methods, null, {}, [], true);
  say(j.problems.some(function (p) { return /TI-001/.test(p); }),
    'TI-SELFTEST', '목적·확인이 둘 다 없는 시험을 잡는다');

  /* trx 에만 있는 시험은 TI-002 가 잡는다 */
  const trxFile = path.join(tmp, 'old.trx');
  fs.writeFileSync(trxFile, '', 'utf8');
  const j2 = judge(
    [{ className: 'XTests', name: '있다', subject: '대', purpose: '목', verify: '확', file: 'X.cs', line: 1 }],
    trxFile, { 'XTests.사라진시험': { cases: 1, pass: 1, fail: 0, other: 0, ms: 0 } }, [], true);
  say(j2.problems.some(function (p) { return /TI-002/.test(p); }),
    'TI-SELFTEST', 'trx 에만 있는 시험을 잡는다 (옛 trx)');

  /* 소스가 trx 보다 나중이면 TI-003 이 잡는다 */
  const newSrc = path.join(tmp, 'new.cs');
  fs.writeFileSync(newSrc, '// later', 'utf8');
  fs.utimesSync(newSrc, new Date(Date.now() + 60000), new Date(Date.now() + 60000));
  const j3 = judge(
    [{ className: 'XTests', name: '있다', subject: '대', purpose: '목', verify: '확', file: 'X.cs', line: 1 }],
    trxFile, {}, [newSrc], true);
  say(j3.problems.some(function (p) { return /TI-003/.test(p); }),
    'TI-SELFTEST', 'trx 보다 나중에 고친 소스를 잡는다');

  fs.rmdirSync(tmp, { recursive: true });
  console.log(fail ? '== FAIL selftest ==' : '== PASS selftest ==');
  process.exit(fail ? 1 : 0);
}

/* ------------------------------------------------------------------ *
 * main
 * ------------------------------------------------------------------ */

function main() {
  const mode = process.argv[2] || '';
  if (mode === 'selftest') return selftest();
  const checkOnly = mode === '--check';

  const files = walk(TESTS, []);
  const methods = [];
  for (const f of files) {
    const r = parseFile(f);
    if (r.error) {
      console.log('FAIL TI-000 ' + r.error);
      process.exit(1);
    }
    for (const m of r.methods) methods.push(m);
  }

  /*
   * `[!]` **`--check` 는 trx 를 보지 않는다.** `scripts/test.sh` 가 부르는 자리이고 그 회귀는
   *       *어디서나 같은 판정* 이 규칙인데, `TestResults/` 는 추적 밖이라 기계마다 있고 없다.
   *       TI-002·TI-003 은 소스만으로 답이 나오지 않으므로 **엑셀을 찍을 때** 판정한다 —
   *       옛 결과로 산출물을 만드는 것을 막는 것이 그 둘의 목적이고, 그 일은 여기서만 일어난다.
   */
  const trxFile = checkOnly ? null : latestTrx();
  const trxMap = trxFile ? readTrx(trxFile) : {};
  const j = judge(methods, trxFile, trxMap, files, false, checkOnly);

  if (j.problems.length) {
    for (const p of j.problems) console.log(p);
    console.log('== FAIL 단위시험 목록 ==');
    process.exit(1);
  }

  if (checkOnly) {
    console.log('== PASS 단위시험 목록 (판정만) ==');
    return;
  }

  if (!j.trxUsable) {
    console.log('FAIL 엑셀을 쓰려면 결과가 있어야 한다 — 시험을 먼저 돌린다');
    process.exit(1);
  }

  write(methods, trxFile, trxMap).catch(function (e) {
    console.log('FAIL 엑셀 생성 — ' + e.message);
    process.exit(1);
  });
}

/* 도구로 부르면 돌고, require 로 부르면 파서만 내준다 (미리보기·시험용). */
if (require.main === module) {
  main();
} else {
  module.exports = { parseFile: parseFile, splitPurpose: splitPurpose };
}
