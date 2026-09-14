/*
 * verify_output.js — docs/baseline/output/ 여섯 종의 **공개 적합성** 게이트.
 *
 * 산출물은 사내 공개용이다. 저장소 안에서만 뜻이 있는 통제 어휘(기준선·FINAL·Phase·
 * PASS·회귀·커밋…)와 따라갈 수 없는 절 참조가 한 글자도 새어 나가면 안 된다.
 *
 * [!] 이 검사는 **정확해야 한다.** 이전 판(build_00.js 안의 목록)은 단순 부분일치라
 *     '검사결과 입력·판독·최종 판정'(업무 범위)과 '폐기능'(폐+기능)을 잡았고,
 *     게다가 적중해도 실패시키지 않아 아무 힘이 없었다. 규칙마다 허용 문자열을 명시하고,
 *     한 건이라도 남으면 exit 1 이다.
 *
 * 반대쪽도 본다 — `03` 은 **반드시 들어 있어야 하는 문장**이 있다 (REQUIRED).
 *
 *   node tools/docgen/verify_output.js            산출물 판정
 *   node tools/docgen/verify_output.js source     설계 소스에 그 문장이 아직 있는가
 *   node tools/docgen/verify_output.js selftest   규칙이 실제로 잡는지
 */
'use strict';

/*
 * rules: [이름, 정규식, 허용 문자열…]
 *   허용은 **정확한 부분문자열**이다. "그 낱말이 들어간 줄은 봐준다" 같은 느슨한 예외를
 *   두지 않는다 — 그것 자체가 우회 통로가 된다.
 */
const RULES = [
  ['FINAL',        /\bFINAL\b/],
  ['READ-ONLY',    /READ-ONLY|READONLY/],          // UI 용어 ReadOnly 는 대소문자가 다르다
  ['기준선',        /기준선/],
  ['기준선 ID',     /HC-RSV-RCP/],
  ['변경 통제',      /변경 통제/],
  ['최종 검수',      /최종 검수/],
  ['최종 판정',      /최종 판정/, '검사결과 입력·판독·최종 판정', '결과 입력·판독'],
  ['판정 GO',       /판정\s*:\s*GO|\bGO\b\s*[—-]/],
  ['시험 판정',      /\bPASS\b|\bFAIL\b|NOT RUN|\bSKIP\b/],
  ['회귀',          /회귀/],
  ['게이트',         /게이트/, '일정 검증 게이트', '게이트 1', '게이트 2', '게이트 3',
                            '게이트 4', '게이트 5', '게이트 6', '게이트 7', '5게이트',
                            '앞 게이트를 통과해야 다음 게이트를 본다'],
  ['커밋·git',      /커밋|\bgit\b/i],
  ['Phase',        /\bPhase\b/],
  ['후속 문서',      /후속 문서|후속 Phase/],
  ['적대적',         /적대적/],
  ['실측',          /실측/],
  ['문서 버전',      /\bv\d+\.\d+\b|\bCANDIDATE\b/],
  // [X] `[234]` -> `[2-9]` 로 고쳤는데 그것도 R10 부터 다시 새는 목록이었다.
  //     바로 이 주석이 "회차가 늘 때마다 고쳐야 하는 목록은 반드시 뒤처진다" 라고 적어 놓고
  //     같은 형태를 다시 넣었다. 자릿수를 열어 두면 뒤처지지 않는다.
  //     R1 회차 표기는 이 저장소에 없으므로 넓혀도 오탐이 늘지 않는다.
  ['재봉인 회차',     /\bR\d+\b/],
  ['절 참조',        /§/],
  ['과제',          /과제/],
];

