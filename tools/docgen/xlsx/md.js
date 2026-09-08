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
 * 공개 정리 — 산출물 독자는 docs/baseline/*.md 를 갖고 있지 않다.
 * 기준선 본문에서 그대로 흘러드는 § 절 참조는 따라갈 수 없는 표시라 잡음이다.
 * 셀에 값을 쓰는 모든 경로가 이 함수를 지난다.
 *
 * [!] 문장 한가운데 홀로 남은 참조는 지우지 않는다 — '§8.2.3의 정렬 누수' 를 지우면
 *     '의 정렬 누수' 가 되어 말이 깨진다. 대신 남겨 두고 emit() 이 실패시킨다.
 *     사람이 그 문장을 다시 쓰게 만드는 것이 조용히 뭉개는 것보다 낫다.
 * ------------------------------------------------------------------ */
const REF = '§\s*\d+(?:\.\d+[a-z]?)*';
const LEFTOVER = [];
/*
 * 문장 한가운데 참조는 기계로 지울 수 없다. 뜻을 지키는 다시쓰기를 이름으로 적어 둔다.
 * 기준선을 고치지 않는다 — 기준선에서 § 는 정당한 상호참조이고, 공개본에서만 잡음이다.
 */
const PROSE = [
  ['§8.2.3의 정렬 누수', '예약접수 검사구성 제약과 같은 정렬 누수'],
];

function pub(v) {
  if (typeof v !== 'string' || v.indexOf('§') < 0) return v;
  for (const [from, to] of PROSE) v = v.split(from).join(to);
  let s = v.replace(new RegExp('\s*\((?:' + REF + ')(?:\s*[,·]\s*(?:' + REF + '))*\)', 'g'), '');
  s = s.replace(/\(([^()]*)\)/g, (m, inner) => {
    if (!new RegExp(REF).test(inner)) return m;
    const t = inner.replace(new RegExp(REF, 'g'), '')
      .replace(/^[\s,·]+|[\s,·]+$/g, '').replace(/[\s,·]*,[\s,·]*/g, ', ');
    return t ? '(' + t + ')' : '';
  });
  if (s.indexOf('§') >= 0) LEFTOVER.push(s.replace(/\s+/g, ' ').slice(0, 130));
  return s;
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
  wb.__sheets = sheets;          // emit() 이 열 너비 왕복을 대조하는 데 쓴다

  for (const s of sheets) {
    // draw 시트는 표가 아니라 그림이다. 눈금선을 끄고 그리기 함수에 맡긴다.
    if (s.draw) {
      const dws = wb.addWorksheet(s.name, { views: [{ showGridLines: false }] });
      dws.columns = s.w.map(width => ({ width }));
      if (s.rowHeight) dws.properties.defaultRowHeight = s.rowHeight;
      const t = dws.getCell(1, 1);
      t.value = s.title;
      t.font = { bold: true, size: 14, color: { argb: NAVY } };
      dws.getRow(1).height = 26;
      s.draw(dws);
      continue;
    }

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
        c.value = v === '' ? null : pub(v);
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
    console.log('  - ' + ws.name.padEnd(16) + ' 행 ' + n);
  }

  // [X] exceljs 4.4 는 너비가 **정확히 9** 인 열의 <col> 을 아예 쓰지 않는다 — 자기 기본값과
  //     같다고 보기 때문이다. 그런데 Excel 의 실제 기본은 8.43 이라 그림이 조용히 어긋난다.
  //     지정한 너비가 파일에 살아남았는지 왕복으로 대조한다. 9 를 쓰지 않는 것으로 피한다.
  let fail = 0;
  if (LEFTOVER.length) {
    fail++;
    console.log('FAIL 산출물에 따라갈 수 없는 절 참조가 남았다 ' + LEFTOVER.length + '건 — 문장을 다시 써라');
    [...new Set(LEFTOVER)].forEach(l => console.log('        ' + l));
  }
  for (const spec of (wb.__sheets || [])) {
    const ws = re.getWorksheet(spec.name);
    const got = ws.columns.map(c => c.width);
    const bad = spec.w.map((want, i) => (got[i] === want ? null : (i + 1) + '열 ' + want + '->'
                 + (got[i] === undefined ? '없음' : got[i]))).filter(Boolean);
    if (bad.length) { fail++; console.log('FAIL ' + spec.name + ' 열 너비가 파일에 안 남았다: ' + bad.join(' · ')); }
  }

  for (const [label, got, want] of checks) {
    const ok = got === want;
    if (!ok) fail++;
    console.log((ok ? 'PASS ' : 'FAIL ') + label + ' = ' + got + (ok ? '' : ' (기대 ' + want + ')'));
  }
  console.log('=== ' + outName + ': FAIL ' + fail + ' ===');
  if (fail) process.exit(1);
}

