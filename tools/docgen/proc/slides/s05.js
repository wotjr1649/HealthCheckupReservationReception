'use strict';
// 5 — P02-04 예약 일정 검증 : 7게이트 순차 + Context별 마감
const { A, TS, node, gate, term, head, note, ar, elab, elabN, table, S } = require('../flow');

const W = 1.23, PITCH = 1.39, X0 = 0.49;
const gx = i => X0 + i * PITCH, gcx = i => gx(i) + W / 2, grr = i => gx(i) + W;
const GY = 2.00, GH = 0.70, GB = GY + GH, GCY = GY + GH / 2;
const RAIL = 3.10;
const NS = { size: 8 };

const GATES = [
  ['예약일이 과거?', 'Yes'],
  ['업무 가능일?', 'No'],
  ['시간대 허용?', 'No'],
  ['당일예약?', null],
  ['Context별\n마감 전?', 'No'],
  ['시간대 정원\n< 20?', 'No'],
  ['동일 수검자 다른\n유효예약 없음?', 'No'],
];

function draw(c) {
  head(c, A.x, 1.06, A.w, '일정 검증 게이트   —   앞 게이트를 통과해야 다음 게이트를 본다. 하나라도 불충족이면 즉시 일정 불가다.');

  term(c, gx(0), GY, W, GH, '일정 검증 시작', NS);
  GATES.forEach(([t], i) => {
    gate(c, gx(i + 1), GY, W, GH, t, NS);
    c.text(gx(i + 1), 1.58, W, 0.18, `게이트 ${i + 1}`, { size: 8, bold: true, color: S.C.hint });
  });
  term(c, gx(8), GY, W, GH, '일정 가능', NS);
  term(c, gx(8), 3.30, W, 0.50, '일정 불가', NS);

  for (let i = 0; i < 8; i++) {
    if (i === 4) continue;                       // 당일예약? Yes 는 다음 게이트로, No 는 우회
    ar(c, [[grr(i), GCY], [gx(i + 1) - 0.03, GCY]]);
  }
  ar(c, [[grr(4), GCY], [gx(5) - 0.03, GCY]]);
  elabN(c, (grr(4) + gx(5)) / 2, GY - 0.13, 'Yes');

  // 당일예약 No → 시간대 정원 게이트로 우회 (Context별 마감 건너뜀)
  // 게이트 번호 라벨을 관통하지 않도록 박스 안쪽 가장자리에서 올린다.
  const ox = grr(4) - 0.12, ix = gx(6) + 0.12;
  ar(c, [[ox, GY], [ox, 1.44], [ix, 1.44], [ix, GY - 0.03]]);
  elab(c, (ox + ix) / 2, 1.44, 'No — 당일예약이 아니면 마감 미적용');

  // 불충족 → 일정 불가 (공통 rail)
  c.line(gcx(1), RAIL, gcx(8), RAIL, { stroke: S.C.ink, sw: S.W.ctrl });
  GATES.forEach(([, lab], i) => {
    if (!lab) return;
    c.line(gcx(i + 1), GB, gcx(i + 1), RAIL, { stroke: S.C.ink, sw: S.W.ctrl });
    elab(c, gcx(i + 1), GB + 0.16, lab);
  });
  ar(c, [[gcx(8), RAIL], [gcx(8), 3.27]]);

  // ── Context별 마감 ───────────────────────────────────────────────
  head(c, A.x, 3.98, 6.20, 'Context별 마감');
  table(c, A.x, 4.30, [
    { t: 'Context', w: 1.40 },
    { t: '당일 허용조건', w: 4.80, align: 'left' },
  ], [
    ['Normal', '해당 시간대 당일예약 마감 전'],
    ['WalkIn', '해당 시간대 접수 마감 전'],
    ['미래일', '당일 마감 미적용, CP/HOL·시간대·정원·중복 적용'],
  ], { size: 10, headSize: 10, rowH: 0.34, headH: 0.32 });

  note(c, A.x, 5.66, 6.20, [
    '· 마감과 같은 시각부터 불가다.',
    '· 마감시간 정책은 CP-04 운영시간(09:00 ≤ 현재시각 < 18:00) 안에서만 적용한다.',
  ], { size: 10, stroke: S.C.dim });

  // ── 적용 규칙 ────────────────────────────────────────────────────
  head(c, 6.90, 3.98, A.r - 6.90, '적용 규칙');
  note(c, 6.90, 4.30, A.r - 6.90, [
    '· 신규예약과 예약변경은 저장 Transaction 안에서 정원·중복을 다시 확인한다.',
    '· 예약변경에서는 변경 대상 WorkId 자체를 중복판단에서 제외하고 다른 RSV/RCP 업무만 충돌로 본다.',
    '· 화면의 정원값은 안내용 Snapshot이며 저장 성공을 보장하지 않는다.',
    '· 중복판단 유효예약은 예약일 ≥ DB 현재일이고 상태가 RSV 또는 RCP인 업무다. 과거 업무는 중복판단에서 제외한다.',
  ], { size: 10 });
}

module.exports = {
  title: 'P02-04  예약 일정 검증',
  caption: '적용 정책: P02-04 CP-01~04, HOL, RP-02~06, RP-09, 마감시간 정책',
  draw,
};