/*
 * 넣었다가 뺀 규칙 두 개. 실측해 보니 참 적중이 0 이고 오탐만 냈다.
 * 오탐만 내는 규칙은 없느니만 못하다 — 예외 목록을 키우게 만들고, 그 목록이 우회 통로가 된다.
 *
 *   폐기        산출물에서는 전부 '입력을 폐기한다'(화면 동작)다. 내부 뜻('계정·권한 폐기
 *               결정')은 docs/phase4 에만 있고 공개본에 온 적이 없다.
 *   Seed/Test   'Seed' 는 04 §8.3.2·§8.4.1·§8.5.2 가 참조 데이터 공급을 부르는 설계서
 *               자체의 어휘다. 기준선·회귀·커밋 같은 저장소 통제 어휘가 아니다.
 */

/*
 * REQUIRED — RULES 의 반대. **반드시 들어 있어야 하는 문장**이다.
 *
 * [X] `03` 화면설계서는 설계 시점의 기록이고 실행 프로그램과 다른 부분이 있다
 *     (2026-09-10 · 2026-09-11 사용자 결정). 그 사실이 공개본 **안**에 없으면 받아 보는
 *     쪽은 그림과 다른 프로그램을 결함으로 읽는다. 저장소 안 문서에 적어 두는 것으로는
 *     닿지 않는다 — 사내로 나가는 것은 이 파일뿐이다.
 *
 * [!] **선언만 두면 썩는다.** 그 결정에 딸려 적어 둔 수치들이 실제로 썩었다. 그래서
 *     문장을 넣는 데서 끝내지 않고 여기서 판정한다. 문구를 고치려면 여기와
 *     `wireframe/screens/a0_list.js` 를 같은 커밋에서 함께 고친다.
 */
const REQUIRED = [
  // 산출물 · 그 문장을 갖는 설계 소스 · 이름 · 조각들
  ['03_검진_예약접수_화면설계서.pptx', 'wireframe/screens/a0_list.js', '문서의 지위',
    /설계 시점의 화면 정의/,
    /동작의 기준은 실행 프로그램/],
];

/** 조각이 다 들어 있는가. 줄바꿈·연속 공백으로 끊겨도 같은 문장이다. */
function missingIn(text, needs) {
  const flat = String(text).replace(/\s+/g, ' ');
  return needs.filter(re => !re.test(flat));
}

/** 판정만 한다 — 파일을 읽지 않으므로 selftest 가 가짜 입력으로 그대로 부를 수 있다. */
function judge(map) {
  const lines = [];
  let fail = 0, checked = 0;

  for (const [file, items] of Object.entries(map)) {
    const hits = [];
    for (const it of items) {
      checked++;
      for (const [name, re, ...allow] of RULES) {
        if (!re.test(it.text)) continue;
        // 허용 문자열을 걷어낸 뒤에도 걸리면 진짜 적중이다.
        let t = it.text;
        for (const a of allow) t = t.split(a).join('');
        if (re.test(t)) hits.push({ name, where: it.where, text: it.text.replace(/\s+/g, ' ').slice(0, 120) });
      }
    }
    const uniq = [...new Map(hits.map(h => [h.name + '|' + h.text, h])).values()];
    if (uniq.length) {
      fail += uniq.length;
      lines.push('FAIL ' + file + ' — 공개용에 들어가면 안 되는 내용 ' + uniq.length + '건');
      for (const h of uniq) lines.push('        [' + h.name + '] ' + h.where + '  ' + h.text);
    } else {
      lines.push('PASS ' + file + ' — 내부 통제 어휘 0건');
    }
  }

  for (const [file, , label, ...needs] of REQUIRED) {
    const items = map[file];
    // [X] 산출물이 없으면 「없으니 통과」가 된다 — 조용히 통과하는 그 함정이다. FAIL 로 센다.
    if (!items) {
      fail++;
      lines.push('FAIL ' + file + ' — 산출물이 없다. ' + label + ' 을(를) 확인할 수 없다');
      continue;
    }
    const missing = missingIn(items.map(i => i.text).join(' '), needs);
    if (missing.length) {
      fail += missing.length;
      lines.push('FAIL ' + file + ' — ' + label + ' 문장이 없다 ' + missing.length + '건');
      for (const re of missing) lines.push('        없는 것: ' + re);
    } else {
      lines.push('PASS ' + file + ' — ' + label + ' 있다');
    }
  }

  return { fail, checked, lines };
}

