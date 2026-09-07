#!/usr/bin/env node
// R4 한글화 — 매핑 자체검증과 치환.
//
//   node tools/r4-rename.js check          매핑이 실제 소스를 전부 덮는지, 충돌이 없는지만 본다
//   node tools/r4-rename.js apply <파일…>   실제로 치환한다 (BOM 보존)
//
// 치환은 세 갈래를 각각 다른 패턴으로 노린다 — plans/11 §2.
// 단순 전역 치환을 쓰지 않는 이유는 R4 에서 Parameter·컬럼·별칭의 글자가 같아지기 때문이다.
'use strict';
const fs = require('fs');
const path = require('path');

const MAP = JSON.parse(fs.readFileSync(path.join(__dirname, 'r4-rename-map.json'), 'utf8'));
const ID = MAP.identifier;
const FORBIDDEN = new Set(MAP.forbidden);
const SP = MAP.sp;

// 긴 이름부터 치환해야 AexOpt01Selected 가 Aex 로 먼저 잘리지 않는다.
const idKeys = Object.keys(ID).sort((a, b) => b.length - a.length);
const spKeys = Object.keys(SP).sort((a, b) => b.length - a.length);

const DEPLOY = ['03_Functions', '04_Procedures_Select', '05_Procedures_Patient_Write',
                '06_Procedures_Reservation_Write', '07_Procedures_Reception_Write']
  .map(n => path.join(__dirname, '..', 'deploy', n + '.sql'));

const read = f => fs.readFileSync(f, 'utf8');

