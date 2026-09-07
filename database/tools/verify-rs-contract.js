#!/usr/bin/env node
// 기대값이 구현을 따라가지 않았는지 본다.
//
// expected-contracts.json 의 Result Set 컬럼 목록을 **기준선 05 의 표**와 직접 대조한다.
// 06 §44.8 이 등재한 결함 — SELECT_수검자상세 RS1 이 계약보다 한 컬럼 모자랐는데
// 기대값 파일도 같은 오류를 담고 있어 계약 110종이 전부 green 이었다 — 이 축을 아무도 보지 않았다.
// verify-contract.js 는 기대값과 실행결과를 맞출 뿐이라 둘이 같이 틀리면 통과한다.
'use strict';
const fs = require('fs');
const path = require('path');

const BASE05 = path.join(__dirname, '..', '..', 'docs', 'baseline', '05_DB_Rule_SP_Contract.md');
const EXPECTED = path.join(__dirname, 'expected-contracts.json');

const md = fs.readFileSync(BASE05, 'utf8');
const expected = JSON.parse(fs.readFileSync(EXPECTED, 'utf8'));

// 05 는 SP 절 안에 Result Set 표를 마크다운 표로 둔다. 표의 첫 컬럼이 `컬럼` 이거나
// `순서` + `컬럼` 인 두 형태가 있어 헤더에서 컬럼 이름 칸의 위치를 찾아 쓴다.
function tablesIn(src) {
  const out = [];
  const lines = src.split('\n');
  for (let i = 0; i < lines.length; i++) {
    if (!/^\|/.test(lines[i]) || !/^\|[\s:\-|]+\|$/.test(lines[i + 1] || '')) continue;
    const head = lines[i].split('|').slice(1, -1).map(s => s.trim());
    const col = head.findIndex(h => h === '컬럼');
    if (col < 0) { continue; }
    const rows = [];
    let j = i + 2;
    for (; j < lines.length && /^\|/.test(lines[j]); j++) {
      const c = lines[j].split('|').slice(1, -1).map(s => s.trim());
      const m = (c[col] || '').match(/^`([^`]+)`$/);
      if (m) rows.push(m[1]);
    }
    if (rows.length) out.push(rows);
    i = j;
  }
  return out;
}

// SP 절을 잘라낸다. 절 제목이 `[dbo].[USP_HC_…]` 를 담는다.
const sections = new Map();
{
  const re = /^#+ [^\n]*`\[dbo\]\.\[(USP_HC_[^\]]+)\]`[^\n]*$/gm;
  const marks = [...md.matchAll(re)].map(m => ({ sp: m[1], at: m.index, len: m[0].length }));
  marks.forEach((m, i) => {
    const end = i + 1 < marks.length ? marks[i + 1].at : md.length;
    sections.set(m.sp, md.slice(m.at + m.len, end));
  });
}

// RS0 은 §3.1 의 공통 표 하나다.
const RS0 = tablesIn(md.slice(md.indexOf('## 3.1'), md.indexOf('## 3.2')))[0];

let fail = 0;
const F = (m, d) => { fail++; console.log('FAIL ' + m); if (d) String(d).split('\n').forEach(l => console.log('        ' + l)); };

// 예약·접수 Write SP 의 성공 RS1 은 §11 머리의 공통 Schema 다 — 절 안에 표가 없다.
const WORK_RS1 = tablesIn(md.slice(md.indexOf('# 11. 예약 Write SP 계약'), md.indexOf('## 11.1')))[0];

let checked = 0;
const bySp = new Map();
for (const [name, sc] of Object.entries(expected)) {
  if (!bySp.has(sc.sp)) bySp.set(sc.sp, []);
  bySp.get(sc.sp).push([name, sc]);
}

for (const [sp, list] of bySp) {
  if (!/^USP_HC_/.test(sp)) continue;          // (fixture) 등 SP 아닌 항목
  const sec = sections.get(sp);
  if (!sec) { F('05 에 절이 없는 SP: ' + sp); continue; }
  const tabs = tablesIn(sec).filter(t => t.length && !t.every(c => /^@/.test(c)));
  const pool = new Set([RS0.join('|'), ...tabs.map(t => t.join('|')), WORK_RS1.join('|')]);

  for (const [name, sc] of list) {
    sc.resultSets.forEach((rs, i) => {
      checked++;
      const key = rs.columns.join('|');
      if (i === 0) {
        if (key !== RS0.join('|')) F(name + ' RS0 가 05 §3.1 과 다름', 'expected: ' + key + '\n05      : ' + RS0.join('|'));
        return;
      }
      if (!pool.has(key))
        F(name + ' RS' + i + ' 컬럼이 05 의 어느 표와도 일치하지 않음',
          'expected: ' + key + '\n05 후보 : ' + [...pool].slice(1).join('\n          '));
    });
  }
}

console.log((fail ? 'FAIL' : 'PASS') + ' RS-CONTRACT 기대값 Result Set ' + checked + '개를 기준선 05 표와 대조 — 불일치 ' + fail + '건');
process.exit(fail ? 1 : 0);
