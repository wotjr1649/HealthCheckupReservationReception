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
  // [X] `active` 를 파싱해 놓고 아무 데서도 보지 않았다. 죽은 변수가 남았다는 것은
  //     원래 보려던 것이 있었다는 뜻이다 — 비활성 행은 판정식(03_Functions.sql)이 걸러내므로
  //     Seed 의 신정 한 줄을 0 으로 바꾸면 신정이 업무 가능일이 된다. 그것을 여기서 본다.
  const inactive = rows.filter(r => r.active !== '1');
  const bad = [];
  if (dup.length)      bad.push('중복 날짜 ' + [...new Set(dup)].join(', '));
  if (badType.length)  bad.push('휴무구분 도메인 밖 ' + badType.map(r => r.date + ' ' + r.type).join(', '));
  if (outside.length)  bad.push('00 §7.4 등재 범위 밖 ' + outside.map(r => r.date).join(', '));
  if (sunday.length)   bad.push('일요일은 Seed 하지 않는다 (06 §14.2) ' + sunday.map(r => r.date).join(', '));
  if (inactive.length) bad.push('사용여부 0 인 Seed 행 (판정식이 걸러내 업무 가능일이 된다) '
                                + inactive.map(r => r.date).join(', '));
  bad.length ? F('HOL-G2', 'Seed 행 구성 오류 ' + bad.length + '건', bad.join('\n'))
             : P('HOL-G2', 'Seed ' + rows.length + '행 - 날짜 중복 0 · 구분 도메인 일치 · 범위 안 · 일요일 0 · 전부 활성');
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

/* ---------------------------------------------------------------- HOL-G5 · G6
 * 만료. 이것이 이 게이트의 본체다.
 *
 * [X] 초안은 잔여를 `00` §7.4 의 **선언 문자열**에서 셌다. 그러면 이 게이트가 red 가 됐을 때
 *     `00` 한 줄을 다음 해로 늘리는 것만으로 green 이 된다 — Seed 는 한 행도 안 늘었는데.
 *     게이트를 침묵시키는 가장 쉬운 편집이 아무것도 고치지 않는 편집인 구조였다.
 *     잔여는 **Seed 의 실제 최종일**에서 세고, 선언이 Seed 를 앞서면 따로 잡는다.
 */
{
  const holidays = rows.filter(r => r.type === '법정공휴일' || r.type === '대체공휴일');
  const seedMax = holidays.length ? holidays.map(r => r.date).sort().pop() : null;
  const today = new Date();
  const todayUTC = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate());
  const iso = new Date(todayUTC).toISOString().slice(0, 10);
  const days = (a, b) => Math.round((Date.parse(b + 'T00:00:00Z') - a) / 86400000);

  if (!seedMax) {
    F('HOL-G5', 'Seed 에 법정·대체 공휴일이 한 행도 없다 - 만료를 판정할 근거가 없다');
    F('HOL-G6', '선언과 Seed 를 대조할 수 없다');
  } else {
    const left = days(todayUTC, seedMax);
    if (left < threshold)
      F('HOL-G5', '공휴일 등재가 만료에 가깝다 - Seed 를 연장하라',
        '오늘 ' + iso + ' · Seed 최종일 ' + seedMax + ' · 잔여 ' + left + '일 < 임계 ' + threshold + '일\n' +
        '고칠 곳: deploy/02_Seed.sql · 06 §14 표 · 00 §7.4 등재 범위 — **셋 다**여야 이 게이트가 다시 green 이 된다\n' +
        '대체공휴일 규칙은 자주 바뀐다. 현행 법령과 월력요항을 다시 조회하라 (06 §14.1).');
    else
      P('HOL-G5', '공휴일 등재 잔여 ' + left + '일 >= 임계 ' + threshold + '일 (오늘 ' + iso + ' · Seed 최종일 ' + seedMax + ')');

    // 선언이 Seed 를 앞서면 그만큼이 빈 구간이다. 공휴일은 최소 월 단위로 있으므로
    // 선언 끝과 Seed 최종일이 60일 넘게 벌어지면 그 해가 통째로 안 들어온 것이다.
    const GAP = 60;
    const gap = days(Date.parse(seedMax + 'T00:00:00Z'), rangeTo);
    if (gap > GAP)
      F('HOL-G6', '00 §7.4 선언이 Seed 보다 ' + gap + '일 앞선다 - 선언만 늘리고 Seed 를 안 넣었다',
        '선언 끝 ' + rangeTo + ' · Seed 최종일 ' + seedMax + ' (허용 ' + GAP + '일)');
    else
      P('HOL-G6', '00 §7.4 선언 끝 ' + rangeTo + ' 과 Seed 최종일 ' + seedMax + ' 의 간격 ' + gap + '일 <= ' + GAP + '일');
  }
}

console.log('\n=== verify-holiday-seed: PASS ' + pass + ' / FAIL ' + fail + ' ===');
process.exit(fail ? 1 : 0);
