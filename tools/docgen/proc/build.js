'use strict';
// 01_검진_예약접수_업무프로세스.pptx 생성기 (단독 실행)
//   node tools/docgen/build_proc.js [출력pptx] [미리보기디렉터리]
// 원본(docs/baseline/01_Process_Definition.md)은 읽지도 쓰지도 않는다 — 내용은 슬라이드 모듈에 옮겨져 있다.
const fs = require('fs');
const path = require('path');
const S = require('./spec');
const { Canvas } = require('./canvas');
const flow = require('./flow');

function loadPptx() {
  for (const p of ['pptxgenjs', 'D:/tmp/hcwork/gen/node_modules/pptxgenjs']) {
    try { return require(p); } catch (e) { /* 다음 후보 */ }
  }
  throw new Error('pptxgenjs 모듈을 찾을 수 없습니다. `npm i pptxgenjs` 후 다시 실행하세요.');
}
const PptxGenJS = loadPptx();

const OUT = process.argv[2] || path.resolve(__dirname, '../../../docs/baseline/output/01_검진_예약접수_업무프로세스.pptx');
const PREV = process.argv[3] || 'D:/tmp/hcproc';

// argv[4] 로 일부 슬라이드만 미리보기 렌더할 수 있다 (작업 중 확인용).
const NAMES = (process.argv[4] || 's00,s01,s02,s03,s04,s05,s06,s07,s08,s09,s10').split(',');

fs.mkdirSync(PREV, { recursive: true });
fs.mkdirSync(path.dirname(OUT), { recursive: true });

const pptx = new PptxGenJS();
pptx.defineLayout({ name: 'W16', width: S.SLIDE.w, height: S.SLIDE.h });
pptx.layout = 'W16';

let warn = 0;
for (const name of NAMES) {
  const m = require(`./slides/${name}`);
  const c = new Canvas();
  flow.frame(c, m.title, m.caption);
  m.draw(c);

  // 슬라이드 밖 도형 자체 점검 (check.js 는 생성 후 OPC 레벨에서 다시 본다)
  for (const it of c.items) {
    const x = it.t === 'line' ? Math.min(it.x1, it.x2) : it.x;
    const y = it.t === 'line' ? Math.min(it.y1, it.y2) : it.y;
    const w = it.t === 'line' ? Math.abs(it.x2 - it.x1) : it.w;
    const h = it.t === 'line' ? Math.abs(it.y2 - it.y1) : it.h;
    if (x < 0 || y < 0 || x + w > S.SLIDE.w + 1e-6 || y + h > S.SLIDE.h + 1e-6) {
      c.warnings.push(`슬라이드 밖: ${it.t} (${x.toFixed(2)},${y.toFixed(2)}) ${w.toFixed(2)}x${h.toFixed(2)}`);
    }
  }

  const slide = pptx.addSlide();
  slide.background = { color: 'FFFFFF' };
  c.toPptx(pptx, slide);

  const svg = c.toSvg(1600);
  fs.writeFileSync(path.join(PREV, `${name}.svg`), svg, 'utf8');
  fs.writeFileSync(path.join(PREV, `${name}.html`),
    `<!doctype html><meta charset="utf-8"><style>html,body{margin:0;background:#fff}</style>${svg}`, 'utf8');
  console.log(`${name}: 도형 ${c.items.length}개  ${m.title}`);
  c.warnings.forEach(w => { warn++; console.log('  ! ' + w); });
}

pptx.writeFile({ fileName: OUT })
  .then(f => console.log(`\nPPTX: ${f}\n경고 ${warn}건`))
  .catch(e => { console.error(e); process.exit(1); });
