#!/usr/bin/env node
/*
 * 통합시험 시나리오 엑셀 생성기 — `docs/phase5/output/P5_통합시험_시나리오.xlsx`
 *
 * 손으로 쓰는 칸이 하나도 없다. `docs/phase5/2026-09-14-Test-Scenarios.md` 하나를 읽는다.
 *
 *   §1  정상 업무 흐름   -> 시나리오 시트
 *   §2  예외 상황        -> 시나리오 시트
 *   §2.5 화면 증빙       -> 화면 증빙 시트 (그림을 실제로 싣는다)
 *
 * `[!]` **시나리오의 단일 출처는 그 문서다** (ROOT AGENTS.md §6). 엑셀에 시나리오를 따로
 *       적으면 그 순간 값이 두 곳이 되고, 둘이 되면 한쪽만 고쳐지는 날이 온다. 고칠 일이
 *       생기면 문서를 고쳐 다시 돌린다 — `build-test-inventory.js` 가 코드 주석에 대해
 *       세운 것과 같은 규칙이다.
 *
 * `[!]` **그림이 없으면 exit 1 이다.** 표가 가리키는 그림을 싣지 못한 채 제출본이 나가면,
 *       받는 사람은 설명만 있고 근거가 없는 문서를 본다. 그림은
 *       `build-scenario-evidence.js` 가 산출물로 옮겨 둔 것을 쓴다.
 *
 * `[X]` **단위시험 엑셀과 파일을 나눈다.** 저쪽은 코드 주석에서 뽑고 이쪽은 문서에서 뽑아
 *       읽는 곳이 다르다. 한 생성기가 두 출처를 들면 어느 쪽이 틀렸는지 판정이 흐려진다.
 *
 *   node tools/build-scenario-inventory.js
 */
'use strict';

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..');
const DOC = path.resolve(ROOT, '..', 'docs', 'phase5', '2026-09-14-Test-Scenarios.md');
const SHOTS = path.resolve(ROOT, '..', 'docs', 'phase5', 'output', 'screens');
const OUT = path.resolve(ROOT, '..', 'docs', 'phase5', 'output', 'P5_통합시험_시나리오.xlsx');

/* 실행 화면 크기. 시트에 실을 때 이 비율로 줄인다. */
const SHOT_WIDTH = 760;
const SHOT_RATIO = 887 / 1916;

let failed = 0;
function fail(line) {
  console.log('FAIL ' + line);
  failed = 1;
}

/** 마크다운 표 한 줄을 칸으로 가른다. 구분선(`|---|`)은 버린다. */
function rowsOf(section) {
  const out = [];
  for (const line of section.split('\n')) {
    const text = line.trim();
    if (!text.startsWith('|')) {
      continue;
    }

    const cells = text.slice(1, text.lastIndexOf('|')).split('|').map(function (c) { return clean(c); });
    if (cells.every(function (c) { return /^-*:?-*$/.test(c.replace(/\s/g, '')); })) {
      continue;
    }

    out.push(cells);
  }

  return out;
}

/** 표기만 걷는다 — 굵게·코드·강조는 엑셀에서 뜻이 없다. */
function clean(text) {
  return text.replace(/\*\*/g, '').replace(/`/g, '').replace(/\s+/g, ' ').trim();
}

/** `## <제목>` 부터 다음 `## ` 앞까지. */
function section(doc, heading) {
  const at = doc.indexOf('\n## ' + heading);
  if (at === -1) {
    return null;
  }

  const rest = doc.slice(at + 1);
  const next = rest.indexOf('\n## ');
  return next === -1 ? rest : rest.slice(0, next);
}

function read() {
  const doc = fs.readFileSync(DOC, 'utf8');
  const normal = section(doc, '1. ');
  const exception = section(doc, '2. ');
  const screens = section(doc, '2.5 ');
  if (!normal || !exception || !screens) {
    fail('시나리오 문서에서 절을 못 찾았다 (§1 · §2 · §2.5) — 제목이 바뀌었다: ' + DOC);
    return null;
  }

  // 머리줄을 빼고 본문만 쓴다.
  const rows = rowsOf(normal).slice(1).concat(rowsOf(exception).slice(1));
  const shots = rowsOf(screens).slice(1);
  if (!rows.length || !shots.length) {
    fail('표가 비었다 — §1/§2 ' + rows.length + '행 · §2.5 ' + shots.length + '행');
    return null;
  }

  return { rows: rows, shots: shots };
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

function write(data) {
  const { mod } = require(path.resolve(ROOT, '..', 'tools', 'docgen', 'paths.js'));
  const ExcelJS = mod('exceljs');
  const wb = new ExcelJS.Workbook();

  /* ── 시트 1 시나리오 ─────────────────────────────────────── */
  const s1 = wb.addWorksheet('시나리오');
  head(s1, [8, 46, 34, 10, 52, 30], ['ID', '시나리오', '기대', '결과', '증거 (시험 이름)', '화면']);

  for (const row of data.rows) {
    const id = row[0];
    const shots = data.shots
      .filter(function (s) { return s[0] === id; })
      .map(function (s) { return s[1]; })
      .join('\n');

    const line = s1.addRow([id, row[1], row[2], row[3], row[4], shots]);
    line.alignment = { vertical: 'top', wrapText: true };
    if (row[3] === 'PASS') {
      line.getCell(4).font = { bold: true, color: { argb: 'FF2E9E4F' } };
    }
  }

  /* ── 시트 2 화면 증빙 ────────────────────────────────────── */
  const s2 = wb.addWorksheet('화면 증빙');
  head(s2, [8, 34, 70], ['ID', '화면', '무엇이 보이는가']);

  const height = Math.round(SHOT_WIDTH * SHOT_RATIO);
  const rowsPerShot = Math.ceil(height / 18) + 2;

  for (const shot of data.shots) {
    const file = path.join(SHOTS, shot[1]);
    if (!fs.existsSync(file)) {
      fail('표가 가리키는 그림이 산출물에 없다: ' + shot[1] + ' — build-scenario-evidence.js 를 먼저 돌린다');
      continue;
    }

    const line = s2.addRow([shot[0], shot[1], shot[2]]);
    line.alignment = { vertical: 'top', wrapText: true };

    const id = wb.addImage({ buffer: fs.readFileSync(file), extension: 'png' });
    s2.addImage(id, {
      tl: { col: 0, row: s2.rowCount },
      ext: { width: SHOT_WIDTH, height: height },
    });

    // 그림이 앉을 자리를 비워 둔다. 비우지 않으면 다음 줄을 덮는다.
    for (let i = 0; i < rowsPerShot; i++) {
      s2.addRow([]);
    }
  }

  if (failed) {
    return;
  }

  fs.mkdirSync(path.dirname(OUT), { recursive: true });
  return wb.xlsx.writeFile(OUT).then(function () {
    console.log('생성 ' + OUT.replace(/\\/g, '/'));
    console.log('시나리오 ' + data.rows.length + '행 · 화면 ' + data.shots.length + '장');
  });
}

function main() {
  const data = read();
  if (!data) {
    return Promise.resolve();
  }

  return Promise.resolve(write(data));
}

main().then(function () {
  console.log('');
  console.log(failed ? '=== 통합시험 시나리오 FAIL ===' : '=== 통합시험 시나리오 PASS ===');
  process.exit(failed);
});
