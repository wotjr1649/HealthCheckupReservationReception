'use strict';
// 부록용 전폭 표 페이지. 행 높이를 실측으로 계산하고, 넘치면 자동으로 다음 장으로 넘긴다.
// (pptxgenjs의 addTable은 행 높이를 계산하지 않아 넘침이 생성 시점에 보이지 않으므로 쓰지 않는다.)
const S = require('./spec');
const { Canvas, F, wrap, lineH } = require('./canvas');

const AREA = { x: 0.46, y: 1.02, w: 12.41, maxH: 5.92 };

// blocks: [{ kind:'table', caption, cols:[{t,w|flex,align}], rows:[[..]|{cells,dim}], size }
//          | { kind:'note', lines:[...] }]
// 반환: [{ title, draw(c) }]
function tablePages(title, blocks, o = {}) {
  const pad = o.pad ?? 0.045;
  const fs = o.size ?? 8.5, hs = o.headSize ?? 8.5;
  const lh = lineH(fs), hlh = lineH(hs);

  // 1) 모든 블록을 "그리기 단위" 목록으로 펼치고 높이를 실측한다
  const units = [];
  for (const b of blocks) {
    if (b.kind === 'note') {
      const ls = b.lines.flatMap(t => wrap(t, fs, AREA.w - 0.20));
      units.push({ h: ls.length * lh + 0.10, draw: (c, y) => {
        ls.forEach((t, i) => c.text(AREA.x + 0.06, y + 0.06 + i * lh, AREA.w - 0.12, lh, t,
          { size: fs, align: 'left', color: S.C.hint }));
      }, keepWithNext: false });
      continue;
    }
    const cols = fitCols(AREA.w, b.cols);
    if (b.caption) {
      units.push({ h: 0.26, draw: (c, y) => c.text(AREA.x, y + 0.02, AREA.w, 0.22, b.caption,
        { size: 10, align: 'left', bold: true, color: S.C.data }), keepWithNext: true });
    }
    // 헤더
    const headLines = cols.map(col => wrap(col.t, hs, col.w - pad * 2));
    const headH = Math.max(...headLines.map(l => l.length)) * hlh + pad * 2;
    const headU = { h: headH, repeat: true, draw: (c, y) => {
      let x = AREA.x;
      cols.forEach((col, i) => {
        c.rect(x, y, col.w, headH, { fill: S.C.band, sw: S.W.hair });
        c.text(x + pad, y + pad, col.w - pad * 2, headH - pad * 2, '', {
          size: hs, bold: true, align: col.align === 'left' ? 'left' : 'center', color: S.C.data,
          valign: 'top', lines: headLines[i],
        });
        x += col.w;
      });
    }, keepWithNext: true };
    units.push(headU);
    for (const r of (b.rows || [])) {
      const cells = Array.isArray(r) ? r : r.cells;
      const dim = !Array.isArray(r) && r.dim;
      const ls = cols.map((col, i) => wrap(cells[i] ?? '', b.size ?? fs, col.w - pad * 2));
      const h = Math.max(...ls.map(l => l.length)) * lineH(b.size ?? fs) + pad * 2;
      units.push({ h, headOf: headU, draw: (c, y) => {
        let x = AREA.x;
        cols.forEach((col, i) => {
          c.rect(x, y, col.w, h, { stroke: S.C.dim, sw: S.W.hair });
          if ((cells[i] ?? '') !== '') {
            c.text(x + pad, y + pad, col.w - pad * 2, h - pad * 2, '', {
              size: b.size ?? fs, align: col.align === 'left' ? 'left' : 'center',
              color: dim ? S.C.dimText : S.C.data, valign: 'top', lines: ls[i],
            });
          }
          x += col.w;
        });
      } });
    }
    units.push({ h: 0.10, draw: () => {}, spacer: true });
  }

  // 2) 페이지 분할 — 넘치면 다음 장, 표가 끊기면 헤더를 다시 그린다
  const pages = [];
  let cur = [], y = AREA.y, lastHead = null;
  const flush = () => { if (cur.length) { const items = cur; pages.push({ items }); cur = []; y = AREA.y; } };
  for (const u of units) {
    if (y + u.h > AREA.y + AREA.maxH) {
      // 앞 장 끝에 남은 "다음과 붙어야 하는" 단위(캡션·헤더)를 같이 넘긴다.
      // 넘기지 않으면 캡션만 앞 장에 고아로 남고 표는 다음 장에서 시작한다.
      const carry = [];
      while (cur.length && cur[cur.length - 1].u.keepWithNext) carry.unshift(cur.pop().u);
      flush();
      carry.forEach(cu => { cur.push({ u: cu, y }); y += cu.h; });
      if (u.headOf && !carry.includes(u.headOf)) { cur.push({ u: u.headOf, y }); y += u.headOf.h; }
    }
    if (u.repeat) lastHead = u;
    cur.push({ u, y });
    y += u.h;
  }
  flush();
  // 마지막 장이 고아(주석 1~2개)면 앞 장으로 되돌려 붙인다 — 자투리 슬라이드 방지
  while (pages.length > 1) {
    const last = pages[pages.length - 1];
    const real = last.items.filter(i => !i.u.spacer);
    if (real.length > 2) break;
    const prev = pages[pages.length - 2];
    let py = Math.max(...prev.items.map(i => i.y + i.u.h));
    const need = real.reduce((s, i) => s + i.u.h, 0);
    if (py + need > AREA.y + AREA.maxH + 0.35) break;
    real.forEach(i => { prev.items.push({ u: i.u, y: py }); py += i.u.h; });
    pages.pop();
  }

  return pages.map((p, i) => ({
    title: i === 0 ? title : title + '  (계속)',
    fullWidth: true,
    draw: (c) => {
      c.text(S.PAGE.title.x, S.PAGE.title.y, 12.4, S.PAGE.title.h, i === 0 ? title : title + '  (계속)',
        { size: S.PAGE.title.size, bold: true, color: '000000', align: 'left' });
      p.items.forEach(({ u, y }) => u.draw(c, y));
    },
  }));
}

function fitCols(totalW, defs) {
  const fixed = defs.filter(d => typeof d.w === 'number').reduce((s, d) => s + d.w, 0);
  const flexTot = defs.filter(d => d.flex).reduce((s, d) => s + d.flex, 0);
  return defs.map(d => ({ ...d, w: typeof d.w === 'number' ? d.w : (totalW - fixed) * d.flex / flexTot }));
}

module.exports = { tablePages, AREA };
