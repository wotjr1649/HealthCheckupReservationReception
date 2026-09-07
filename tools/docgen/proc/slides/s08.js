'use strict';
// 8 — P03-01 접수 처리 · P03-02 접수 대상 확인 · P03-03 접수 가능조건 검증(5게이트)
// WalkIn 복귀 경로(P03-02 → P02-01 WalkIn → WorkId 반환)를 함께 표시한다.
const { A, node, gate, term, sub, head, ar, elab, elabN, S } = require('../flow');

// P03-01 : 7레인 × 2행
const P = [0.46, 2.255, 4.05, 5.845, 7.64, 9.435, 11.23], PW = 1.61;
const pcx = i => P[i] + PW / 2, pr = i => P[i] + PW;
const RA = 1.38, RB = 2.30, PH = 0.52;

// P03-02 : 6레인 × 3행
const M = [0.46, 2.558, 4.656, 6.754, 8.852, 10.95], MW = 1.918;
const mcx = i => M[i] + MW / 2, mr = i => M[i] + MW;
const T = [3.20, 3.96, 4.72], MH = 0.52;
const my = i => T[i] + MH / 2, mb = i => T[i] + MH;

// P03-03 : 7레인 × 1행
const G = [0.46, 2.264, 4.068, 5.872, 7.676, 9.48, 11.284], GW = 1.584;
const gcx = i => G[i] + GW / 2, gr = i => G[i] + GW;
const GY = 5.82, GH = 0.44, RAIL = 6.44;

const NS = { size: 8 };

