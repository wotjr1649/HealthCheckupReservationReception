'use strict';
// 화면 어휘 — 모든 화면이 같은 부품으로 그려져야 한 벌로 보인다.
const S = require('./spec');
const { F, wrap, lineH } = require('./canvas');

const ROW = 0.195;            // 그리드 행 높이 (9pt 줄상자 0.166in + 여백)
const FIELD_H = 0.24;         // 입력/표시 필드 높이
const BTN = { w: 1.05, h: 0.28 };

// ── 창 셸 ────────────────────────────────────────────────────────────
// 상단 Navigation + Context Ribbon + 업무 Tab 스트립 + 하단 업무 상태바.
// 반환값은 업무 View 사각형.
function shell(c, { nav, navActive, ribbon, tabs, tabActive, status = '업무 상태 :  업무 가능', marks = {} }) {
  const F0 = S.PAGE.frame;
  const x0 = F0.x, y0 = F0.y, W = F0.w, H = F0.h;
  c.rect(x0, y0, W, H, { stroke: S.C.ink, sw: S.W.win });

  let y = y0;
  // Navigation
  c.rect(x0, y, W, S.CHROME.nav, { stroke: S.C.ink, sw: S.W.panel, fill: S.C.band });
  nav.forEach((n, i) => {
    const bw = 1.28, bx = x0 + 0.12 + i * (bw + 0.06);
    c.box(bx, y + 0.04, bw, S.CHROME.nav - 0.08, n, {
      bold: i === navActive, sw: i === navActive ? S.W.panel : S.W.ctrl, fill: i === navActive ? S.C.white : null,
    });
  });
  if (marks.nav) c.markLeft(x0 + 0.12, y + 0.04, S.CHROME.nav - 0.08, marks.nav);
  y += S.CHROME.nav;

  // Context Ribbon — [그룹명 / 버튼들] 을 세로 2단으로
  c.rect(x0, y, W, S.CHROME.ribbon, { stroke: S.C.ink, sw: S.W.panel });
  let rx = x0 + 0.26;
  ribbon.forEach((grp, gi) => {
    const bw = grp.buttons.map(b => Math.max(0.80, F.widthIn(b.t, S.TEXT.label) + 0.30));
    const gw = bw.reduce((s, v) => s + v + 0.08, 0) - 0.08;
    c.text(rx, y + 0.04, gw, 0.16, grp.name, { size: S.TEXT.small, align: 'left', color: S.C.hint });
    let bx = rx;
    grp.buttons.forEach((b, i) => {
      c.box(bx, y + 0.22, bw[i], 0.30, b.t, { sw: b.on === false ? S.W.hair : S.W.panel, dim: b.on === false, bold: !!b.bold });
      if (b.mark) c.markLeft(bx, y + 0.22, 0.30, b.mark);
      bx += bw[i] + 0.08;
    });
    rx += gw + 0.22;
    if (gi < ribbon.length - 1) { c.line(rx - 0.11, y + 0.06, rx - 0.11, y + S.CHROME.ribbon - 0.06, { stroke: S.C.dim }); }
  });
  if (marks.ribbon) c.markLeft(x0 + 0.26, y + 0.22, 0.30, marks.ribbon);
  y += S.CHROME.ribbon;

  // 업무 Tab
  c.rect(x0, y, W, S.CHROME.tab, { stroke: S.C.ink, sw: S.W.panel, fill: S.C.band });
  let tx = x0 + 0.12;
  tabs.forEach((t, i) => {
    const bw = Math.max(1.30, F.widthIn(t, S.TEXT.small) + 0.34);
    c.box(tx, y + 0.02, bw, S.CHROME.tab - 0.04, t + '  ×', {
      bold: i === tabActive, sw: i === tabActive ? S.W.panel : S.W.hair, fill: i === tabActive ? S.C.white : null, size: S.TEXT.small,
    });
    tx += bw + 0.05;
  });
  y += S.CHROME.tab;

  const viewY = y, viewH = H - S.CHROME.nav - S.CHROME.ribbon - S.CHROME.tab - S.CHROME.status;

  // 상태바
  const stY = y0 + H - S.CHROME.status;
  c.rect(x0, stY, W, S.CHROME.status, { stroke: S.C.ink, sw: S.W.panel, fill: S.C.band });
  c.text(x0 + 0.26, stY, 5.0, S.CHROME.status, status, { size: S.TEXT.small, align: 'left', color: S.C.hint });
  if (marks.status) c.markLeft(x0, stY, S.CHROME.status, marks.status);

  const inset = 0.12;
  return { x: x0 + inset, y: viewY, w: W - inset * 2, h: viewH, x0, y0, W, H };
}

