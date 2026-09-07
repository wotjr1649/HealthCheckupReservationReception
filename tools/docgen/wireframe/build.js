'use strict';
const fs = require('fs');
const path = require('path');
const PptxGenJS = require('pptxgenjs');
const S = require('./spec');
const { Canvas } = require('./canvas');
const { drawPage } = require('./page');

const OUT = process.argv[2] || 'D:/tmp/hcwork/preview';
const names = (process.argv[3] || 'wf_rsv_01').split(',');
const FILE = process.argv[4] || '03_preview.pptx';
const PREVIEW = process.env.WF_PREVIEW || 'D:/tmp/hcwork/preview';

fs.mkdirSync(OUT, { recursive: true });

const pptx = new PptxGenJS();
pptx.defineLayout({ name: 'W16', width: S.SLIDE.w, height: S.SLIDE.h });
pptx.layout = 'W16';
pptx.author = '검진 예약·접수 관리 프로그램';
pptx.title = '검진 예약·접수 화면설계서';

let n = 0;
const emit = (key, page) => {
  const c = new Canvas();
  page.draw(c);
  let info = { descHeight: 0 };
  if (!page.fullWidth) info = drawPage(c, { title: page.title, desc: page.desc });

  const slide = pptx.addSlide();
  slide.background = { color: 'FFFFFF' };
  c.toPptx(pptx, slide);

  // 미리보기(SVG/HTML)는 산출물 폴더를 더럽히지 않도록 항상 별도 경로에 쓴다.
  // Chrome 헤드리스로 PNG를 떠서 눈으로 확인하는 용도.
  fs.mkdirSync(PREVIEW, { recursive: true });
  const svg = c.toSvg(1600);
  fs.writeFileSync(path.join(PREVIEW, `${key}.svg`), svg, 'utf8');
  fs.writeFileSync(path.join(PREVIEW, `${key}.html`),
    `<!doctype html><meta charset="utf-8"><style>html,body{margin:0;background:#fff}</style>${svg}`, 'utf8');
  n++;
  console.log(`${String(n).padStart(2)}. ${key}  도형 ${c.items.length}` + (page.fullWidth ? '' : ` / 설명표 ${info.descHeight.toFixed(2)}in`));
  if (c.warnings.length) c.warnings.forEach(w => console.log('     ! ' + w));
};

for (const name of names) {
  const m = require(`./screens/${name}`);
  if (Array.isArray(m.pages)) m.pages.forEach((p, i) => emit(m.pages.length > 1 ? `${name}_${i + 1}` : name, p));
  else emit(name, m);
}

pptx.writeFile({ fileName: path.join(OUT, FILE) })
  .then(f => console.log(`\n${n}장 → ${f}`))
  .catch(e => { console.error(e); process.exit(1); });