/*
 * selftest — 규칙이 실제로 잡는지 가짜 입력으로 본다.
 * 이 파일에는 없던 것이다. 이 저장소의 다른 게이트는 전부 갖고 있고, fail-open 인 게이트를
 * 여러 번 겪었다. 판정을 judge() 로 떼어 낸 이유가 그것뿐이다.
 */
/*
 * source — 설계 소스에 그 문장이 아직 있는가.
 *
 * [X] **산출물만 보면 「지웠다」를 다음 재생성까지 아무도 모른다.** 그 사이의 커밋은 조용히
 *     지나간다. 소스는 stdlib 만으로 볼 수 있으므로 상시 회귀가 여기를 본다
 *     (`winforms/scripts/test.sh`). 산출물 쪽은 xlsx·pptx 판독 모듈이 필요해 그 회귀의
 *     「어디서나 같은 판정」을 깨므로 `build_all.js` 가 생성 직후에 본다.
 */
if (process.argv[2] === 'source') {
  const fs = require('fs');
  const path = require('path');
  let fail = 0;

  for (const [, src, label, ...needs] of REQUIRED) {
    const full = path.join(__dirname, src);
    if (!fs.existsSync(full)) {
      fail++;
      console.log('FAIL ' + src + ' — 설계 소스가 없다. ' + label + ' 을(를) 확인할 수 없다');
      continue;
    }
    const missing = missingIn(fs.readFileSync(full, 'utf8'), needs);
    if (missing.length) {
      fail += missing.length;
      console.log('FAIL ' + src + ' — ' + label + ' 문장이 없다 ' + missing.length + '건');
      for (const re of missing) console.log('        없는 것: ' + re);
    } else {
      console.log('PASS ' + src + ' — ' + label + ' 있다');
    }
  }

  console.log('=== verify_output(source): FAIL ' + fail + ' ===');
  process.exit(fail ? 1 : 0);
}

if (process.argv[2] === 'selftest') {
  const WF = '03_검진_예약접수_화면설계서.pptx';
  const NOTICE = '이 문서는 설계 시점의 화면 정의다. 개발 과정에서 사용성 판단에 따라 실제 화면과 '
    + '달라진 부분이 있으며, 동작의 기준은 실행 프로그램이다. 업무 규칙과 항목의 의미는 이 문서가 기준이다.';
  const one = text => ({ [WF]: [{ where: 'selftest', text }] });
  let rc = 0;
  const run = (label, want, map) => {
    const got = judge(map).fail;
    if (got === want) { console.log('PASS VOUT-SELFTEST ' + label); }
    else { console.log('FAIL VOUT-SELFTEST ' + label + ' (기대 FAIL ' + want + ' · 실제 ' + got + ')'); rc = 1; }
  };

  run('지위 문장이 있으면 통과한다',       0, one(NOTICE));
  run('지위 문장이 없으면 잡는다',         2, one('아무 말도 하지 않는다'));
  run('앞 절반만 있으면 잡는다',           1, one('이 문서는 설계 시점의 화면 정의다.'));
  run('줄바꿈으로 끊겨도 같은 문장이다',    0, one(NOTICE.split(' ').join('\n')));
  run('산출물이 아예 없으면 잡는다',        1, {});
  run('통제 어휘를 잡는다',                1, one(NOTICE + ' FINAL'));
  run('허용 문자열은 봐준다',              0, one(NOTICE + ' 검사결과 입력·판독·최종 판정'));
  process.exit(rc);
}

(async () => {
  // [!] 여기서 부른다 — `source` 모드는 stdlib 만으로 돌아야 한다.
  const { all } = require('./scan_output.js');
  const { fail, checked, lines } = judge(await all());
  for (const l of lines) console.log(l);
  console.log('\n=== verify_output: 텍스트 ' + checked + '조각 · FAIL ' + fail + ' ===');
  process.exit(fail ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
