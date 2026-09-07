/*
 * inspect_erd.js — 생성된 ERD 시트를 텍스트로 되그린다.
 *
 * 그림은 오류 없이 만들어져도 상자가 겹치거나 선이 엉뚱한 칸에 갈 수 있다.
 * 눈으로 열지 않고 배치를 판정하려면 되그려 보는 수밖에 없다.
 *
 * 실행: node tools/docgen/xlsx/inspect_erd.js [시트명]
 */
'use strict';
const ExcelJS = require('D:/tmp/hcwork/gen/node_modules/exceljs');
const OUT = 'D:/AIDEV/HealthCheckupReservationReception/docs/baseline/output/04_검진_예약접수_DB설계서.xlsx';

const w = s => [...String(s)].reduce((a, ch) => a + (ch.charCodeAt(0) > 0x2000 ? 2 : 1), 0);
const pad = (s, n) => { s = String(s == null ? '' : s); const d = n - w(s); return d > 0 ? s + ' '.repeat(d) : s; };
const clip = (s, n) => { s = String(s == null ? '' : s); let o = '', k = 0;
  for (const ch of s) { const cw = ch.charCodeAt(0) > 0x2000 ? 2 : 1; if (k + cw > n) break; o += ch; k += cw; } return o; };

(async () => {
  const wb = new ExcelJS.Workbook();
  await wb.xlsx.readFile(OUT);
  const want = process.argv[2];

  for (const ws of wb.worksheets) {
    if (!/ERD/.test(ws.name)) continue;
    if (want && ws.name !== want) continue;

    const cols = ws.columns.length;
    let maxRow = 0;
    ws.eachRow({ includeEmpty: false }, (r, i) => { maxRow = i; });

    // 병합 범위를 모아 둔다 — 값이 좌상단에만 있으므로 표시에 필요하다.
    const merged = new Map();
    for (const key of Object.keys(ws._merges || {})) {
      const m = ws._merges[key];
      const { top, left, bottom, right } = m.model || m;
      for (let r = top; r <= bottom; r++) for (let c = left; c <= right; c++)
        merged.set(r + ',' + c, top + ',' + left);
    }

    console.log('\n================ ' + ws.name + '  (열 ' + cols + ' · 행 ' + maxRow + ') ================');
    console.log('열 너비: ' + ws.columns.map((c, i) => (i + 1) + ':' + c.width).join('  '));
    console.log('기호  █ 제목  ▒ 부제  │ 세로선  ─ 가로선  ┆┈ 점선');

    const CW = 8;   // 되그릴 때 한 열에 쓰는 글자수
    for (let r = 1; r <= maxRow; r++) {
      let line = pad(r, 3) + '|';
      let any = false;
      for (let c = 1; c <= cols; c++) {
        const cell = ws.getCell(r, c);
        const src = merged.get(r + ',' + c);
        let v = cell.value;
        if (src && src !== r + ',' + c) v = '';
        v = v == null ? '' : String(v);

        const b = cell.border || {};
        const fill = (cell.fill && cell.fill.fgColor && cell.fill.fgColor.argb) || '';
        let mark = ' ';
        if (fill === 'FF1F3864') mark = '\u2588';
        else if (fill === 'FFEAEEF6') mark = '\u2592';
        else if (fill === 'FFFFF6E0') mark = '\u00b7';

        let txt = clip(v, CW - 2);
        if (v && !txt) txt = '#';
        let seg = mark + pad(txt, CW - 2);

        // 오른쪽 테두리 표시
        const rs = b.right && b.right.style;
        seg += rs === 'medium' || rs === 'thick' ? '\u2502' : rs === 'dashed' ? '\u250a' : rs === 'thin' ? ':' : ' ';
        line += seg;
        if (v || mark !== ' ' || rs) any = true;
      }
      // 아래 테두리가 있는 행이면 그 아래에 줄을 하나 더 그린다
      let under = '   +';
      let hasUnder = false;
      for (let c = 1; c <= cols; c++) {
        const bs = (ws.getCell(r, c).border || {}).bottom;
        const st = bs && bs.style;
        const ch = st === 'medium' || st === 'thick' ? '\u2500' : st === 'dashed' ? '\u2508' : st === 'hair' || st === 'thin' ? '.' : ' ';
        if (ch !== ' ') hasUnder = true;
        under += ch.repeat(CW);
      }
      if (any) console.log(line);
      if (hasUnder) console.log(under);
    }
  }
})().catch(e => { console.error(e); process.exit(1); });