module.exports = { read, cell, pub, sections, tables, fences, workbook, emit, OUTDIR };

/* ================================================================== *
 * 그리기 킷 — ERD 용.
 *
 * exceljs 4.4 는 도형을 못 그리고 이 환경에는 래스터라이저가 없어 한글이 든 PNG 를
 * 만들 수 없다. 그래서 셀 자체로 그린다 — 테두리·채움·병합만 쓰므로 이미지와 달리
 * 선택·검색·인쇄가 되고 파일이 커지지 않는다.
 *
 * 선은 셀 테두리다. 가로선은 어떤 행의 bottom, 세로선은 어떤 열의 right 다.
 * ================================================================== */
const LINE = 'FF44546A';

/* 기존 테두리를 지우지 않고 한 변만 얹는다. 얹는 순서가 결과를 바꾸지 않아야 한다. */
function edge(ws, r, c, side, style, color) {
  const cell = ws.getCell(r, c);
  cell.border = Object.assign({}, cell.border, { [side]: { style, color: { argb: color || LINE } } });
}

const hline = (ws, r, c1, c2, style) => { for (let c = c1; c <= c2; c++) edge(ws, r, c, 'bottom', style || 'medium'); };
const vline = (ws, c, r1, r2, style) => { for (let r = r1; r <= r2; r++) edge(ws, r, c, 'right', style || 'medium'); };

/* 사각형 바깥 테두리 */
function rect(ws, r1, c1, r2, c2, style) {
  for (let c = c1; c <= c2; c++) { edge(ws, r1, c, 'top', style); edge(ws, r2, c, 'bottom', style); }
  for (let r = r1; r <= r2; r++) { edge(ws, r, c1, 'left', style); edge(ws, r, c2, 'right', style); }
}

function put(ws, r, c, v, o) {
  const cell = ws.getCell(r, c);
  cell.value = v === '' ? null : pub(v);
  cell.font = Object.assign({ size: 9 }, (o || {}).font);
  cell.alignment = Object.assign({ vertical: 'middle', horizontal: 'left' }, (o || {}).align);
  if ((o || {}).fill) cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: o.fill } };
  return cell;
}

const merge = (ws, r1, c1, r2, c2) => { try { ws.mergeCells(r1, c1, r2, c2); } catch (e) { /* 이미 병합 */ } };

/*
 * Entity 상자 하나.
 *   o = { r, c, w, title, sub, rows: [[a, b, c], …] }
 *   w 는 차지하는 열 수이며 rows 의 각 배열 길이와 같아야 한다.
 *   반환값은 { top, bottom, left, right } — 연결선을 그 좌표에 붙인다.
 */
function entity(ws, o) {
  const { r, c, w, title, sub, rows } = o;
  let y = r;

  merge(ws, y, c, y, c + w - 1);
  put(ws, y, c, title, { font: { size: 11, bold: true, color: { argb: 'FFFFFFFF' } },
                         align: { horizontal: 'center' }, fill: NAVY });
  for (let i = 1; i < w; i++) put(ws, y, c + i, '', { fill: NAVY });
  y++;

  if (sub) {
    merge(ws, y, c, y, c + w - 1);
    put(ws, y, c, sub, { font: { size: 8, italic: true, color: { argb: 'FF44546A' } },
                         align: { horizontal: 'center' }, fill: 'FFEAEEF6' });
    for (let i = 1; i < w; i++) put(ws, y, c + i, '', { fill: 'FFEAEEF6' });
    y++;
  }

  const first = y;
  for (const row of rows) {
    row.forEach((v, i) => {
      const key = /^(PK|FK|UQ|PK,FK)$/.test(String(row[0] || ''));
      put(ws, y, c + i, v, {
        font: { size: 9, bold: i === 0 || (i === 1 && key) },
        align: { horizontal: i === 0 ? 'center' : 'left' },
        fill: key ? 'FFFFF6E0' : 'FFFFFFFF',
      });
      edge(ws, y, c + i, 'bottom', 'hair', 'FFD5DCE8');
    });
    y++;
  }
  const bottom = y - 1;
  rect(ws, r, c, bottom, c + w - 1, 'medium');
  return { top: r, bottom, left: c, right: c + w - 1, first, title };
}

module.exports.edge = edge;
module.exports.hline = hline;
module.exports.vline = vline;
module.exports.rect = rect;
module.exports.put = put;
module.exports.merge = merge;
module.exports.entity = entity;
module.exports.NAVY = NAVY;
