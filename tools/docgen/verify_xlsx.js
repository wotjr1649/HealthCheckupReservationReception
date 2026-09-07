'use strict';
// 산출물 xlsx 독립 검증 — 생성 스크립트와 다른 경로(JSZip 직독)로 확인한다.
const fs = require('fs');
const JSZip = require('jszip');

const BAN = ['FINAL', 'READ-ONLY', '기준선', 'HC-RSV-RCP', '변경 통제', '최종 검수', '최종 판정: GO',
  '적대적 검수', '종결된 결함', '과제', 'Seed', 'Test Data', 'v1.2', '13종', '후속 Phase', '기준본'];

const dec = s => s.replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"')
  .replace(/&apos;/g, "'").replace(/&#(\d+);/g, (m, d) => String.fromCharCode(+d)).replace(/&amp;/g, '&');
const P = '(?:[a-zA-Z0-9]+:)?';
const rx = (s, f) => new RegExp(s.split('@').join(P), f);

(async () => {
  for (const file of process.argv.slice(2)) {
    const zip = await JSZip.loadAsync(fs.readFileSync(file));
    const rd = async p => (zip.file(p) ? zip.file(p).async('string') : null);

    const wb = await rd('xl/workbook.xml');
    const rels = await rd('xl/_rels/workbook.xml.rels');
    const relMap = {};
    for (const m of rels.matchAll(/<Relationship\b[^>]*?\/>/g)) {
      const id = m[0].match(/Id="([^"]+)"/), t = m[0].match(/Target="([^"]+)"/);
      if (id && t) relMap[id[1]] = t[1].replace(/^\/?xl\//, '').replace(/^\//, '');
    }
    const sheets = [...wb.matchAll(rx('<@sheet ([^>]*?)/>', 'g'))].map(m => ({
      name: dec(m[1].match(/name="([^"]+)"/)[1]),
      file: relMap[m[1].match(/r:id="([^"]+)"/)[1]],
    }));

    const ssRaw = await rd('xl/sharedStrings.xml');
    const shared = ssRaw ? [...ssRaw.matchAll(rx('<@si>([^]*?)</@si>', 'g'))]
      .map(m => [...m[1].matchAll(rx('<@t[^>]*>([^]*?)</@t>', 'g'))].map(t => dec(t[1])).join('')) : [];

    console.log('\n' + '='.repeat(70) + '\n' + file.split(/[\\/]/).pop());
    let allText = '';
    for (const s of sheets) {
      const xml = await rd('xl/' + s.file);
      if (!xml) { console.log(`  ${s.name}: MISSING`); continue; }
      const rows = [...xml.matchAll(rx('<@row[^>]*>([^]*?)</@row>', 'g'))];
      let cells = 0;
      for (const r of rows) {
        for (const cm of r[1].matchAll(rx('<@c ([^>]*?)(?:/>|>([^]*?)</@c>)', 'g'))) {
          const a = cm[1], inner = cm[2] || '';
          let v = '';
          if (/t="s"/.test(a)) { const m = inner.match(rx('<@v>(\\d+)</@v>')); v = m ? shared[+m[1]] : ''; }
          else if (/t="inlineStr"/.test(a)) v = [...inner.matchAll(rx('<@t[^>]*>([^]*?)</@t>', 'g'))].map(t => dec(t[1])).join('');
          else { const m = inner.match(rx('<@v>([^]*?)</@v>')); v = m ? dec(m[1]) : ''; }
          if (v !== '') { cells++; allText += v + '\n'; }
        }
      }
      const pane = /<@pane\b/.test(xml.replace(/x:/g, '')) || /<pane\b/.test(xml);
      const af = /autoFilter/.test(xml);
      console.log(`  ${s.name.padEnd(18)} 행 ${String(rows.length).padStart(3)} / 값셀 ${String(cells).padStart(4)} / 틀고정 ${pane ? 'O' : 'X'} / 필터 ${af ? 'O' : 'X'}`);
    }
    const hits = BAN.filter(b => allText.includes(b));
    console.log('  금지 문자열: ' + (hits.length ? '⚠ ' + hits.join(', ') : '0건'));
    // 규칙 ID 커버리지
    const ids = [];
    for (const [p, n] of [['CP', 6], ['EP', 10], ['RP', 10], ['RCP', 6], ['TGT', 5], ['NEX', 7], ['AEX', 5], ['HOL', 5]])
      for (let i = 1; i <= n; i++) ids.push(`${p}-${String(i).padStart(2, '0')}`);
    const miss = ids.filter(id => !allText.includes(id));
    if (/업무정책/.test(file)) console.log(`  규칙 ID ${ids.length - miss.length}/${ids.length}` + (miss.length ? ' 누락: ' + miss.join(',') : ' 전부 존재'));
  }
})();