// ── check ────────────────────────────────────────────────────────────────────
function check() {
  let fail = 0;
  const F = (m, d) => { fail++; console.log('FAIL ' + m); if (d) String(d).split('\n').slice(0, 40).forEach(l => console.log('        ' + l)); };
  const P = m => console.log('PASS ' + m);

  // C1 배포 소스의 모든 @변수가 매핑되었는가.
  const seen = new Set();
  for (const f of DEPLOY)
    // 뒤에 한글이 붙으면 @B형간염제외여부 의 'B' 같은 조각이다 — 변수 이름이 아니다.
    for (const m of read(f).matchAll(/(?:^|[^@A-Za-z0-9_])@([A-Za-z][A-Za-z0-9_]*)(?![가-힣])/g)) seen.add(m[1]);
  const unmapped = [...seen].filter(v => !ID[v] && !FORBIDDEN.has(v)).sort();
  unmapped.length ? F('C1 매핑에 없는 @변수 ' + unmapped.length + '건', unmapped.join(' '))
                  : P('C1 배포 @변수 ' + seen.size + '종 전부 매핑 또는 금지목록에 있음');

  // C2 금지목록이 실제로 sp_getapplock 인자로만 쓰이는가.
  // 호출이 두 줄로 접혀 있고 주석이 이 이름들을 인용하므로 창을 두 줄 넓히고 주석줄은 뺀다.
  const bad = [];
  for (const f of DEPLOY) {
    const ls = read(f).split('\n');
    ls.forEach((l, i) => {
      if (/^\s*--/.test(l)) return;
      const win = ls.slice(Math.max(0, i - 2), i + 1).join('\n');
      for (const v of FORBIDDEN)
        if (new RegExp('@' + v + '\\b').test(l) && !/sp_getapplock|sp_releaseapplock/.test(win))
          bad.push(path.basename(f) + ':' + (i + 1) + ' @' + v);
    });
  }
  bad.length ? F('C2 금지 이름이 applock 호출 밖에서 쓰임 ' + bad.length + '건', bad.join('\n'))
             : P('C2 금지 이름 ' + FORBIDDEN.size + '종 전부 applock 호출 안에서만 쓰임');

  // C3 같은 객체 안에서 서로 다른 영문이 같은 한글로 합쳐지지 않는가.
  //     합쳐지면 두 변수가 한 변수가 되어 조용히 값을 덮어쓴다 — 실행으로는 안 보인다.
  const clash = [];
  for (const f of DEPLOY) {
    const src = read(f);
    const parts = src.split(/(?=^CREATE OR ALTER (?:PROCEDURE|FUNCTION))/m);
    for (const p of parts) {
      const nm = (p.match(/^CREATE OR ALTER (?:PROCEDURE|FUNCTION) \[dbo\]\.\[([^\]]+)\]/m) || [])[1];
      if (!nm) continue;
      const byKo = new Map();
      for (const m of p.matchAll(/(?:^|[^@A-Za-z0-9_])@([A-Za-z][A-Za-z0-9_]*)(?![가-힣])/g)) {
        const en = m[1];
        if (!ID[en]) continue;
        const ko = ID[en];
        if (!byKo.has(ko)) byKo.set(ko, new Set());
        byKo.get(ko).add(en);
      }
      for (const [ko, ens] of byKo) if (ens.size > 1) clash.push(nm + ': @' + [...ens].join(' + @') + ' -> @' + ko);
    }
  }
  clash.length ? F('C3 한 객체 안에서 두 변수가 한 이름으로 합쳐짐 ' + clash.length + '건', clash.join('\n'))
               : P('C3 객체별 @변수 이름 충돌 0건');

  // C4 한글 이름의 문자·길이 예산. sysname = 128 글자.
  const badKo = [];
  for (const [en, ko] of Object.entries(ID)) {
    if (ko.length > 128) badKo.push(en + ' -> ' + ko + ' (' + ko.length + '글자)');
    if (!/^[가-힣A-Za-z0-9]+$/.test(ko)) badKo.push(en + ' -> ' + ko + ' (허용 밖 문자)');
  }
  for (const ko of Object.values(SP)) if (!/^[가-힣A-Za-z0-9_]+$/.test(ko)) badKo.push(ko + ' (허용 밖 문자)');
  badKo.length ? F('C4 이름 예산·문자 위반 ' + badKo.length + '건', badKo.join('\n'))
               : P('C4 한글 이름 ' + Object.keys(ID).length + '종 전부 문자·길이 적합 (최장 '
                   + Math.max(...Object.values(ID).map(s => s.length)) + '글자)');

  // C5 한글 이름이 서로 겹치지 않는가 (전역).
  const rev = new Map();
  for (const [en, ko] of Object.entries(ID)) { if (!rev.has(ko)) rev.set(ko, []); rev.get(ko).push(en); }
  const dup = [...rev].filter(([, v]) => v.length > 1).map(([k, v]) => k + ' <- ' + v.join(', '));
  dup.length ? console.log('INFO C5 한글 이름을 공유하는 영문 ' + dup.length + '쌍 (C3 가 통과했으면 같은 개념이다)\n        ' + dup.join('\n        '))
             : P('C5 한글 이름 전역 중복 0건');

  // C6 SP 16개가 전부 매핑에 있는가.
  const spSeen = new Set();
  for (const f of DEPLOY) for (const m of read(f).matchAll(/USP_HC_[A-Za-z가-힣0-9_]+/g)) spSeen.add(m[0]);
  const spMissing = [...spSeen].filter(s => !SP[s] && !Object.values(SP).includes(s)).sort();
  spMissing.length ? F('C6 매핑에 없는 SP 이름 ' + spMissing.length + '건', spMissing.join('\n'))
                   : P('C6 배포가 참조하는 SP ' + spSeen.size + '종 전부 매핑에 있음');

  console.log('\n=== r4-rename check: FAIL ' + fail + ' ===');
  process.exit(fail ? 1 : 0);
}

