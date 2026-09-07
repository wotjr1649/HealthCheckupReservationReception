'use strict';
// 프로세스 정의서용 도형 어휘 — 03 화면설계서의 spec/canvas를 그대로 쓰고 노드 종류만 추가한다.
// pptxgenjs에는 굽은(bent) 커넥터가 없으므로 꺾인 화살표는 직선 2~3개를 손으로 이어 그린다.
const S = require('./spec');
const { F, wrap, lineH } = require('./canvas');

// 본문(다이어그램) 영역 — 03과 달리 우측 설명표가 없어 전폭을 쓴다.
const A = { x: 0.46, y: 1.02, w: 12.41, h: 5.85, r: 12.87, b: 6.87 };
const TS = { node: 9, small: 8, edge: 8, head: 10, cap: 9 };

function fitLines(c, label, size, innerW, h, tag) {
  const lines = wrap(label, size, innerW);
  if (lines.length * lineH(size) > h + 0.005) {
    c.warnings.push(`넘침 ${tag || ''} "${label}" ${lines.length}줄 → ${h.toFixed(2)}in`);
  }
  const over = lines.filter(l => F.widthIn(l, size) > innerW + 0.005);
  if (over.length) c.warnings.push(`가로넘침 ${tag || ''} "${over[0]}"`);
  return lines;
}

// 일반 처리 노드
function node(c, x, y, w, h, label, o = {}) {
  const size = o.size ?? TS.node;
  c.rect(x, y, w, h, { stroke: o.stroke ?? S.C.ink, sw: o.sw ?? S.W.ctrl, fill: o.fill ?? null, dash: o.dash });
  if (label) {
    c.text(x, y, w, h, '', {
      size, bold: !!o.bold, color: o.color ?? S.C.inkText, align: 'center', valign: 'middle',
      lines: fitLines(c, label, size, w - 0.10, h, o.tag),
    });
  }
  return { x, y, w, h, cx: x + w / 2, cy: y + h / 2, r: x + w, b: y + h };
}

// 판정 노드 — 마름모 대신 옅은 채움 + `?` 로 끝나는 라벨(문서 전체 일관)
function gate(c, x, y, w, h, label, o = {}) {
  return node(c, x, y, w, h, label, { ...o, fill: o.fill ?? S.C.band, sw: o.sw ?? S.W.panel });
}

// 시작/종료 노드 — 얇은 이중 테두리
function term(c, x, y, w, h, label, o = {}) {
  const n = node(c, x, y, w, h, label, { ...o, sw: o.sw ?? S.W.panel, fill: o.fill ?? S.C.white });
  c.rect(x + 0.035, y + 0.035, w - 0.07, h - 0.07, { stroke: o.stroke ?? S.C.ink, sw: S.W.hair });
  return n;
}

// 하위 프로세스 호출 노드 — mermaid [[ ]] 대응(좌우 이중 세로선)
function sub(c, x, y, w, h, label, o = {}) {
  const n = node(c, x, y, w, h, label, o);
  c.line(x + 0.06, y, x + 0.06, y + h, { stroke: o.stroke ?? S.C.ink, sw: S.W.hair });
  c.line(x + w - 0.06, y, x + w - 0.06, y + h, { stroke: o.stroke ?? S.C.ink, sw: S.W.hair });
  return n;
}

// 구역 제목 띠
function head(c, x, y, w, text, o = {}) {
  const h = o.h ?? 0.26;
  c.rect(x, y, w, h, { fill: S.C.band, stroke: S.C.ink, sw: S.W.hair });
  c.text(x + 0.10, y, w - 0.20, h, text, { size: o.size ?? TS.head, bold: true, align: 'left', color: S.C.data });
  return y + h;
}

// 설명/주석 상자 (여러 줄, 좌측 정렬)
function note(c, x, y, w, lines, o = {}) {
  const size = o.size ?? TS.small;
  const lh = lineH(size);
  const all = [];
  for (const l of lines) for (const s of wrap(l, size, w - 0.24)) all.push(s);
  const h = o.h ?? all.length * lh + 0.16;
  c.rect(x, y, w, h, { stroke: o.stroke ?? S.C.ink, sw: o.sw ?? S.W.hair, fill: o.fill ?? null, dash: o.dash });
  c.text(x + 0.12, y + 0.08, w - 0.24, h - 0.16, '', {
    size, color: o.color ?? S.C.data, align: 'left', valign: 'top', lines: all, bold: !!o.bold,
  });
  return { x, y, w, h, b: y + h, r: x + w };
}

