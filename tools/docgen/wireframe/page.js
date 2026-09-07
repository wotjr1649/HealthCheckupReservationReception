'use strict';
const S = require('./spec');
const { Canvas, F, wrap, lineH } = require('./canvas');

// 제목 + 우측 설명표. 표는 addTable을 쓰지 않고 사각형+텍스트로 직접 그린다 —
// pptxgenjs의 addTable은 행 높이를 계산하지 않아(<a:tr h="0">) 넘침이 생성 시점에 보이지 않는다.
function drawPage(c, { title, desc }) {
  c.text(S.PAGE.title.x, S.PAGE.title.y, S.PAGE.title.w, S.PAGE.title.h, title, {
    size: S.PAGE.title.size, bold: true, color: S.PAGE.title.color, align: 'left',
  });

  const d = S.PAGE.desc;
  const pad = S.DESC_RULE.pad;
  const textW = d.wText - pad * 2;
  const lh = lineH(S.TEXT.desc);

  const rows = desc.map(r => {
    const lines = wrap(r.text, S.TEXT.desc, textW);
    return { n: r.n ?? '', lines, h: Math.max(lines.length * lh + pad * 2, 0.30) };
  });

  const total = rows.reduce((s, r) => s + r.h, 0);
  if (total > d.maxH) {
    throw new Error(`설명표 넘침: ${total.toFixed(2)}in > ${d.maxH}in (항목 ${rows.length}개). ` +
      `항목당 ${S.DESC_RULE.maxChars}자 이내로 줄이거나 콜아웃을 ${S.DESC_RULE.maxCallouts}개 이하로 묶을 것.`);
  }
  const over = desc.filter(r => r.text.length > S.DESC_RULE.maxChars);
  if (over.length) c.warnings.push(`설명 ${S.DESC_RULE.maxChars}자 초과 ${over.length}건: ` + over.map(r => r.n).join(','));

  let y = d.y;
  for (const r of rows) {
    c.rect(d.x, y, d.wNum, r.h, { stroke: S.C.data, sw: S.W.hair });
    c.rect(d.x + d.wNum, y, d.wText, r.h, { stroke: S.C.data, sw: S.W.hair });
    if (r.n !== '') c.text(d.x, y, d.wNum, r.h, r.n, { size: S.TEXT.descNum, color: S.C.data });
    c.text(d.x + d.wNum + pad, y + pad, textW, r.h - pad * 2, '', {
      size: S.TEXT.desc, color: S.C.data, align: 'left', valign: 'top', lines: r.lines,
    });
    y += r.h;
  }
  return { descBottom: y, descHeight: total };
}

module.exports = { drawPage };