// ── apply ────────────────────────────────────────────────────────────────────
// 정규식 여러 개로 형태를 좇지 않는다. 소스를 네 종류의 조각(주석·문자열·대괄호·코드)으로
// 자른 뒤 조각의 종류에 따라 다르게 다룬다. 대괄호 안은 이미 한글 물리 컬럼이므로 손대지 않고,
// 문자열은 값이지 식별자가 아니므로 통째로 일치할 때만 바꾼다 — plans/11 §2.
function segments(s) {
  const out = [];
  let i = 0, buf = '';
  const flush = () => { if (buf) { out.push({ t: 'code', v: buf }); buf = ''; } };
  while (i < s.length) {
    const c = s[i];
    if (c === '-' && s[i + 1] === '-') {                       // 줄 주석
      flush(); const e = s.indexOf('\n', i); const j = e < 0 ? s.length : e;
      out.push({ t: 'comment', v: s.slice(i, j) }); i = j;
    } else if (c === '/' && s[i + 1] === '*') {                // 블록 주석
      flush(); const e = s.indexOf('*/', i + 2); const j = e < 0 ? s.length : e + 2;
      out.push({ t: 'comment', v: s.slice(i, j) }); i = j;
    } else if (c === "'" || ((c === 'N' || c === 'n') && s[i + 1] === "'"
                             && !/[A-Za-z0-9_@]/.test(s[i - 1] || ' '))) {   // 문자열. N 접두사를 함께 먹는다
      flush(); if (c !== "'") { buf = ''; }                    // (N 은 아래에서 조각에 포함된다)
      const start = i; if (c !== "'") i++;
      let j = i + 1;
      while (j < s.length) { if (s[j] === "'") { if (s[j + 1] === "'") j += 2; else { j++; break; } } else j++; }
      out.push({ t: 'str', v: s.slice(start, j) }); i = j;
    } else if (c === '[') {                                    // 대괄호 식별자
      flush(); const e = s.indexOf(']', i); const j = e < 0 ? s.length : e + 1;
      out.push({ t: 'bracket', v: s.slice(i, j) }); i = j;
    } else { buf += c; i++; }
  }
  flush();
  return out;
}

// 낱말 단위로 훑는다. 앞 글자가 @ 면 변수, . 면 한정 컬럼, 그 밖이면 맨몸 식별자다.
// 낱말을 통째로 집으므로 AexOpt01Selected 가 Aex 로 잘리는 일이 원리적으로 없다.
const WORD = /([@.])?\b([A-Za-z][A-Za-z0-9_]*)\b/g;