// 꺾은 화살표 — 점 배열을 직선 여러 개로 잇고 마지막 구간에만 화살촉을 단다.
function ar(c, pts, o = {}) {
  for (let i = 1; i < pts.length; i++) {
    c.line(pts[i - 1][0], pts[i - 1][1], pts[i][0], pts[i][1], {
      stroke: o.stroke ?? S.C.ink, sw: o.sw ?? S.W.ctrl, dash: o.dash,
      arrow: i === pts.length - 1 ? (o.arrow !== false) : false,
    });
  }
}

// 간선 라벨 — 선 위에 얹을 때만 흰 마스크를 깐다(mask:false 면 도형 테두리를 지우지 않는다).
function elab(c, cx, cy, text, o = {}) {
  const size = o.size ?? TS.edge;
  const w = F.widthIn(text, size) + 0.10, h = lineH(size) + 0.02;
  if (o.mask !== false) c.rect(cx - w / 2, cy - h / 2, w, h, { fill: S.C.white, stroke: S.C.white, sw: 0.25 });
  c.text(cx - w / 2, cy - h / 2, w, h, text, { size, color: o.color ?? S.C.hint, align: 'center' });
}
// 같은 행에서 이웃 노드로 가는 짧은 가로 화살표의 Yes/No — 선 위쪽에 마스크 없이 얹는다.
function elabN(c, cx, cy, text) { elab(c, cx, cy, text, { mask: false }); }

// 표 — addTable은 행 높이를 계산하지 않으므로(<a:tr h="0">) 사각형+텍스트로 직접 그린다.
// cols: [{t, w, align}], rows: [[...]] / {cells, bold}
function table(c, x, y, cols, rows, o = {}) {
  const size = o.size ?? TS.small, headSize = o.headSize ?? TS.small;
  const lh = lineH(size), pad = o.pad ?? 0.05;
  const wrapCell = (v, col) => wrap(String(v ?? ''), size, col.w - pad * 2 - 0.04);
  const rh = [];
  for (const r of rows) {
    const cells = Array.isArray(r) ? r : r.cells;
    let n = 1;
    cells.forEach((v, i) => { n = Math.max(n, wrapCell(v, cols[i]).length); });
    rh.push(Math.max(o.rowH ?? 0.24, n * lh + pad * 2));
  }
  const hh = o.headH ?? 0.26;
  let hx = x;
  cols.forEach(col => {
    c.rect(hx, y, col.w, hh, { fill: S.C.band, stroke: S.C.ink, sw: S.W.hair });
    c.text(hx + pad, y, col.w - pad * 2, hh, col.t, { size: headSize, bold: true, color: S.C.data, align: col.align === 'left' ? 'left' : 'center' });
    hx += col.w;
  });
  let ry = y + hh;
  rows.forEach((r, ri) => {
    const cells = Array.isArray(r) ? r : r.cells;
    const bold = !Array.isArray(r) && r.bold;
    let cx = x;
    cells.forEach((v, i) => {
      const col = cols[i];
      c.rect(cx, ry, col.w, rh[ri], { stroke: S.C.dim, sw: S.W.hair, fill: (!Array.isArray(r) && r.fill) || null });
      if (v !== '' && v != null) {
        const al = col.align === 'left' ? 'left' : 'center';
        c.text(cx + pad, ry + pad, col.w - pad * 2, rh[ri] - pad * 2, '', {
          size, bold, color: S.C.data, align: al, valign: 'middle', lines: wrapCell(v, col),
        });
      }
      cx += col.w;
    });
    ry += rh[ri];
  });
  return { bottom: ry, width: cols.reduce((s, col) => s + col.w, 0) };
}

// 제목 + 하단 정책 캡션
function frame(c, title, caption) {
  c.text(S.PAGE.title.x, S.PAGE.title.y, 12.41, S.PAGE.title.h, title, {
    size: S.PAGE.title.size, bold: true, color: '000000', align: 'left',
  });
  if (caption) {
    c.text(A.x, 6.98, A.w, 0.26, caption, { size: TS.cap, color: S.C.dimText, align: 'left', valign: 'middle' });
    if (F.widthIn(caption, TS.cap) > A.w) c.warnings.push(`캡션 가로넘침: ${caption}`);
  }
}

module.exports = { A, TS, node, gate, term, sub, head, note, ar, elab, elabN, table, frame, S, F, wrap, lineH, fitLines };
