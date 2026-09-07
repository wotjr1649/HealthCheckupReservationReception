'use strict';
// 6 — P02-06 예약 변경. 원문 다이어그램은 노드 24·판정 8·연결선 29로 한 장에 판독되지 않는다.
// 축약 흐름 1줄 + 변경 Matrix + 종료점 4개로 재구성한다.
const { A, node, gate, term, sub, head, note, ar, table, S } = require('../flow');

const CW = 1.356, CP = 1.556, CY = 1.50, CH = 0.72;
const cx0 = i => 0.46 + i * CP, ccx = i => cx0(i) + CW / 2, crr = i => cx0(i) + CW;
const NS = { size: 8 };

const ENDS = [
  ['E1   예약 변경 완료', '실제 변경 있음 → 영향범위만 원자적 저장'],
  ['E2   변경 대상 없음', '조회 결과에서 변경 대상을 선택하지 않음'],
  ['E3   변경 불가 / 기존예약 유지', '상태 ≠ RSV · 예약일 변경 후 비대상 · 저장 불가'],
  ['E4   변경 없음 / No-op', '요청값이 DB 현재값과 같아 데이터 변경 없음'],
];

function draw(c) {
  head(c, A.x, 1.06, A.w, '축약 흐름   —   RSV 확인 → 실제 변경 계산 → 예약일/시간대/AEX 영향검증 → 상태·RowVersion 확인 → 저장 / No-op / 기존예약 유지');

  term(c, cx0(0), CY, CW, CH, '예약 변경 시작', NS);
  sub(c, cx0(1), CY, CW, CH, 'P02-05 예약 조회 ·\n대상 선택', NS);
  gate(c, cx0(2), CY, CW, CH, '현재 상태 = RSV?', NS);
  node(c, cx0(3), CY, CW, CH, 'DB 현재값과 요청값\n비교 · 실제 변경 계산', NS);
  node(c, cx0(4), CY, CW, CH, '예약일 / 시간대 / AEX\n영향검증', NS);
  node(c, cx0(5), CY, CW, CH, '상태 · RowVersion\n최종확인', NS);
  for (let i = 0; i < 5; i++) ar(c, [[crr(i), CY + CH / 2], [cx0(i + 1) - 0.03, CY + CH / 2]]);

  // 종료점 4개 — 우측으로 분기
  const EX = 9.90, EW = A.r - EX, EH = 0.50, EP = 0.56, EY = 1.20;
  c.line(9.72, EY + EH / 2, 9.72, EY + 3 * EP + EH / 2, { stroke: S.C.ink, sw: S.W.ctrl });
  ar(c, [[crr(5), CY + CH / 2], [9.72, CY + CH / 2]], { arrow: false });
  ENDS.forEach(([t, cond], i) => {
    const y = EY + i * EP;
    ar(c, [[9.72, y + EH / 2], [EX - 0.03, y + EH / 2]]);
    term(c, EX, y, EW, EH, t + '\n' + cond, NS);
  });

  // ── 변경 Matrix ──────────────────────────────────────────────────
  head(c, A.x, 3.60, 7.34, '변경 Matrix   —   실제 변경조합별 재검증·저장 범위');
  table(c, A.x, 3.92, [
    { t: '실제 변경조합', w: 2.10, align: 'left' },
    { t: '일정', w: 0.58 }, { t: 'TGT', w: 0.58 }, { t: 'NEX', w: 0.58 }, { t: 'AEX', w: 0.58 },
    { t: '저장', w: 2.92, align: 'left' },
  ], [
    ['예약일 변경 포함', 'O', 'O', 'O', 'O', 'Work+영향 Detail'],
    ['시간대만 변경', 'O', 'X', 'X', 'X', 'Work만'],
    ['AEX만 변경', 'X', 'X', 'X', 'O', 'AEX Detail+Work 동시성 갱신'],
    ['시간대+AEX', 'O', 'X', 'X', 'O', 'Work+AEX Detail'],
    ['변경 없음', 'X', 'X', 'X', 'X', '데이터 변경 없음'],
  ], { size: 9.5, headSize: 9.5, rowH: 0.30, headH: 0.30 });

  // ── 적용 규칙 ────────────────────────────────────────────────────
  head(c, 8.00, 3.60, A.r - 8.00, '적용 규칙');
  note(c, 8.00, 3.92, A.r - 8.00, [
    '· 예약일 변경 후 비대상이면 변경을 저장하지 않고 기존 예약을 유지한다.',
    '· 새 NEX와 기존/요청 AEX가 충돌하면 유효하게 재선택한 뒤 저장한다.',
    '· 시간대만 변경한 경우 TGT/NEX/AEX를 재검증하거나 재작성하지 않는다.',
    '· AEX 실제 변경은 Work Aggregate 변경으로 처리해 Work의 동시성 토큰도 갱신한다.',
    '· AEX 집합이 동일하면 Detail과 Work를 불필요하게 갱신하지 않는다.',
    '· 일정·중복 검증에서 현재 WorkId는 제외하며, 자기 자신 때문에 다른 유효예약으로 차단되지 않아야 한다.',
  ], { size: 9 });

  // ── 예약일 변경 영향 재검증 ──────────────────────────────────────
  head(c, A.x, 5.94, A.w, '예약일 변경 영향 재검증   —   유효한 경우만 저장한다');
  const BW = 1.918, BP = 2.098, BY = 6.28, BH = 0.44;
  ['예약일 변경', '일정검증', 'TGT 재판정', 'NEX 재구성', 'AEX 재검증', '유효한 경우만 저장'].forEach((t, i) => {
    const x = A.x + i * BP;
    node(c, x, BY, BW, BH, t, { size: 9 });
    if (i > 0) ar(c, [[x - (BP - BW) + 0.02, BY + BH / 2], [x - 0.03, BY + BH / 2]]);
  });
}

module.exports = {
  title: 'P02-06  예약 변경',
  caption: '적용 정책: P02-06 RP-03, RP-06~09, TGT, NEX, AEX',
  draw,
};