// ── 패널 (캡션 + 구분선) → 내부 사각형 반환 ────────────────────────
function panel(c, x, y, w, h, caption, o = {}) {
  c.rect(x, y, w, h, { sw: S.W.panel, stroke: o.stroke ?? S.C.ink });
  if (caption) {
    c.text(x + 0.08, y + 0.03, w - 0.16, 0.18, caption, { size: S.TEXT.small, align: 'left', bold: true, color: S.C.hint });
    c.line(x, y + 0.23, x + w, y + 0.23, { stroke: S.C.dim });
    return { x: x + 0.10, y: y + 0.27, w: w - 0.20, h: h - 0.27 - 0.08 };
  }
  return { x: x + 0.10, y: y + 0.08, w: w - 0.20, h: h - 0.16 };
}

// ── 라벨 + 값 박스 ──────────────────────────────────────────────────
function field(c, x, y, labelW, boxW, label, value, o = {}) {
  c.text(x, y, labelW, FIELD_H, label, { size: S.TEXT.small, align: 'left', color: S.C.hint });
  c.box(x + labelW, y, boxW, FIELD_H, value ?? '', {
    size: S.TEXT.gridData, color: o.dim ? S.C.dimText : S.C.data, dim: o.dim, fill: o.readonly ? S.C.band : null,
  });
  return x + labelW + boxW;
}

// ── 그리드 ──────────────────────────────────────────────────────────
// cols: [{t:'헤더', w:1.2, align:'left'|'center'}], rows: [[v,v,...]] 또는 {cells:[...], dim:true}
function grid(c, x, y, cols, rows, o = {}) {
  const rh = o.rowH ?? ROW, fs = o.size ?? S.TEXT.gridData;
  let hx = x;
  cols.forEach(col => {
    c.rect(hx, y, col.w, rh, { fill: S.C.band, sw: S.W.hair });
    c.text(hx + (col.align === 'left' ? 0.06 : 0), y, col.w - (col.align === 'left' ? 0.06 : 0), rh, col.t,
      { size: o.headSize ?? S.TEXT.gridHead, bold: true, align: col.align === 'left' ? 'left' : 'center', color: S.C.data });
    hx += col.w;
  });
  let ry = y + rh;
  for (const r of rows) {
    const cells = Array.isArray(r) ? r : r.cells;
    const dim = !Array.isArray(r) && r.dim;
    let cxx = x;
    cells.forEach((v, i) => {
      const col = cols[i];
      c.rect(cxx, ry, col.w, rh, { stroke: S.C.dim, sw: S.W.hair });
      if (v !== '' && v != null) {
        const al = col.align === 'left' ? 'left' : 'center';
        c.text(cxx + (al === 'left' ? 0.06 : 0), ry, col.w - (al === 'left' ? 0.06 : 0), rh, v,
          { size: col.size ?? fs, align: al, color: dim ? S.C.dimText : S.C.data });
      }
      cxx += col.w;
    });
    ry += rh;
  }
  return { bottom: ry, width: cols.reduce((s, col) => s + col.w, 0) };
}

// 폭 배분 헬퍼: 남는 폭을 비율로 나눈다
function cols(totalW, defs) {
  const fixed = defs.filter(d => typeof d.w === 'number').reduce((s, d) => s + d.w, 0);
  const flexTot = defs.filter(d => d.flex).reduce((s, d) => s + d.flex, 0);
  return defs.map(d => ({ ...d, w: typeof d.w === 'number' ? d.w : (totalW - fixed) * d.flex / flexTot }));
}