function draw(c) {
  // ── P03-01 접수 처리 ─────────────────────────────────────────────
  head(c, A.x, 1.06, A.w, 'P03-01  접수 처리', { h: 0.24 });
  term(c, P[0], RA, PW, PH, '접수 처리 시작', NS);
  gate(c, P[1], RA, PW, PH, 'PatientId 또는\nWorkId 전달됨?', NS);
  sub(c, P[2], RA, PW, PH, 'P01-01\n수검자 조회 및 확정', NS);
  gate(c, P[3], RA, PW, PH, '수검자 확정?', NS);
  sub(c, P[4], RA, PW, PH, 'P03-02\n접수 대상 확인', NS);
  gate(c, P[5], RA, PW, PH, '접수 대상\nWorkId 확정?', NS);
  term(c, P[6], RA, PW, PH, '접수 대상 없음 / 중단', NS);

  node(c, P[0], RB, PW, PH, '예약정보·NEX·AEX 확인', NS);
  sub(c, P[1], RB, PW, PH, 'P03-03\n접수 가능조건 검증', NS);
  gate(c, P[2], RB, PW, PH, '접수 가능?', NS);
  node(c, P[3], RB, PW, PH, '상태·동시성 최종검증', NS);
  node(c, P[4], RB, PW, PH, '동일 Work 행\nRSV→RCP 원자적 변경', NS);
  term(c, P[5], RB, PW, PH, '접수 완료', NS);
  term(c, P[6], RB, PW, PH, '접수 불가', NS);

  const ay = RA + PH / 2, by = RB + PH / 2, ab = RA + PH;
  ar(c, [[pr(0), ay], [P[1] - 0.03, ay]]);
  ar(c, [[pr(1), ay], [P[2] - 0.03, ay]]);
  elabN(c, (pr(1) + P[2]) / 2, ay - 0.19, 'No');
  ar(c, [[pr(2), ay], [P[3] - 0.03, ay]]);
  ar(c, [[pr(3), ay], [P[4] - 0.03, ay]]);
  elabN(c, (pr(3) + P[4]) / 2, ay - 0.19, 'Yes');
  ar(c, [[pr(4), ay], [P[5] - 0.03, ay]]);
  ar(c, [[pr(5), ay], [P[6] - 0.03, ay]]);
  elabN(c, (pr(5) + P[6]) / 2, ay - 0.19, 'No');
  // PatientId/WorkId 전달됨 Yes → P03-02
  ar(c, [[pcx(1), ab], [pcx(1), 2.06], [pcx(4) - 0.35, 2.06], [pcx(4) - 0.35, ab + 0.03]]);
  elab(c, pcx(1) + 0.42, 2.06, 'Yes');
  // 수검자 확정 No → 접수 대상 없음 / 중단
  ar(c, [[pcx(3), ab], [pcx(3), 1.98], [pcx(6), 1.98], [pcx(6), ab + 0.03]]);
  elab(c, pcx(3) + 0.42, 1.98, 'No');
  // 접수 대상 WorkId 확정 Yes → 예약정보 확인
  ar(c, [[pcx(5), ab], [pcx(5), 2.18], [pcx(0), 2.18], [pcx(0), RB - 0.03]]);
  elab(c, pcx(5) - 0.60, 2.18, 'Yes');
  ar(c, [[pr(0), by], [P[1] - 0.03, by]]);
  ar(c, [[pr(1), by], [P[2] - 0.03, by]]);
  ar(c, [[pr(2), by], [P[3] - 0.03, by]]);
  elabN(c, (pr(2) + P[3]) / 2, by - 0.19, 'Yes');
  ar(c, [[pr(3), by], [P[4] - 0.03, by]]);
  ar(c, [[pr(4), by], [P[5] - 0.03, by]]);
  // 접수 가능 No → 접수 불가
  ar(c, [[pcx(2), RB], [pcx(2), 2.24], [pcx(6), 2.24], [pcx(6), RB - 0.03]]);
  elab(c, pcx(2) + 0.40, 2.24, 'No');

  // ── P03-02 접수 대상 확인 ────────────────────────────────────────
  head(c, A.x, 2.92, A.w, 'P03-02  접수 대상 확인   ·   당일 RSV 우선 · RCP는 중복접수로 차단 · 당일 업무가 없으면 WalkIn 당일예약 후 복귀', { h: 0.24 });
  term(c, M[0], T[0], MW, MH, '접수 대상 확인 시작', NS);
  node(c, M[1], T[0], MW, MH, '수검자의 당일\n취소(CNR·CNC) 제외 업무 조회', NS);
  gate(c, M[2], T[0], MW, MH, '당일 업무 존재?', NS);
  gate(c, M[3], T[0], MW, MH, '상태 = RSV?', NS);
  node(c, M[4], T[0], MW, MH, '기존 WorkId 확정', NS);
  term(c, M[5], T[0], MW, MH, '접수 대상 WorkId 확정', NS);

  term(c, M[2], T[1], MW, MH, '접수 대상 없음', NS);
  gate(c, M[3], T[1], MW, MH, '상태 = RCP?', NS);
  node(c, M[4], T[1], MW, MH, '중복접수 대상 /\n이후 조건검증에서 차단', NS);

  gate(c, M[0], T[2], MW, MH, '중복판단\n유효예약 존재?', NS);
  sub(c, M[1], T[2], MW, MH, 'P02-01\nWalkIn 신규 당일예약', NS);
  gate(c, M[2], T[2], MW, MH, '예약 생성 성공?', NS);
  term(c, M[5], T[2], MW, MH, '접수 불가', NS);

  ar(c, [[mr(0), my(0)], [M[1] - 0.03, my(0)]]);
  ar(c, [[mr(1), my(0)], [M[2] - 0.03, my(0)]]);
  ar(c, [[mr(2), my(0)], [M[3] - 0.03, my(0)]]);
  elabN(c, (mr(2) + M[3]) / 2, my(0) - 0.19, 'Yes');
  ar(c, [[mr(3), my(0)], [M[4] - 0.03, my(0)]]);
  elabN(c, (mr(3) + M[4]) / 2, my(0) - 0.19, 'Yes');
  ar(c, [[mr(4), my(0)], [M[5] - 0.03, my(0)]]);
  ar(c, [[mcx(3), mb(0)], [mcx(3), T[1] - 0.03]]);
  elab(c, mcx(3) + 0.26, (mb(0) + T[1]) / 2, 'No');
  ar(c, [[mr(3), my(1)], [M[4] - 0.03, my(1)]]);
  elabN(c, (mr(3) + M[4]) / 2, my(1) - 0.19, 'Yes');
  ar(c, [[M[3], my(1)], [mr(2) + 0.03, my(1)]]);
  elabN(c, (M[3] + mr(2)) / 2, my(1) - 0.19, 'No');
  // 중복접수 대상 → 접수 불가
  ar(c, [[mr(4), my(1)], [mcx(5), my(1)], [mcx(5), T[2] - 0.03]]);
  // 당일 업무 없음 → WalkIn 분기
  ar(c, [[mcx(2), mb(0)], [mcx(2), 3.80], [mcx(0), 3.80], [mcx(0), T[2] - 0.03]]);
  elab(c, mcx(2) - 0.44, 3.80, 'No');
  ar(c, [[mr(0), my(2)], [M[1] - 0.03, my(2)]]);
  elabN(c, (mr(0) + M[1]) / 2, my(2) - 0.19, 'No');
  ar(c, [[mr(1), my(2)], [M[2] - 0.03, my(2)]]);
  // 중복판단 유효예약 있음 → 접수 불가
  ar(c, [[mcx(0), mb(2)], [mcx(0), 5.34], [mcx(5) - 0.42, 5.34], [mcx(5) - 0.42, mb(2) + 0.03]]);
  elab(c, mcx(0) + 0.44, 5.34, 'Yes');
  // 예약 생성 실패 → 접수 대상 없음
  ar(c, [[mcx(2), T[2]], [mcx(2), mb(1) + 0.03]]);
  elab(c, mcx(2) + 0.26, (mb(1) + T[2]) / 2, 'No');
  // 예약 생성 성공 → 접수 대상 WorkId 확정 (WalkIn 복귀)
  ar(c, [[mr(2), my(2)], [8.762, my(2)], [8.762, 3.84], [mcx(5), 3.84], [mcx(5), mb(0) + 0.03]]);
  elab(c, 8.762 + 0.90, 3.84, 'Yes — WorkId 반환');

  // ── P03-03 접수 가능조건 검증 ────────────────────────────────────
  head(c, A.x, 5.50, A.w, 'P03-03  접수 가능조건 검증   —   5게이트를 모두 통과해야 접수 가능하다', { h: 0.24 });
  term(c, G[0], GY, GW, GH, '접수 가능조건\n검증 시작', NS);
  const GT = ['현재 상태 = RSV?', '예약일 = DB 현재일?', '업무 가능일?', '09:00 ≤ 현재시각\n< 18:00?', '해당 시간대\n접수 마감 전?'];
  GT.forEach((t, i) => gate(c, G[i + 1], GY, GW, GH, t, NS));
  term(c, G[6], GY, GW, GH, '접수 가능', NS);
  term(c, G[6], 6.50, GW, 0.30, '접수 불가', NS);
  for (let i = 0; i < 6; i++) ar(c, [[gr(i), GY + GH / 2], [G[i + 1] - 0.03, GY + GH / 2]]);
  c.line(gcx(1), RAIL, gcx(6), RAIL, { stroke: S.C.ink, sw: S.W.ctrl });
  for (let i = 1; i <= 5; i++) c.line(gcx(i), GY + GH, gcx(i), RAIL, { stroke: S.C.ink, sw: S.W.ctrl });
  ar(c, [[gcx(6), RAIL], [gcx(6), 6.47]]);
  c.text(A.x, 6.32, 1.72, 0.18, 'No — 하나라도 불충족', { size: 8, align: 'left', color: S.C.hint });
}

module.exports = {
  title: 'P03  접수 처리 · 접수 대상 확인 · 접수 가능조건 검증',
  caption: '적용 정책: P03-01 RCP-01, RCP-04, RP-08 / P03-02 CP-05, RP-05, RP-06, RCP-01 / P03-03 CP-01~04, HOL, RCP-02, RCP-03, 마감시간 정책',
  draw,
};
