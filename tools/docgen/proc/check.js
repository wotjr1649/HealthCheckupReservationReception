'use strict';
// 생성된 PPTX가 PowerPoint 복구 대화상자 없이 열릴 조건을 검사한다.
// (실제 PowerPoint 실행은 이 환경에서 차단되어 있어 OPC 패키지 수준으로 검증한다.)
const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const pptx = process.argv[2];
const dir = process.argv[3] || 'D:/tmp/hcwork/opccheck';
// 반드시 비우고 푼다. unzip -o 는 덮어쓰기만 하므로 이전 실행에서 남은 슬라이드가
// 그대로 남아 "없는 슬라이드가 통과했다"는 결과를 만든다.
fs.rmSync(dir, { recursive: true, force: true });
fs.mkdirSync(dir, { recursive: true });
execFileSync('unzip', ['-qq', '-o', pptx, '-d', dir], { stdio: 'pipe' });

const files = [];
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p); else files.push(p.replace(/\\/g, '/').slice(dir.length));
  }
})(dir);

const problems = [];

// 1) [Content_Types].xml 이 모든 파트를 덮는가
const ct = fs.readFileSync(path.join(dir, '[Content_Types].xml'), 'utf8');
const defaults = new Set([...ct.matchAll(/Extension="([^"]+)"/g)].map(m => m[1].toLowerCase()));
const overrides = new Set([...ct.matchAll(/PartName="([^"]+)"/g)].map(m => m[1]));
for (const f of files) {
  if (f === '/[Content_Types].xml') continue;
  // path.extname('/_rels/.rels') === '' 이므로 파일명 기준으로 직접 뽑는다
  const base = path.posix.basename(f);
  const ext = base.includes('.') ? base.slice(base.lastIndexOf('.') + 1).toLowerCase() : '';
  if (!overrides.has(f) && !defaults.has(ext)) problems.push(`Content_Types 누락: ${f}`);
}

// 2) 관계 대상이 실제로 존재하는가
let relCount = 0;
for (const f of files.filter(f => f.endsWith('.rels'))) {
  const base = path.posix.dirname(path.posix.dirname(f));
  const xml = fs.readFileSync(path.join(dir, f), 'utf8');
  for (const m of xml.matchAll(/<Relationship\b[^>]*>/g)) {
    const t = m[0].match(/Target="([^"]+)"/), ext = m[0].match(/TargetMode="External"/);
    if (!t || ext) continue;
    relCount++;
    const target = t[1].startsWith('/') ? t[1] : path.posix.normalize(path.posix.join(base, t[1]));
    if (!files.includes(target)) problems.push(`dangling rel: ${f} -> ${t[1]}`);
  }
}

// 3) 모든 XML 파트가 well-formed 인가 (태그 균형 + 미이스케이프 &)
let xmlCount = 0;
for (const f of files.filter(f => /\.(xml|rels)$/.test(f))) {
  xmlCount++;
  const s = fs.readFileSync(path.join(dir, f), 'utf8');
  if (/&(?!(amp|lt|gt|quot|apos|#[0-9]+|#x[0-9a-fA-F]+);)/.test(s)) problems.push(`미이스케이프 & : ${f}`);
  const stack = [];
  for (const m of s.matchAll(/<(\/?)([A-Za-z_][\w.:-]*)([^>]*?)(\/?)>/g)) {
    if (m[2] === '?xml' || m[0].startsWith('<!')) continue;
    if (m[1] === '/') { if (stack.pop() !== m[2]) { problems.push(`태그 불균형: ${f} @${m[2]}`); break; } }
    else if (m[4] !== '/') stack.push(m[2]);
  }
  if (stack.length) problems.push(`닫히지 않은 태그 ${stack.length}개: ${f}`);
}

// 4) 슬라이드별 도형 좌표가 슬라이드 밖으로 나가는가
const EMU = 914400;
const pres = fs.readFileSync(path.join(dir, '/ppt/presentation.xml'), 'utf8');
const sz = pres.match(/<p:sldSz[^>]*cx="(\d+)"[^>]*cy="(\d+)"/);
const SW = +sz[1], SH = +sz[2];
for (const f of files.filter(f => /^\/ppt\/slides\/slide\d+\.xml$/.test(f))) {
  const s = fs.readFileSync(path.join(dir, f), 'utf8');
  let n = 0, out = 0;
  const offs = [...s.matchAll(/<a:off x="(-?\d+)" y="(-?\d+)"\s*\/>\s*<a:ext cx="(\d+)" cy="(\d+)"\s*\/>/g)];
  for (const m of offs) {
    n++;
    const x = +m[1], y = +m[2], w = +m[3], h = +m[4];
    if (x < 0 || y < 0 || x + w > SW + 1 || y + h > SH + 1) {
      out++;
      problems.push(`슬라이드 밖 도형: ${f} (${(x / EMU).toFixed(2)},${(y / EMU).toFixed(2)}) ${(w / EMU).toFixed(2)}x${(h / EMU).toFixed(2)}`);
    }
  }
  console.log(`${f}: 도형 ${n}개, 슬라이드 밖 ${out}개`);
}

console.log(`\n파트 ${files.length} / XML ${xmlCount} / 관계 ${relCount}`);
console.log(problems.length ? '문제 ' + problems.length + '건:\n  ' + problems.slice(0, 20).join('\n  ') : 'CLEAN — 복구 경고 없이 열릴 조건 충족');
process.exit(problems.length ? 1 : 0);
