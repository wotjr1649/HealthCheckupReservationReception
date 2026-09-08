/*
 * scan_output.js — docs/baseline/output/ 여섯 종에서 텍스트를 전부 뽑아 훑는다.
 *
 * 산출물은 사내 공개용이다. 내부 통제 어휘(기준선·FINAL·Phase·PASS…)가 한 글자도
 * 새어 나가면 안 되고, 00~05 가 서로 다른 수치를 말해서도 안 된다.
 * 이 파일은 **탐색용** 이다 — 판정은 verify_output.js 가 한다.
 *
 * 실행: node tools/docgen/scan_output.js [정규식]
 */
'use strict';
const fs = require('fs');
const path = require('path');
const ExcelJS = require('D:/tmp/hcwork/gen/node_modules/exceljs');
const JSZip = require('D:/tmp/hcwork/gen/node_modules/jszip');

const OUT = 'D:/AIDEV/HealthCheckupReservationReception/docs/baseline/output';

async function textOf(file) {
  const full = path.join(OUT, file);
  const items = [];   // { where, text }
  if (/\.xlsx$/i.test(file)) {
    const wb = new ExcelJS.Workbook();
    await wb.xlsx.readFile(full);
    for (const ws of wb.worksheets) {
      ws.eachRow({ includeEmpty: false }, (row, r) => {
        row.eachCell({ includeEmpty: false }, (cell, c) => {
          const v = cell.value;
          const s = v && typeof v === 'object' && v.richText
            ? v.richText.map(t => t.text).join('')
            : (v == null ? '' : String(v));
          if (s.trim()) items.push({ where: ws.name + '!' + r + ',' + c, text: s });
        });
      });
    }
  } else {
    const z = await JSZip.loadAsync(fs.readFileSync(full));
    const names = Object.keys(z.files)
      .filter(n => /^ppt\/(slides|notesSlides)\/[^/]+\.xml$/.test(n))
      .sort((a, b) => (a.match(/\d+/) || [0])[0] - (b.match(/\d+/) || [0])[0]);
    for (const n of names) {
      const xml = await z.file(n).async('string');
      const slide = path.basename(n, '.xml');
      for (const m of xml.matchAll(/<a:t>([\s\S]*?)<\/a:t>/g)) {
        const s = m[1].replace(/&amp;/g, '&').replace(/&lt;/g, '<').replace(/&gt;/g, '>')
                      .replace(/&quot;/g, '"').replace(/&apos;/g, "'");
        if (s.trim()) items.push({ where: slide, text: s });
      }
    }
  }
  return items;
}

async function all() {
  // ~$ 로 시작하는 것은 Excel·PowerPoint 가 파일을 열어 둔 동안 만드는 잠금 파일이다. zip 이 아니다.
  const files = fs.readdirSync(OUT).filter(f => /\.(xlsx|pptx)$/i.test(f) && !/^~\$/.test(f)).sort();
  const out = {};
  for (const f of files) out[f] = await textOf(f);
  return out;
}

module.exports = { all, textOf, OUT };

if (require.main === module) {
  const re = new RegExp(process.argv[2] || 'FINAL|READ-ONLY|기준선|HC-RSV-RCP|변경 통제|최종 검수|최종 판정|'
    + 'Phase|후속 문서|후속 Phase|\\bPASS\\b|\\bFAIL\\b|NOT RUN|\\bSKIP\\b|회귀|게이트|커밋|'
    + '\\bv\\d+\\.\\d+|\\bR[234]\\b|§|실측|CANDIDATE|폐기|TODO|Seed/Test|과제|적대적|READONLY|GO\\b', 'i');
  all().then(map => {
    let n = 0;
    for (const [f, items] of Object.entries(map)) {
      const hits = items.filter(i => re.test(i.text));
      console.log('\n=== ' + f + '  (텍스트 ' + items.length + '조각 · 적중 ' + hits.length + ') ===');
      const seen = new Set();
      for (const h of hits) {
        const key = h.text.slice(0, 90);
        if (seen.has(key)) continue;
        seen.add(key); n++;
        console.log('  ' + h.where.padEnd(18) + ' ' + h.text.replace(/\s+/g, ' ').slice(0, 130));
      }
    }
    console.log('\n적중 고유 ' + n + '건');
  }).catch(e => { console.error(e); process.exit(1); });
}
