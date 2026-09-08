/*
 * verify-holiday-seed.js — 공휴일 Seed 의 만료와 사본 일치를 판정한다 (00 HOL-06 · 06 §14).
 *
 * 이 게이트가 있는 이유는 하나다. **공휴일 Seed 는 조용히 만료된다.**
 * 등재 최종일을 넘어가면 그 뒤의 공휴일이 전부 업무 가능일로 판정되고, 아무것도 실패하지 않는다.
 * 예약이 잡히고 접수가 되고 나서야 사람이 알아챈다.
 *
 * `00` §7.4 가 등재 범위와 경고 임계의 **단일 출처**다. 그 값을 세 곳이 비추고 있으므로
 * (Seed · SP 의 @경고임계일수 · 06 §14 표) 넷이 서로 맞는지도 함께 본다 (ROOT AGENTS.md §6).
 *
 * [!] 임계 미만이면 **FAIL 이다.** 00 HOL-06 은 "경고한다" 고 적지만, 이 저장소에서
 *     실패시키지 않는 검사는 초록인 채 아무것도 검증하지 않다가 잊힌다 (06 §44.8).
 *     잔여 180일은 사람이 Seed 를 갱신하기 넉넉한 창이고, 그 창을 넘겼다면 회귀가 멈추는 편이 낫다.
 *
 *   node tools/verify-holiday-seed.js
 */
'use strict';
const fs = require('fs');
const path = require('path');

const DB = path.resolve(__dirname, '..');
const ROOT = path.resolve(DB, '..');
const read = p => fs.readFileSync(p, 'utf8').replace(/^﻿/, '');

let pass = 0, fail = 0;
const P = (id, m) => { pass++; console.log('PASS ' + id + ' ' + m); };
const F = (id, m, d) => { fail++; console.log('FAIL ' + id + ' ' + m); if (d) console.log('        ' + String(d).split('\n').join('\n        ')); };

const POLICY = path.join(ROOT, 'docs/baseline/00_Project_Policy.md');
const SPEC   = path.join(ROOT, 'docs/baseline/06_DB_Transaction_Security_Seed.md');
const SEED   = path.join(DB, 'deploy/02_Seed.sql');
const PROC   = path.join(DB, 'deploy/07a_Procedures_Holiday.sql');

/* ---------------------------------------------------------------- HOL-G1
 * 00 §7.4 의 단일 출처를 읽는다. 못 읽으면 나머지를 판정할 근거가 없다 —
 * 미실행은 PASS 가 아니므로 여기서 끝낸다.
 */
const policy = read(POLICY);
const mRange = /공휴일 Seed 등재 범위\s*=\s*(\d{4}-\d{2}-\d{2})\s*~\s*(\d{4}-\d{2}-\d{2})/.exec(policy);
const mThr   = /갱신 경고 임계\s*=\s*잔여\s*(\d+)\s*일/.exec(policy);
if (!mRange || !mThr) {
  F('HOL-G1', '00 §7.4 에서 등재 범위·경고 임계를 읽지 못했다 - 미실행은 PASS 가 아니다',
    '기대 형식:\n  공휴일 Seed 등재 범위 = YYYY-MM-DD ~ YYYY-MM-DD\n  갱신 경고 임계 = 잔여 N일');
  console.log('\n=== verify-holiday-seed: PASS ' + pass + ' / FAIL ' + fail + ' ===');
  process.exit(1);
}
const [, rangeFrom, rangeTo] = mRange;
const threshold = Number(mThr[1]);
P('HOL-G1', '00 §7.4 단일 출처: 등재 범위 ' + rangeFrom + ' ~ ' + rangeTo + ' · 경고 임계 잔여 ' + threshold + '일');

/* ---------------------------------------------------------------- HOL-G2
 * Seed 실측. deploy/02_Seed.sql 의 [휴무일] INSERT 행을 그대로 읽는다.
 */