// ── 모달: 부모 화면을 회색 외곽으로 남기고 그 위에 올린다 ──────────
function modal(c, { w, h, title, buttons, parent = '예약/접수 공통 Workbench (WF-WRK-01)' }) {
  const F0 = S.PAGE.frame;
  // 부모 화면 (비활성)
  c.rect(F0.x, F0.y, F0.w, F0.h, { stroke: S.C.parent, sw: S.W.panel });
  c.rect(F0.x, F0.y, F0.w, 0.28, { stroke: S.C.parent, sw: S.W.hair, fill: S.C.band });
  c.text(F0.x + 0.14, F0.y, 6.0, 0.28, parent + '   —   비활성(Modal)', { size: S.TEXT.small, align: 'left', color: S.C.dimText });

  const mx = F0.x + (F0.w - w) / 2, my = F0.y + (F0.h - h) / 2;
  c.rect(mx, my, w, h, { stroke: S.C.ink, sw: S.W.win, fill: S.C.white });
  // 제목 탭 — 레퍼런스 방식: 상단 경계에 걸친 작은 박스
  const tw = Math.max(1.6, F.widthIn(title, S.TEXT.label) + 0.50);
  c.box(mx + (w - tw) / 2, my - 0.15, tw, 0.30, title, { sw: S.W.panel, fill: S.C.white, bold: true });

  // 하단 버튼 — 우측 정렬
  const bY = my + h - 0.42;
  let bx = mx + w - 0.16;
  const placed = [];
  [...buttons].reverse().forEach(b => {
    const bw = Math.max(BTN.w, F.widthIn(b.t ?? b, S.TEXT.label) + 0.40);
    bx -= bw;
    c.box(bx, bY, bw, BTN.h, b.t ?? b, { sw: S.W.panel, bold: !!b.primary });
    placed.unshift({ x: bx, y: bY, w: bw, h: BTN.h, t: b.t ?? b });
    bx -= 0.10;
  });
  return { x: mx, y: my, w, h, inner: { x: mx + 0.16, y: my + 0.26, w: w - 0.32, h: h - 0.26 - 0.56 }, buttons: placed };
}

// ── 확인창 (Confirmation) ───────────────────────────────────────────
function confirm(c, x, y, w, h, { title, lines, buttons }) {
  c.rect(x, y, w, h, { stroke: S.C.ink, sw: S.W.win, fill: S.C.white });
  const tw = Math.max(1.5, F.widthIn(title, S.TEXT.label) + 0.50);
  c.box(x + (w - tw) / 2, y - 0.15, tw, 0.30, title, { sw: S.W.panel, fill: S.C.white, bold: true });
  const lh = lineH(S.TEXT.label);
  lines.forEach((ln, i) => {
    c.text(x + 0.18, y + 0.34 + i * (lh + 0.06), w - 0.36, lh, ln,
      { size: S.TEXT.label, align: 'left', color: i === 0 ? S.C.data : S.C.hint });
  });
  const bY = y + h - 0.42;
  let bx = x + w - 0.16;
  [...buttons].reverse().forEach(b => {
    const bw = Math.max(0.90, F.widthIn(b.t ?? b, S.TEXT.label) + 0.40);
    bx -= bw;
    c.box(bx, bY, bw, BTN.h, b.t ?? b, { sw: S.W.panel, bold: !!b.primary });
    bx -= 0.10;
  });
}

// ── 조회조건 밴드 ───────────────────────────────────────────────────
// items: [{label, w, value}] — 값이 없으면 빈 입력칸
function searchBand(c, x, y, w, rows, o = {}) {
  const h = rows.length * 0.32 + 0.30;
  const r = panel(c, x, y, w, h, o.caption ?? '조회조건');

  // 버튼을 먼저 배치해 우측 점유폭을 확정한다. 필드를 먼저 그리면 마지막 필드가
  // 우측정렬된 버튼에 덮여 화면에서 사라지고, 생성 시점에는 아무것도 알려주지 않는다.
  let bx = x + w - 0.14;
  if (o.buttons) {
    [...o.buttons].reverse().forEach(b => {
      const bw = Math.max(0.80, F.widthIn(b, S.TEXT.label) + 0.36);
      bx -= bw;
      c.box(bx, r.y, bw, FIELD_H + 0.02, b, { sw: S.W.panel });
      bx -= 0.08;
    });
  }
  const fieldRight = o.buttons ? bx - 0.02 : x + w - 0.14;

  rows.forEach((row, ri) => {
    let fx = r.x;
    row.forEach(f => {
      const end = fx + (f.lw ?? 0.68) + (f.w ?? 1.30);
      if (ri === 0 && o.buttons && end > fieldRight) {
        c.warnings.push(`조회조건 겹침: "${f.label}" 우측 ${end.toFixed(2)}in > 버튼 경계 ${fieldRight.toFixed(2)}in`);
      }
      fx = field(c, fx, r.y + ri * 0.32, f.lw ?? 0.68, f.w ?? 1.30, f.label, f.value ?? '') + 0.16;
    });
  });
  return { bottom: y + h };
}

module.exports = { ROW, FIELD_H, BTN, shell, panel, field, grid, cols, modal, confirm, searchBand };