// mode
//   sql    코드의 맨몸 식별자를 [한글] 로 만든다. 배포·테스트 .sql 이 이 모드다.
//   plain  SP 이름과 '문자열 전체가 매핑 이름인 것' 만 바꾼다. .js/.json/.sh 가 이 모드다 —
//          JS 지역변수나 정규식을 건드리면 게이트 자체가 깨진다.
function apply(files, mode) {
  for (const f of files) {
    const raw = read(f);
    const bom = raw.charCodeAt(0) === 0xFEFF;
    const src = bom ? raw.slice(1) : raw;
    const hit = { sp: 0, varr: 0, ident: 0, str: 0, comment: 0 };

    // md  ```sql 펜스 안은 sql 모드(대괄호), 그 밖은 산문이라 대괄호를 붙이지 않는다.
    //     V14 는 계획 md 의 **대괄호** 식별자만 보므로 이 경계가 게이트의 경계와 같다.
    if (mode === 'md') {
      const lines = src.split('\n');
      let inSql = false, inAny = false;
      const outL = lines.map(l => {
        const f = l.match(/^\s*```(\w*)/);
        if (f) { if (!inAny) { inAny = true; inSql = /^sql$/i.test(f[1]); } else { inAny = false; inSql = false; } return l; }
        let v = l;
        for (const k of spKeys) if (v.includes(k)) { hit.sp += v.split(k).length - 1; v = v.split(k).join(SP[k]); }
        return v.replace(WORD, (whole, pre, w) => {
          if (!ID[w]) return whole;
          if (pre === '@') { if (FORBIDDEN.has(w)) return whole; hit.varr++; return '@' + ID[w]; }
          if (w === 'Msg') return whole;
          hit.ident++;
          return (pre || '') + (inSql ? '[' + ID[w] + ']' : ID[w]);
        });
      });
      fs.writeFileSync(f, (bom ? '﻿' : '') + outL.join('\n'), 'utf8');
      console.log('applied ' + path.basename(f).padEnd(40) + ' SP ' + hit.sp + ' · @변수 ' + hit.varr + ' · 식별자 ' + hit.ident);
      continue;
    }

    if (mode === 'plain') {
      let v = src;
      for (const k of spKeys) if (v.includes(k)) { hit.sp += v.split(k).length - 1; v = v.split(k).join(SP[k]); }
      v = v.replace(/(['"`])([A-Za-z][A-Za-z0-9_]*)\1/g,
        (m, q, w) => { if (!ID[w]) return m; hit.str++; return q + ID[w] + q; });
      fs.writeFileSync(f, (bom ? '﻿' : '') + v, 'utf8');
      console.log('applied ' + path.basename(f).padEnd(34) + ' SP ' + hit.sp + ' · 리터럴 ' + hit.str);
      continue;
    }

    const out = segments(src).map(seg => {
      let v = seg.v;
      for (const k of spKeys) if (v.includes(k)) { hit.sp += v.split(k).length - 1; v = v.split(k).join(SP[k]); }
      if (seg.t === 'bracket') {          // [ExamCode] 처럼 이미 대괄호가 붙은 영문 계약 컬럼
        const m = v.match(/^\[([A-Za-z][A-Za-z0-9_]*)\]$/);
        if (m && ID[m[1]]) { hit.ident++; return '[' + ID[m[1]] + ']'; }
        return v;
      }

      // 문자열은 값이지 식별자가 아니다. 통째로 매핑 이름과 같을 때만 바꾼다.
      // [!] 두 글자 이하는 절대 바꾸지 않는다 — 'C' 는 자동 차트번호 접두사이고
      //     sys.objects.type 코드이기도 하다. 실제로 두 곳을 깨뜨렸다.
      if (seg.t === 'str') {
        const m = v.match(/^N?'(@?)((?:[^']|'')*)'$/);
        if (m && ID[m[2]] && m[2].length >= 3) { hit.str++; return "N'" + m[1] + ID[m[2]] + "'"; }
        return v;
      }

      return v.replace(WORD, (whole, pre, w) => {
        if (pre === '@') { if (FORBIDDEN.has(w) || !ID[w]) return whole; hit.varr++; return '@' + ID[w]; }
        if (!ID[w]) return whole;
        // 주석에서 Msg 는 SQL Server 오류 표기(Msg 1934)이지 RS0 컬럼이 아니다 — 실제로 41곳을 깨뜨렸다.
        if (seg.t === 'comment') {
          if (w === 'Msg') return whole;
          hit.comment++; return (pre || '') + ID[w];   // 산문은 대괄호 없이
        }
        hit.ident++;
        return (pre || '') + '[' + ID[w] + ']';
      });
    }).join('');

    fs.writeFileSync(f, (bom ? '﻿' : '') + out, 'utf8');
    console.log('applied ' + path.basename(f).padEnd(34)
      + ' SP ' + hit.sp + ' · @변수 ' + hit.varr + ' · 식별자 ' + hit.ident
      + ' · 리터럴 ' + hit.str + ' · 주석 ' + hit.comment);
  }
}

const [, , cmd, ...rest] = process.argv;
if (cmd === 'check') check();
else if (cmd === 'apply') apply(rest.length ? rest : DEPLOY, 'sql');
else if (cmd === 'apply-plain') apply(rest, 'plain');
else if (cmd === 'apply-md') apply(rest, 'md');
else { console.log('usage: r4-rename.js check | apply [files…] | apply-plain <files…>'); process.exit(2); }
