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
 *   node tools/docgen/verify_output.js
 */
'use strict';
const { all } = require('./scan_output.js');

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

(async () => {
  const map = await all();
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
      console.log('FAIL ' + file + ' — 공개용에 들어가면 안 되는 내용 ' + uniq.length + '건');
      for (const h of uniq) console.log('        [' + h.name + '] ' + h.where + '  ' + h.text);
    } else {
      console.log('PASS ' + file + ' — 내부 통제 어휘 0건');
    }
  }

  console.log('\n=== verify_output: 텍스트 ' + checked + '조각 · FAIL ' + fail + ' ===');
  process.exit(fail ? 1 : 0);
})().catch(e => { console.error(e); process.exit(1); });
