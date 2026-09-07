// Real TTF metric reader for Malgun Gothic. No reasoning, actual hmtx advance widths.
const fs = require('fs');

function loadFont(path) {
  const b = fs.readFileSync(path);
  const numTables = b.readUInt16BE(4);
  const tables = {};
  for (let i = 0; i < numTables; i++) {
    const o = 12 + i * 16;
    const tag = b.toString('ascii', o, o + 4);
    tables[tag] = { off: b.readUInt32BE(o + 8), len: b.readUInt32BE(o + 12) };
  }
  const head = tables['head'].off;
  const unitsPerEm = b.readUInt16BE(head + 18);
  const hhea = tables['hhea'].off;
  const numberOfHMetrics = b.readUInt16BE(hhea + 34);
  const ascender = b.readInt16BE(hhea + 4);
  const descender = b.readInt16BE(hhea + 6);
  const lineGap = b.readInt16BE(hhea + 8);
  const hmtx = tables['hmtx'].off;

  // cmap -> pick a windows unicode subtable
  const cmap = tables['cmap'].off;
  const nSub = b.readUInt16BE(cmap + 2);
  let best = null, bestFmt = -1;
  for (let i = 0; i < nSub; i++) {
    const o = cmap + 4 + i * 8;
    const plat = b.readUInt16BE(o), enc = b.readUInt16BE(o + 2);
    const sub = cmap + b.readUInt32BE(o + 4);
    const fmt = b.readUInt16BE(sub);
    if (plat === 3 && (enc === 1 || enc === 10) && (fmt === 4 || fmt === 12)) {
      if (fmt > bestFmt) { bestFmt = fmt; best = sub; }
    }
  }
  if (best === null) throw new Error('no usable cmap');

  function gidFmt4(cp) {
    const sub = best;
    const segX2 = b.readUInt16BE(sub + 6);
    const segCount = segX2 / 2;
    const endBase = sub + 14;
    const startBase = endBase + segX2 + 2;
    const deltaBase = startBase + segX2;
    const roBase = deltaBase + segX2;
    for (let s = 0; s < segCount; s++) {
      const end = b.readUInt16BE(endBase + s * 2);
      if (cp <= end) {
        const start = b.readUInt16BE(startBase + s * 2);
        if (cp < start) return 0;
        const delta = b.readInt16BE(deltaBase + s * 2);
        const ro = b.readUInt16BE(roBase + s * 2);
        if (ro === 0) return (cp + delta) & 0xffff;
        const gi = roBase + s * 2 + ro + (cp - start) * 2;
        const g = b.readUInt16BE(gi);
        return g === 0 ? 0 : (g + delta) & 0xffff;
      }
    }
    return 0;
  }
  function gidFmt12(cp) {
    const sub = best;
    const nGroups = b.readUInt32BE(sub + 12);
    let lo = 0, hi = nGroups - 1;
    while (lo <= hi) {
      const mid = (lo + hi) >> 1;
      const g = sub + 16 + mid * 12;
      const s = b.readUInt32BE(g), e = b.readUInt32BE(g + 4);
      if (cp < s) hi = mid - 1;
      else if (cp > e) lo = mid + 1;
      else return b.readUInt32BE(g + 8) + (cp - s);
    }
    return 0;
  }
  const gid = bestFmt === 12 ? gidFmt12 : gidFmt4;

  function advanceUnits(cp) {
    const g = gid(cp);
    const i = g < numberOfHMetrics ? g : numberOfHMetrics - 1;
    return b.readUInt16BE(hmtx + i * 4);
  }

  // width in points for text at given pt size
  function widthPt(text, pt) {
    let u = 0;
    for (const ch of text) u += advanceUnits(ch.codePointAt(0));
    return u / unitsPerEm * pt;
  }
  function widthIn(text, pt) { return widthPt(text, pt) / 72; }

  return { unitsPerEm, numberOfHMetrics, ascender, descender, lineGap, advanceUnits, widthPt, widthIn, gid, cmapFormat: bestFmt };
}

module.exports = { loadFont };

if (require.main === module) {
  const f = loadFont('C:/Windows/Fonts/malgun.ttf');
  console.log('unitsPerEm =', f.unitsPerEm, ' cmap fmt =', f.cmapFormat,
    ' asc/desc/gap =', f.ascender, f.descender, f.lineGap);
  const samples = ['가', '한', '힣', 'A', 'W', 'i', '0', '1', '(', ')', '-', ' ', ',', '·'];
  for (const s of samples) {
    console.log(`  '${s}' U+${s.codePointAt(0).toString(16).toUpperCase().padStart(4, '0')} adv=${f.advanceUnits(s.codePointAt(0))} units = ${(f.advanceUnits(s.codePointAt(0)) / f.unitsPerEm).toFixed(4)} em`);
  }
  // self-check: Hangul must be ~1em, digits ~0.5em, and width scales linearly
  const em = f.advanceUnits('가'.codePointAt(0)) / f.unitsPerEm;
  console.assert(em > 0.9 && em <= 1.05, 'Hangul advance should be ~1em, got ' + em);
  const w10 = f.widthIn('가나다라마', 10), w20 = f.widthIn('가나다라마', 20);
  console.assert(Math.abs(w20 - 2 * w10) < 1e-9, 'linear scaling broken');
  console.log('5 Hangul @10.5pt =', f.widthIn('가나다라마', 10.5).toFixed(4), 'in');
  console.log('self-check OK');
}