const seed = read(SEED);
const block = seed.split(/INSERT INTO \[dbo\]\.\[휴무일\]/)[1] || '';
const rows = [...block.split(/;\s*\r?\nGO/)[0].matchAll(
  /\('(\d{4}-\d{2}-\d{2})',\s*N'([^']*)',\s*N'([^']*)',\s*([01]),\s*(NULL|N'[^']*')\s*\)/g)]
  .map(m => ({ date: m[1], name: m[2], type: m[3], active: m[4] }));

if (!rows.length) {
  F('HOL-G2', 'deploy/02_Seed.sql 에서 휴무일 Seed 행을 하나도 읽지 못했다 - 미실행은 PASS 가 아니다');
} else {
  const dup = rows.map(r => r.date).filter((d, i, a) => a.indexOf(d) !== i);
  const TYPES = ['법정공휴일', '대체공휴일', '자체휴무일'];
  const badType = rows.filter(r => !TYPES.includes(r.type));
  const outside = rows.filter(r => r.date < rangeFrom || r.date > rangeTo);
  // 일요일은 Seed 하지 않는다 (06 §14.2). 요일 판정은 UTC 기준으로 고정한다.
  const sunday = rows.filter(r => new Date(r.date + 'T00:00:00Z').getUTCDay() === 0);
  const bad = [];
  if (dup.length)     bad.push('중복 날짜 ' + [...new Set(dup)].join(', '));
  if (badType.length) bad.push('휴무구분 도메인 밖 ' + badType.map(r => r.date + ' ' + r.type).join(', '));
  if (outside.length) bad.push('00 §7.4 등재 범위 밖 ' + outside.map(r => r.date).join(', '));
  if (sunday.length)  bad.push('일요일은 Seed 하지 않는다 (06 §14.2) ' + sunday.map(r => r.date).join(', '));
  bad.length ? F('HOL-G2', 'Seed 행 구성 오류 ' + bad.length + '건', bad.join('\n'))
             : P('HOL-G2', 'Seed ' + rows.length + '행 - 날짜 중복 0 · 구분 도메인 일치 · 범위 안 · 일요일 0');
}

/* ---------------------------------------------------------------- HOL-G3
 * 06 §14 표와 Seed 가 같은 날짜 집합인가. 문서만 고치고 SQL 을 잊는 드리프트를 잡는다.
 */
{
  const spec = read(SPEC);
  const sec = spec.split(/^# 14\. /m)[1] || '';
  const docRows = [...sec.split(/^# 15\./m)[0].matchAll(/^\| `(\d{4}-\d{2}-\d{2})` \| [^|]+ \| ([^|]+?) \| ([^|]+?) \|/gm)]
    .map(m => ({ date: m[1], name: m[2].trim(), type: m[3].trim() }));
  if (!docRows.length) {
    F('HOL-G3', '06 §14 에서 휴무일 표를 읽지 못했다 - 미실행은 PASS 가 아니다');
  } else {
    const sd = new Set(rows.map(r => r.date)), dd = new Set(docRows.map(r => r.date));
    const onlySeed = [...sd].filter(d => !dd.has(d));
    const onlyDoc  = [...dd].filter(d => !sd.has(d));
    // 이름·구분까지 본다. 날짜만 맞고 구분이 어긋나면 화면 편집 가능 여부가 달라진다.
    const byDate = Object.fromEntries(docRows.map(r => [r.date, r]));
    const mismatch = rows.filter(r => byDate[r.date] &&
      (byDate[r.date].type !== r.type || byDate[r.date].name !== r.name))
      .map(r => r.date + ' Seed=' + r.name + '/' + r.type +
                ' 06 §14=' + byDate[r.date].name + '/' + byDate[r.date].type);
    const bad = [];
    if (onlySeed.length) bad.push('Seed 에만 있음: ' + onlySeed.join(', '));
    if (onlyDoc.length)  bad.push('06 §14 에만 있음: ' + onlyDoc.join(', '));
    if (mismatch.length) bad.push('이름·구분 불일치:\n  ' + mismatch.join('\n  '));
    bad.length ? F('HOL-G3', '06 §14 표와 Seed 가 어긋난다', bad.join('\n'))
               : P('HOL-G3', '06 §14 표 ' + docRows.length + '행 = Seed ' + rows.length + '행 (날짜·이름·구분 전건)');
  }
}

/* ---------------------------------------------------------------- HOL-G4
 * SP 가 들고 있는 @경고임계일수 사본이 00 §7.4 와 같은가.
 * 화면은 이 값을 SP 에서 받아 쓰므로, 사본이 틀리면 화면이 틀린 임계로 경고한다.
 */
{
  const proc = read(PROC);
  const m = /DECLARE\s+@경고임계일수\s+INT\s*=\s*(\d+)\s*;/.exec(proc);
  if (!m) F('HOL-G4', '07a_Procedures_Holiday.sql 에서 @경고임계일수 를 읽지 못했다 - 미실행은 PASS 가 아니다');
  else if (Number(m[1]) !== threshold)
    F('HOL-G4', 'SP 의 @경고임계일수 사본이 00 §7.4 와 다르다', 'SP=' + m[1] + ' / 00 §7.4=' + threshold);
  else P('HOL-G4', 'SP 의 @경고임계일수 사본 = 00 §7.4 (' + threshold + '일)');
}

/* ---------------------------------------------------------------- HOL-G5
 * 만료. 이것이 이 게이트의 본체다.
 */
{
  const today = new Date();
  const todayUTC = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate());
  const endUTC = Date.parse(rangeTo + 'T00:00:00Z');
  const left = Math.round((endUTC - todayUTC) / 86400000);
  const iso = new Date(todayUTC).toISOString().slice(0, 10);
  if (left < threshold)
    F('HOL-G5', '공휴일 등재가 만료에 가깝다 - Seed 를 연장하라',
      '오늘 ' + iso + ' · 등재 최종일 ' + rangeTo + ' · 잔여 ' + left + '일 < 임계 ' + threshold + '일\n' +
      '고칠 곳: 00 §7.4 등재 범위 · 06 §14 표 · deploy/02_Seed.sql\n' +
      '대체공휴일 규칙은 자주 바뀐다. 현행 법령과 월력요항을 다시 조회하라 (06 §14.1).');
  else
    P('HOL-G5', '공휴일 등재 잔여 ' + left + '일 >= 임계 ' + threshold + '일 (오늘 ' + iso + ' · 최종일 ' + rangeTo + ')');
}

console.log('\n=== verify-holiday-seed: PASS ' + pass + ' / FAIL ' + fail + ' ===');
process.exit(fail ? 1 : 0);
