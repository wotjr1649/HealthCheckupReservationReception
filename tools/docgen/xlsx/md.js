/*
 * md.js — 기준선 마크다운을 읽어 표를 뽑고, 공통 서식으로 xlsx 를 그린다.
 *
 * build_00.js / build_02.js 와 달리 04·05 생성기는 **내용을 하드코딩하지 않는다.**
 * 기준선을 고쳤는데 산출물이 옛 값을 들고 있는 상태가 이 프로젝트에서 실제로 있었다
 * (database/CLAUDE.md §3). 원본을 파싱하면 그 어긋남이 원리적으로 생기지 않는다.
 *
 * docs/baseline/ 은 읽기만 한다.
 */
'use strict';
const fs = require('fs');
const path = require('path');

const ExcelJS = require('D:/tmp/hcwork/gen/node_modules/exceljs');
const ROOT = 'D:/AIDEV/HealthCheckupReservationReception';
const BASE = path.join(ROOT, 'docs', 'baseline');
const OUTDIR = path.join(BASE, 'output');

const read = f => fs.readFileSync(path.join(BASE, f), 'utf8');

// 셀에서 마크다운 장식을 걷어낸다. 값은 그대로 두고 표기만 없앤다.
const cell = s => String(s == null ? '' : s)
  .replace(/`/g, '').replace(/\*\*/g, '').replace(/<br\s*\/?>/gi, ' ').trim();

/* 문서를 제목 단위로 자른다. 키는 제목 줄 전체(장식 제거)다. */
function sections(src) {
  const out = [];
  const lines = src.split('\n');
  let cur = { title: '(머리말)', level: 0, body: [] };
  let inFence = false;
  for (const l of lines) {
    if (/^\s*```/.test(l)) inFence = !inFence;
    const m = !inFence && l.match(/^(#{1,6})\s+(.*)$/);
    if (m) { out.push(cur); cur = { title: cell(m[2]), level: m[1].length, body: [] }; }
    else cur.body.push(l);
  }
  out.push(cur);
  for (const s of out) s.text = s.body.join('\n');
  return out;
}

/* 한 구획 안의 마크다운 표를 전부 뽑는다. 코드펜스 안은 표가 아니다. */
function tables(text) {
  const out = [];
  const lines = text.split('\n');
  let inFence = false;
  for (let i = 0; i < lines.length; i++) {
    if (/^\s*```/.test(lines[i])) { inFence = !inFence; continue; }
    if (inFence) continue;
    if (!/^\s*\|/.test(lines[i]) || !/^\s*\|[\s:\-|]+\|\s*$/.test(lines[i + 1] || '')) continue;
    const head = lines[i].trim().replace(/^\||\|$/g, '').split('|').map(cell);
    const rows = [];
    let j = i + 2;
    for (; j < lines.length && /^\s*\|/.test(lines[j]); j++)
      rows.push(lines[j].trim().replace(/^\||\|$/g, '').split('|').map(cell));
    out.push({ head, rows });
    i = j - 1;
  }
  return out;
}

/* 코드펜스 본문만 뽑는다. lang 을 주면 그 언어만. */
function fences(text, lang) {
  const out = [];
  const lines = text.split('\n');
  let on = false, cur = [], l0 = '';
  for (const l of lines) {
    const m = l.match(/^\s*```(\w*)/);
    if (m) {
      if (!on) { on = true; cur = []; l0 = m[1]; }
      else { on = false; if (!lang || l0.toLowerCase() === lang) out.push(cur.join('\n')); }
      continue;
    }
    if (on) cur.push(l);
  }
  return out;
}

/* ------------------------------------------------------------------ *
 * 서식 — build_00.js 와 같은 모양으로 맞춘다.
 * ------------------------------------------------------------------ */
const NAVY = 'FF1F3864';
const HEAD_FILL = { type: 'pattern', pattern: 'solid', fgColor: { argb: NAVY } };
const ZEBRA = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF2F5FA' } };
const THIN = { style: 'thin', color: { argb: 'FFB7C0D0' } };
const BORDER = { top: THIN, left: THIN, bottom: THIN, right: THIN };

/* sheets: [{ name, title, head:[], w:[], rows:[[]], group?:0 }] — group 열이 바뀔 때 얼룩을 뒤집는다 */
function workbook(creator, sheets) {
  const wb = new ExcelJS.Workbook();
  wb.creator = creator;
  wb.created = new Date();

  for (const s of sheets) {
    const ws = wb.addWorksheet(s.name, { views: [{ state: 'frozen', xSplit: 0, ySplit: 2 }] });
    ws.columns = s.w.map(width => ({ width }));

    const t = ws.getRow(1);
    t.getCell(1).value = s.title;
    t.getCell(1).font = { bold: true, size: 14, color: { argb: NAVY } };
    t.height = 26;

    const h = ws.getRow(2);
    s.head.forEach((v, i) => {
      const c = h.getCell(i + 1);
      c.value = v;
      c.font = { bold: true, size: 11, color: { argb: 'FFFFFFFF' } };
      c.fill = HEAD_FILL;
      c.alignment = { vertical: 'middle', horizontal: 'center', wrapText: true };
      c.border = BORDER;
    });
    h.height = 24;

    let r = 3, prev = null, zebra = false;
    for (const row of s.rows) {
      const g = s.group == null ? null : row[s.group];
      if (g !== prev) { if (prev !== null) zebra = !zebra; prev = g; }
      const xl = ws.getRow(r);
      row.forEach((v, i) => {
        const c = xl.getCell(i + 1);
        c.value = v === '' ? null : v;
        c.font = { size: 10 };
        c.alignment = { vertical: 'top', wrapText: true,
                        horizontal: (s.ctr || []).includes(i) ? 'center' : 'left' };
        c.border = BORDER;
        if (zebra) c.fill = ZEBRA;
      });
      r++;
    }
    ws.autoFilter = { from: { row: 2, column: 1 }, to: { row: 2, column: s.head.length } };
  }
  return wb;
}

/* 생성 뒤 같은 프로세스에서 다시 열어 본다. 쓰기만 하고 끝내지 않는다. */
async function emit(wb, outName, checks) {
  fs.mkdirSync(OUTDIR, { recursive: true });
  const out = path.join(OUTDIR, outName);
  await wb.xlsx.writeFile(out);

  const re = new ExcelJS.Workbook();
  await re.xlsx.readFile(out);
  console.log('생성 완료: ' + out);
  console.log('시트 ' + re.worksheets.length + '개');
  for (const ws of re.worksheets) {
    let n = 0;
    ws.eachRow({ includeEmpty: false }, () => n++);
    console.log('  - ' + ws.name.padEnd(16) + ' 행 ' + n + ' (데이터 ' + (n - 2) + ')');
  }

  let fail = 0;
  for (const [label, got, want] of checks) {
    const ok = got === want;
    if (!ok) fail++;
    console.log((ok ? 'PASS ' : 'FAIL ') + label + ' = ' + got + (ok ? '' : ' (기대 ' + want + ')'));
  }
  console.log('=== ' + outName + ': FAIL ' + fail + ' ===');
  if (fail) process.exit(1);
}

module.exports = { read, cell, sections, tables, fences, workbook, emit, OUTDIR };
