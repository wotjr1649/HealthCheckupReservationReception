'use strict';
// 한 벌의 도형 목록에서 PPTX와 SVG를 동시에 뽑는다.
// SVG는 Chrome headless로 PNG를 떠서 사람이 눈으로 확인하는 용도 — 생성물이 실제로 어떻게 보이는지
// 확인하지 않는 것이 지난 실패의 직접 원인이었다.
const { loadFont } = require('../font');
const S = require('./spec');

const F = loadFont(S.FONT_PATH);

function esc(s) {
  return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// 실측 폭 기준 줄바꿈. 동아시아 문자는 어절 경계가 없으므로 글자 단위, 라틴은 공백 단위.
function wrap(text, pt, widthIn) {
  const lines = [];
  for (const para of String(text).split('\n')) {
    let cur = '';
    const toks = para.match(/[A-Za-z0-9()·.,%~\/\-]+|\s+|[^\s]/g) || [];
    for (const t of toks) {
      const next = cur + t;
      if (F.widthIn(next.trimEnd(), pt) > widthIn && cur.trim() !== '') {
        lines.push(cur.trimEnd());
        cur = /^\s+$/.test(t) ? '' : t;
      } else {
        cur = next;
      }
    }
    lines.push(cur.trimEnd());
  }
  return lines;
}

function lineH(pt) { return pt * S.LINE / 72; }

// z-order는 pptxgenjs에서 추가 순서가 전부다. layer로 명시하고 렌더 직전 안정 정렬한다.
//   0 = 화면 도형/텍스트, 1 = 빨간 영역 강조, 2 = 콜아웃
class Canvas {
  constructor() { this.items = []; this.warnings = []; this.layer = 0; }

  at(layer, fn) { const p = this.layer; this.layer = layer; fn(this); this.layer = p; return this; }
  sorted() { return this.items.map((it, i) => [it, i]).sort((a, b) => (a[0].z - b[0].z) || (a[1] - b[1])).map(p => p[0]); }

  rect(x, y, w, h, o = {}) {
    this.items.push({ t: 'rect', z: o.z ?? this.layer, x, y, w, h, stroke: o.stroke ?? S.C.ink, sw: o.sw ?? S.W.ctrl, fill: o.fill ?? null, dash: o.dash ?? null, ctl: !!o.ctl });
    return this;
  }
  line(x1, y1, x2, y2, o = {}) {
    this.items.push({ t: 'line', z: o.z ?? this.layer, x1, y1, x2, y2, stroke: o.stroke ?? S.C.ink, sw: o.sw ?? S.W.hair, dash: o.dash ?? null, arrow: o.arrow ?? null });
    return this;
  }
  ellipse(x, y, w, h, o = {}) {
    this.items.push({ t: 'ellipse', z: o.z ?? this.layer, x, y, w, h, stroke: o.stroke ?? S.C.ink, sw: o.sw ?? S.W.ctrl, fill: o.fill ?? null });
    return this;
  }
  // 단일 줄 텍스트. 박스 안 가운데 정렬이 기본(레퍼런스 방식).
  text(x, y, w, h, str, o = {}) {
    this.items.push({
      t: 'text', z: o.z ?? this.layer, x, y, w, h, str: String(str),
      size: o.size ?? S.TEXT.label, bold: !!o.bold, color: o.color ?? S.C.inkText,
      align: o.align ?? 'center', valign: o.valign ?? 'middle', lines: o.lines ?? null,
    });
    return this;
  }
  // 라벨이 담긴 박스 — 레퍼런스의 기본 어휘.
  // `ctl: true` 로 표시해 둔다. 겹침 검사(checkOverlaps)가 보는 것은 이 부류뿐이다 —
  // 패널·그리드칸은 rect() 로 그리며 자식을 품는 것이 정상이라 대상이 아니다.
  box(x, y, w, h, label, o = {}) {
    this.rect(x, y, w, h, { stroke: o.stroke ?? (o.dim ? S.C.dim : S.C.ink), sw: o.sw ?? S.W.ctrl, fill: o.fill ?? null, dash: o.dash, ctl: true });
    if (label != null && label !== '') {
      this.text(x, y, w, h, label, {
        size: o.size ?? S.TEXT.label, bold: o.bold, align: o.align ?? 'center',
        color: o.color ?? (o.dim ? S.C.dimText : S.C.inkText),
      });
    }
    return this;
  }
  // 빨간 원형 콜아웃. 하위번호(1-1)는 실측상 0.24in 원에 안 들어가므로 가로 타원(최소 0.257in).
  // 레퍼런스 방식: 대상 요소의 왼쪽 경계선 위에 세로 중앙으로 걸친다 — 라벨 텍스트를 가리지 않는다.
  callout(cx, cy, label) {
    const sub = String(label).includes('-');
    const w = sub ? 0.36 : 0.24, h = 0.24;
    this.ellipse(cx - w / 2, cy - h / 2, w, h, { z: 2, fill: S.C.red, stroke: S.C.red, sw: 0.75 });
    this.text(cx - w / 2, cy - h / 2, w, h, label, { z: 2, size: S.TEXT.callout, bold: true, color: S.C.white });
    return this;
  }
  // 대상 박스의 좌측 경계에 세로 중앙으로 건다. 경계보다 살짝 바깥으로 밀어 내용 텍스트를 가리지 않게 한다.
  markLeft(x, y, h, label) { return this.callout(x - 0.04, y + h / 2, label); }
  // 대상 박스의 상단 경계 좌측 모서리에 건다 (가로로 넓은 영역용).
  markTop(x, y, label) { return this.callout(x + 0.06, y, label); }
  // 영역 강조 (빨간 사각형, 채움 없음)
  zone(x, y, w, h) { return this.rect(x, y, w, h, { z: 1, stroke: S.C.red, sw: 1.75 }); }

  // --- 겹침 검사 ----------------------------------------------------------
  // [X] 이런 검사가 없었다. searchBand 안의 자체 경고 하나뿐이었고 그마저 build.js 가
  //     **출력만 하고 실패시키지 않았다** — 초록인 채 아무것도 막지 않는 그 형태다.
  //     그 사이 DLG-HOL-01 의 [삭제] 와 modal 하단 [닫기] 가 0.81 x 0.13in 겹친 채
  //     R7·R8 두 회차를 통과했다. 사람이 pptx 를 열어야만 보였다.
  //
  //     대상은 box() 로 그린 컨트롤(버튼·입력칸·탭)뿐이다. 패널·그리드칸은 rect() 라
  //     자식을 품는 것이 정상이므로 보지 않는다. z 가 다르면 의도적으로 겹쳐 올린 것이다
  //     (콜아웃·모달 제목탭). 맞닿은 변은 겹침이 아니므로 EPS 만큼 물러서서 판정한다.
  //     반환값은 **이번에 찾은 겹침 수**다. `설명 44자 초과` 같은 기존 연성 경고와
  //     같은 배열에 담기되 실패 판정은 이 수로만 한다 — 오래 참아 온 미용 경고를
  //     지금 와서 실패로 만들면 13장이 한꺼번에 막히고, 그것은 이 결함과 무관하다.
  checkOverlaps() {
    const EPS = 0.01;                       // in. 선 두께·반올림으로 생기는 접촉을 흘린다
    let found = 0;
    const b = this.items.filter(i => i.t === 'rect' && i.ctl);
    for (let i = 0; i < b.length; i++) {
      for (let j = i + 1; j < b.length; j++) {
        const a = b[i], d = b[j];
        if (a.z !== d.z) continue;
        const ox = Math.min(a.x + a.w, d.x + d.w) - Math.max(a.x, d.x);
        const oy = Math.min(a.y + a.h, d.y + d.h) - Math.max(a.y, d.y);
        if (ox > EPS && oy > EPS) {
          found++;
          this.warnings.push(
            `컨트롤 겹침 ${ox.toFixed(2)}x${oy.toFixed(2)}in — ` +
            `(${a.x.toFixed(2)},${a.y.toFixed(2)} ${a.w.toFixed(2)}x${a.h.toFixed(2)}) 와 ` +
            `(${d.x.toFixed(2)},${d.y.toFixed(2)} ${d.w.toFixed(2)}x${d.h.toFixed(2)})`);
        }
      }
    }
    return found;
  }

  // --- 렌더러 -------------------------------------------------------------
  toSvg(px = 1600) {
    const k = px / S.SLIDE.w;
    const H = Math.round(S.SLIDE.h * k);
    const ITEMS = this.sorted();
    const o = [`<svg xmlns="http://www.w3.org/2000/svg" width="${px}" height="${H}" viewBox="0 0 ${px} ${H}">`,
      `<rect width="100%" height="100%" fill="#ffffff"/>`];
    const dash = d => d === 'dash' ? ` stroke-dasharray="${(0.08 * k).toFixed(1)},${(0.05 * k).toFixed(1)}"` : '';
    for (const i of ITEMS) {
      if (i.t === 'rect') {
        o.push(`<rect x="${(i.x * k).toFixed(2)}" y="${(i.y * k).toFixed(2)}" width="${(i.w * k).toFixed(2)}" height="${(i.h * k).toFixed(2)}" fill="${i.fill ? '#' + i.fill : 'none'}" stroke="#${i.stroke}" stroke-width="${(i.sw / 72 * k).toFixed(2)}"${dash(i.dash)}/>`);
      } else if (i.t === 'ellipse') {
        o.push(`<ellipse cx="${((i.x + i.w / 2) * k).toFixed(2)}" cy="${((i.y + i.h / 2) * k).toFixed(2)}" rx="${(i.w / 2 * k).toFixed(2)}" ry="${(i.h / 2 * k).toFixed(2)}" fill="${i.fill ? '#' + i.fill : 'none'}" stroke="#${i.stroke}" stroke-width="${(i.sw / 72 * k).toFixed(2)}"/>`);
      } else if (i.t === 'line') {
        o.push(`<line x1="${(i.x1 * k).toFixed(2)}" y1="${(i.y1 * k).toFixed(2)}" x2="${(i.x2 * k).toFixed(2)}" y2="${(i.y2 * k).toFixed(2)}" stroke="#${i.stroke}" stroke-width="${(i.sw / 72 * k).toFixed(2)}"${dash(i.dash)}${i.arrow ? ' marker-end="url(#ar)"' : ''}/>`);
      } else if (i.t === 'text') {
        const ls = i.lines || [i.str];
        const lh = lineH(i.size);
        const total = ls.length * lh;
        let top = i.valign === 'top' ? i.y : i.valign === 'bottom' ? i.y + i.h - total : i.y + (i.h - total) / 2;
        const anchor = i.align === 'left' ? 'start' : i.align === 'right' ? 'end' : 'middle';
        const tx = i.align === 'left' ? i.x + 0.05 : i.align === 'right' ? i.x + i.w - 0.05 : i.x + i.w / 2;
        ls.forEach((ln, n) => {
          const cy = top + lh * n + lh / 2;
          o.push(`<text x="${(tx * k).toFixed(2)}" y="${(cy * k).toFixed(2)}" font-family="${S.FONT}, 'Malgun Gothic', sans-serif" font-size="${(i.size / 72 * k).toFixed(2)}" font-weight="${i.bold ? 700 : 400}" fill="#${i.color}" text-anchor="${anchor}" dominant-baseline="central" xml:space="preserve">${esc(ln)}</text>`);
        });
      }
    }
    o.push('</svg>');
    return o.join('\n');
  }

  toPptx(pptx, slide) {
    for (const i of this.sorted()) {
      if (i.t === 'rect') {
        slide.addShape(pptx.ShapeType.rect, {
          x: i.x, y: i.y, w: i.w, h: i.h,
          fill: i.fill ? { color: i.fill } : { type: 'none' },
          line: { color: i.stroke, width: i.sw, dashType: i.dash === 'dash' ? 'dash' : 'solid' },
        });
      } else if (i.t === 'ellipse') {
        slide.addShape(pptx.ShapeType.ellipse, {
          x: i.x, y: i.y, w: i.w, h: i.h,
          fill: i.fill ? { color: i.fill } : { type: 'none' },
          line: { color: i.stroke, width: i.sw },
        });
      } else if (i.t === 'line') {
        const x = Math.min(i.x1, i.x2), y = Math.min(i.y1, i.y2);
        slide.addShape(pptx.ShapeType.line, {
          x, y, w: Math.abs(i.x2 - i.x1), h: Math.abs(i.y2 - i.y1),
          flipH: i.x2 < i.x1, flipV: i.y2 < i.y1,
          line: { color: i.stroke, width: i.sw, dashType: i.dash === 'dash' ? 'dash' : 'solid', endArrowType: i.arrow ? 'triangle' : 'none' },
        });
      } else if (i.t === 'text') {
        const body = (i.lines || [i.str]).join('\n');
        slide.addText(body, {
          x: i.x, y: i.y, w: i.w, h: i.h,
          fontFace: S.FONT, fontSize: i.size, bold: i.bold, color: i.color, lang: 'ko-KR',
          align: i.align, valign: i.valign, margin: 0, wrap: false, fit: 'none',
          lineSpacingMultiple: 1.0,
        });
      }
    }
  }
}

module.exports = { Canvas, F, wrap, lineH };
