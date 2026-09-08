/*
 * strip_refs.js — 생성기의 **문자열 리터럴 안에 있는** § 절 참조를 걷어낸다.
 *
 * 산출물은 사내 공개용이고 독자는 docs/baseline/*.md 를 갖고 있지 않다.
 * `(§5.2, §23.4)` 같은 참조는 따라갈 수 없는 표시라 읽는 사람에게 잡음이다.
 *
 * [!] 주석 안의 § 는 건드리지 않는다 — 그것은 코드를 읽는 사람에게 필요한 근거다.
 *     그래서 단순 치환을 쓰지 않고 문자열/주석을 갈라 문자열 조각에만 규칙을 적용한다.
 *
 *   node tools/docgen/strip_refs.js            무엇이 바뀌는지만 보여준다 (dry-run)
 *   node tools/docgen/strip_refs.js --write     실제로 고친다
 */
'use strict';
const fs = require('fs');
const path = require('path');

const ROOT = __dirname;
const TARGETS = []
  .concat(fs.readdirSync(path.join(ROOT, 'wireframe', 'screens')).map(f => 'wireframe/screens/' + f))
  .concat(fs.readdirSync(path.join(ROOT, 'proc', 'slides')).map(f => 'proc/slides/' + f))
  .filter(f => /\.js$/.test(f));

/*
 * [X] xlsx/ 생성기는 대상이 아니다. 두 가지 이유가 있고 둘 다 실측했다.
 *   1. 그 파일들의 문자열에는 산출물 텍스트와 **개발자용 콘솔 라벨**이 섞여 있다.
 *      구조로 구분할 수 없어 emit() 의 검사 이름까지 지웠다 ('컬럼 전건 (04  실측)').
 *   2. 아래 segments() 는 **정규식 리터럴을 모른다.** /'/g 같은 것이 나오면 상태가
 *      어긋나 뒤따르는 주석을 문자열로 본다 — build_02.js 의 주석 한 줄이 실제로 그렇게 바뀌었다.
 * 그래서 xlsx/ 의 § 는 손으로 고친다. 개수가 10 남짓이라 도구를 더 똑똑하게 만드는 것보다 싸다.
 */

/* JS 소스를 코드/주석/문자열 조각으로 가른다. */
function segments(src) {
  const out = [];
  let i = 0, buf = '';
  const flush = () => { if (buf) { out.push({ t: 'code', v: buf }); buf = ''; } };
  while (i < src.length) {
    const c = src[i], d = src[i + 1];
    if (c === '/' && d === '/') {
      flush(); const e = src.indexOf('\n', i); const j = e < 0 ? src.length : e;
      out.push({ t: 'comment', v: src.slice(i, j) }); i = j; continue;
    }
    if (c === '/' && d === '*') {
      flush(); const e = src.indexOf('*/', i + 2); const j = e < 0 ? src.length : e + 2;
      out.push({ t: 'comment', v: src.slice(i, j) }); i = j; continue;
    }
    if (c === "'" || c === '"' || c === '`') {
      flush(); let j = i + 1;
      while (j < src.length) {
        if (src[j] === '\\') { j += 2; continue; }
        if (src[j] === c) { j++; break; }
        j++;
      }
      out.push({ t: 'str', v: src.slice(i, j) }); i = j; continue;
    }
    buf += c; i++;
  }
  flush();
  return out;
}

const REF = '§\\s*\\d+(?:\\.\\d+[a-z]?)*';
const reOnlyRefs = new RegExp('\\s*\\((?:' + REF + ')(?:\\s*[,·]\\s*(?:' + REF + '))*\\)', 'g');
const reAnyRef = new RegExp(REF);
const reAllRefs = new RegExp(REF, 'g');

/*
 * [X] 공백을 일괄 정리하지 않는다. 초판이 그렇게 했다가 'C# WinForms · .NET' 을
 *     '·.NET' 으로 만들었고, PPTX 정렬용으로 일부러 넣은 여러 칸 공백까지 뭉갰다.
 *     지우는 자리에서만 앞 공백을 함께 먹는다.
 * [!] 문장 한가운데 홀로 있는 참조는 자동으로 지우지 않는다 — '§9.6과 같다' 를 지우면
 *     '과 같다' 가 되어 말이 깨진다. 손으로 고치도록 목록만 낸다.
 */
function strip(v) {
  let s = v.replace(reOnlyRefs, '');                       // (1) 괄호가 참조뿐이면 괄호째
  s = s.replace(/\(([^()]*)\)/g, (m, inner) => {           // (2) 다른 근거가 섞였으면 § 만
    if (!reAnyRef.test(inner)) return m;
    const t = inner.replace(reAllRefs, '')
      .replace(/^[\s,·]+|[\s,·]+$/g, '')
      .replace(/[\s,·]*,[\s,·]*/g, ', ');
    return t ? '(' + t + ')' : '';
  });
  return s;
}

const write = process.argv.includes('--write');
const MANUAL = [];
let files = 0, hits = 0;

for (const rel of TARGETS) {
  const p = path.join(ROOT, rel);
  const src = fs.readFileSync(p, 'utf8');
  if (src.indexOf('§') < 0) continue;

  let changed = 0;
  const out = segments(src).map(seg => {
    if (seg.t !== 'str' || seg.v.indexOf('§') < 0) return seg.v;
    const after = strip(seg.v);
    if ((after.match(reAllRefs) || []).length)
      MANUAL.push(rel + '\n      ' + after.replace(/\s+/g, ' ').slice(0, 150));
    if (after !== seg.v) {
      changed++;
      console.log('  ' + rel);
      console.log('    - ' + seg.v.replace(/\s+/g, ' ').slice(0, 116));
      console.log('    + ' + after.replace(/\s+/g, ' ').slice(0, 116));
    }
    return after;
  }).join('');

  if (!changed) continue;
  files++; hits += changed;
  if (write) fs.writeFileSync(p, out, 'utf8');
}

console.log('\n' + (write ? '고침' : 'dry-run') + ': 파일 ' + files + ' · 문자열 ' + hits);
if (MANUAL.length) {
  console.log('\n손으로 고쳐야 하는 것 ' + MANUAL.length + '건 — 문장 안에 참조가 박혀 있다');
  MANUAL.forEach(m => console.log('  ' + m));
}
if (!write) console.log('\n실제로 고치려면 --write');
