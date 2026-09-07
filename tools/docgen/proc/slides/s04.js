'use strict';
// 4 — P02 예약 관리 개요 : P02 전체 + P02-01 신규 예약 등록 + P02-02 예약 내용 구성 + P02-03 예약 일정 설정
// 기존 유효예약 존재 → 신규 중단, WorkId 로 기존 Work 를 여는 분기(E2)를 살려 둔다.
const { A, node, gate, term, sub, head, note, ar, elab, elabN, S } = require('../flow');

const P = [0.46, 2.255, 4.05, 5.845, 7.64, 9.435, 11.23], PW = 1.61;
const px = i => P[i], pcx = i => P[i] + PW / 2, pr = i => P[i] + PW;
const RA = 2.38, RB = 3.70, RH = 0.54;

const C5 = [0.46, 1.92, 3.38, 4.84, 6.30], CW = 1.30;
const cx5 = i => C5[i] + CW / 2, cr5 = i => C5[i] + CW;
const D3 = [7.90, 9.625, 11.35], DW = 1.52;
const dcx = i => D3[i] + DW / 2, dr = i => D3[i] + DW;
const RR = [4.68, 5.38, 6.08], RH2 = 0.46;

const NS = { size: 8 };

function draw(c) {
  // ── P02 전체 프로세스 ────────────────────────────────────────────
  term(c, 0.60, 1.32, 1.60, 0.42, '예약 관리 시작', NS);
  ar(c, [[2.23, 1.53], [2.37, 1.53]]);
  gate(c, 2.40, 1.32, 1.15, 0.42, '업무 구분?', NS);
  ar(c, [[3.55, 1.53], [3.75, 1.53]], { arrow: false });
  c.line(3.75, 1.16, 3.75, 1.88, { stroke: S.C.ink, sw: S.W.ctrl });
  [['신규 예약   →   P02-01 신규 예약 등록', 1.06],
   ['조회   →   P02-05 예약 조회', 1.30],
   ['변경   →   P02-06 예약 변경', 1.54],
   ['취소   →   P02-07 예약 취소', 1.78]].forEach(([t, y]) => {
    ar(c, [[3.75, y + 0.10], [3.97, y + 0.10]]);
    sub(c, 4.00, y, 3.15, 0.20, t, NS);
    ar(c, [[7.15, y + 0.10], [7.28, y + 0.10], [7.28, 1.53], [7.42, 1.53]]);
  });
  term(c, 7.45, 1.32, 1.75, 0.42, '예약 관리 종료', NS);
  note(c, 9.45, 1.06, A.r - 9.45, [
    '프로세스 간 호출 관계',
    'P02-01  →  P01-01 · P02-02',
    'P02-02  →  P02-03 · P02-04 · TGT · NEX · AEX',
    'P02-06  →  P02-05 · P02-03·04 · TGT · AEX',
    'P02-07  →  P02-05',
  ], { color: S.C.hint });

  // ── P02-01 신규 예약 등록 ────────────────────────────────────────
  head(c, A.x, 2.06, A.w, 'P02-01  신규 예약 등록', { h: 0.24 });
  term(c, px(0), RA, PW, RH, '신규 예약 시작', NS);
  gate(c, px(1), RA, PW, RH, 'PatientId 전달됨?', NS);
  sub(c, px(2), RA, PW, RH, 'P01-01\n수검자 조회 및 확정', NS);
  gate(c, px(3), RA, PW, RH, '수검자 확정?', NS);
  term(c, px(4), RA, PW, RH, '예약 중단 또는 실패', NS);
  sub(c, px(5), RA, PW, RH, 'P02-05 기존 예약 조회 /\nWorkId 선택', NS);
  term(c, px(6), RA, PW, RH, '기존 예약 연결 후\n신규예약 종료', NS);

  gate(c, px(0), RB, PW, RH, '중복판단\n유효예약 존재?', NS);
  sub(c, px(1), RB, PW, RH, 'P02-02\n예약 내용 구성', NS);
  gate(c, px(2), RB, PW, RH, '예약 내용 확정?', NS);
  node(c, px(3), RB, PW, RH, '저장시점 전체 Rule·\n동시성 최종검증', NS);
  gate(c, px(4), RB, PW, RH, '저장 가능?', NS);
  node(c, px(5), RB, PW, RH, 'Work+Exam 원자적 저장 /\n상태=RSV', NS);
  term(c, px(6), RB, PW, RH, '예약 완료 /\nWorkId 반환', NS);

  const ay = RA + RH / 2, by = RB + RH / 2, ab = RA + RH;
  ar(c, [[pr(0), ay], [px(1) - 0.03, ay]]);
  ar(c, [[pr(1), ay], [px(2) - 0.03, ay]]);
  elabN(c, (pr(1) + px(2)) / 2, ay - 0.19, 'No');
  ar(c, [[pr(2), ay], [px(3) - 0.03, ay]]);
  ar(c, [[pr(3), ay], [px(4) - 0.03, ay]]);
  elabN(c, (pr(3) + px(4)) / 2, ay - 0.19, 'No');
  // PatientId 전달됨 Yes → 중복판단
  ar(c, [[pcx(1), ab], [pcx(1), 3.14], [pcx(0) - 0.35, 3.14], [pcx(0) - 0.35, RB - 0.03]]);
  elab(c, pcx(1) - 0.45, 3.14, 'Yes');
  // 수검자 확정 Yes → 중복판단
  ar(c, [[pcx(3), ab], [pcx(3), 3.30], [pcx(0), 3.30], [pcx(0), RB - 0.03]]);
  elab(c, pcx(3) - 0.40, 3.30, 'Yes');
  // 중복판단 Yes → P02-05 기존 예약 조회
  ar(c, [[pcx(0) + 0.35, RB], [pcx(0) + 0.35, 3.36], [pcx(5), 3.36], [pcx(5), ab + 0.03]]);
  elab(c, pcx(5) - 0.55, 3.36, 'Yes');
  ar(c, [[pr(5), ay], [px(6) - 0.03, ay]]);
  // 중복판단 No → P02-02
  ar(c, [[pr(0), by], [px(1) - 0.03, by]]);
  elabN(c, (pr(0) + px(1)) / 2, by - 0.19, 'No');
  ar(c, [[pr(1), by], [px(2) - 0.03, by]]);
  ar(c, [[pr(2), by], [px(3) - 0.03, by]]);
  elabN(c, (pr(2) + px(3)) / 2, by - 0.19, 'Yes');
  ar(c, [[pr(3), by], [px(4) - 0.03, by]]);
  ar(c, [[pr(4), by], [px(5) - 0.03, by]]);
  elabN(c, (pr(4) + px(5)) / 2, by - 0.19, 'Yes');
  ar(c, [[pr(5), by], [px(6) - 0.03, by]]);
  // 예약 내용 확정 No / 저장 가능 No → 예약 중단 또는 실패
  ar(c, [[pcx(2), RB], [pcx(2), 3.58], [pcx(4) - 0.35, 3.58], [pcx(4) - 0.35, ab + 0.03]]);
  elab(c, pcx(2) + 0.34, 3.58, 'No');
  ar(c, [[pcx(4) + 0.35, RB], [pcx(4) + 0.35, 3.58], [pcx(4) - 0.32, 3.58]], { arrow: false });
  elab(c, pcx(4) + 0.35, 3.64, 'No');

  // ── P02-02 예약 내용 구성 ────────────────────────────────────────
  head(c, A.x, 4.36, 7.14, 'P02-02  예약 내용 구성', { h: 0.24 });
  const y0 = RR[0] + RH2 / 2, y1 = RR[1] + RH2 / 2, y2 = RR[2] + RH2 / 2;
  term(c, C5[0], RR[0], CW, RH2, '예약 내용\n구성 시작', NS);
  sub(c, C5[1], RR[0], CW, RH2, 'P02-03\n예약 일정 설정', NS);
  sub(c, C5[2], RR[0], CW, RH2, 'P02-04\n예약 일정 검증', NS);
  gate(c, C5[3], RR[0], CW, RH2, '일정 유효?', NS);
  term(c, C5[4], RR[0], CW, RH2, '예약 내용\n구성 중단', NS);

  node(c, C5[0], RR[1], CW, RH2, 'TGT 대상판정', NS);
  gate(c, C5[1], RR[1], CW, RH2, '일반건강검진\n대상?', NS);
  term(c, C5[2], RR[1], CW, RH2, '예약 진행 불가', NS);
  node(c, C5[3], RR[1], CW, RH2, 'NEX 자동구성', NS);
  node(c, C5[4], RR[1], CW, RH2, 'AEX 선택', NS);

  term(c, C5[2], RR[2], CW, RH2, '예약 내용 확정', NS);
  gate(c, C5[3], RR[2], CW, RH2, '검사구성 유효?', NS);
  node(c, C5[4], RR[2], CW, RH2, 'AEX 검증', NS);

  // 'AEX 검증' 노드만 있고 판정 기준이 없으면 무엇을 보고 유효/무효인지 알 수 없다.
  note(c, C5[0], RR[2], 2.86, [
    'P02-02 적용 규칙',
    '· AEX는 0개 이상 선택할 수 있다.',
    '· NEX와 같은 ExamItemCode 또는 성별조건 불충족 AEX는 선택 불가',
  ], { color: S.C.hint, size: 7.5 });

  ar(c, [[cr5(0), y0], [C5[1] - 0.03, y0]]);
  ar(c, [[cr5(1), y0], [C5[2] - 0.03, y0]]);
  ar(c, [[cr5(2), y0], [C5[3] - 0.03, y0]]);
  ar(c, [[cr5(3), y0], [C5[4] - 0.03, y0]]);
  elabN(c, (cr5(3) + C5[4]) / 2, y0 - 0.17, 'No');
  ar(c, [[cx5(3), RR[0] + RH2], [cx5(3), 5.20], [cx5(0), 5.20], [cx5(0), RR[1] - 0.03]]);
  elab(c, cx5(3) - 0.36, 5.20, 'Yes');
  ar(c, [[cr5(0), y1], [C5[1] - 0.03, y1]]);
  ar(c, [[cr5(1), y1], [C5[2] - 0.03, y1]]);
  elabN(c, (cr5(1) + C5[2]) / 2, y1 - 0.17, 'No');
  ar(c, [[cx5(1), RR[1] + RH2], [cx5(1), 5.96], [cx5(3), 5.96], [cx5(3), RR[1] + RH2 + 0.03]]);
  elab(c, cx5(1) + 0.36, 5.96, 'Yes');
  ar(c, [[cr5(3), y1], [C5[4] - 0.03, y1]]);
  ar(c, [[cx5(4), RR[1] + RH2], [cx5(4), RR[2] - 0.03]]);
  ar(c, [[C5[4], y2], [cr5(3) + 0.03, y2]]);
  ar(c, [[C5[3], y2], [cr5(2) + 0.03, y2]]);
  elabN(c, (C5[3] + cr5(2)) / 2, y2 - 0.17, 'Yes');
  // 검사구성 무효 → AEX 재선택 (우측 여백으로 되돌림)
  ar(c, [[cx5(3), RR[2] + RH2], [cx5(3), 6.66], [7.74, 6.66], [7.74, y1], [cr5(4) + 0.03, y1]]);
  elab(c, cx5(3) + 0.34, 6.66, 'No');

  // ── P02-03 예약 일정 설정 ────────────────────────────────────────
  head(c, 7.90, 4.36, A.r - 7.90, 'P02-03  예약 일정 설정   ·   평일 AM/PM, 토요일 AM만', { h: 0.24 });
  term(c, D3[0], RR[0], DW, RH2, '일정 설정 시작', NS);
  gate(c, D3[1], RR[0], DW, RH2, 'Context', NS);
  node(c, D3[2], RR[0], DW, RH2, 'Normal:\n예약일 선택', NS);
  node(c, D3[1], RR[1], DW, RH2, 'WalkIn: 예약일=DB 오늘 /\nReadOnly', NS);
  node(c, D3[2], RR[1], DW, RH2, '가능 시간대 및\n정원 조회', NS);
  term(c, D3[0], RR[2], DW, RH2, '일정 설정 중단', NS);
  gate(c, D3[1], RR[2], DW, RH2, '시간대 선택?', NS);
  term(c, D3[2], RR[2], DW, RH2, '일정 입력 완료', NS);

  ar(c, [[dr(0), y0], [D3[1] - 0.03, y0]]);
  ar(c, [[dr(1), y0], [D3[2] - 0.03, y0]]);
  elabN(c, (dr(1) + D3[2]) / 2, y0 - 0.17, 'Normal');
  ar(c, [[dcx(1), RR[0] + RH2], [dcx(1), RR[1] - 0.03]]);
  elab(c, dcx(1) + 0.34, (RR[0] + RH2 + RR[1]) / 2, 'WalkIn');
  ar(c, [[dcx(2), RR[0] + RH2], [dcx(2), RR[1] - 0.03]]);
  ar(c, [[dr(1), y1], [D3[2] - 0.03, y1]]);
  ar(c, [[dcx(2), RR[1] + RH2], [dcx(2), 5.94], [dcx(1), 5.94], [dcx(1), RR[2] - 0.03]]);
  ar(c, [[D3[1], y2], [dr(0) + 0.03, y2]]);
  elabN(c, (D3[1] + dr(0)) / 2, y2 - 0.17, 'No');
  ar(c, [[dr(1), y2], [D3[2] - 0.03, y2]]);
  elabN(c, (dr(1) + D3[2]) / 2, y2 - 0.17, 'Yes');
}

module.exports = {
  title: 'P02  예약 관리 개요',
  caption: '적용 정책: P02-01 RP-01, RP-06 / P02-02 RP-07, RP-08, TGT, NEX, AEX / P02-03 RP-02, RP-04, RP-05',
  draw,
};
