'use strict';
// CNF-RSV-01 예약 취소 확인 — 원문 03 §13.1. 문구는 원문 그대로 인용한다.
const S = require('../spec');
const K = require('../kit');

function draw(c) {
  const F = S.PAGE.frame;
  // 부모 화면 (비활성)
  c.rect(F.x, F.y, F.w, F.h, { stroke: S.C.parent, sw: S.W.panel });
  c.rect(F.x, F.y, F.w, 0.28, { stroke: S.C.parent, sw: S.W.hair, fill: S.C.band });
  c.text(F.x + 0.14, F.y, 7.0, 0.28, '예약/접수 공통 Workbench (WF-WRK-01)  ·  예약 Context   —   비활성(Confirm)',
    { size: S.TEXT.small, align: 'left', color: S.C.dimText });
  // 호출 지점 표시
  c.box(F.x + 0.30, F.y + 0.52, 1.15, 0.30, '예약취소', { stroke: S.C.parent, color: S.C.dimText, sw: S.W.hair });
  c.text(F.x + 1.55, F.y + 0.52, 3.2, 0.30, '← 선택행 상태 = 예약(RSV) 일 때만 활성',
    { size: S.TEXT.small, align: 'left', color: S.C.dimText });
  c.markLeft(F.x + 0.30, F.y + 0.52, 0.30, '1');

  const w = 5.30, h = 1.90;
  const x = F.x + (F.w - w) / 2, y = F.y + (F.h - h) / 2;
  K.confirm(c, x, y, w, h, {
    title: '예약 취소',
    lines: ['선택한 예약을 취소하시겠습니까?', '취소 후 기존 예약으로 복원할 수 없습니다.'],
    buttons: [{ t: '확인', primary: true }, { t: '닫기' }],
  });
  c.markLeft(x, y + 0.30, 0.30, '2');
  c.markLeft(x + w - 2.10, y + h - 0.42, 0.28, '3');
}

const desc = [
  { n: '', text: 'CNF-RSV-01  예약 취소 확인 · Confirm · P02-07 · F-RSV-003' },
  { n: '1', text: '예약(RSV) 상태에서만 호출된다 (RP-10)' },
  { n: '2', text: '문구는 원문 확정본을 그대로 사용한다' },
  { n: '3', text: '[확인] 시 DB에서 상태·RowVersion을 재확인한 후 CNR로 전이' },
  { n: '', text: '동일 업무 행을 취소 상태로 바꾸며 Work와 검사 Detail을 물리 삭제하지 않는다 (CP-05).' },
  { n: '', text: '취소 상태에서 복원하지 않는다. 재진행은 신규 예약을 생성한다.' },
];

module.exports = { title: '예약 취소 확인', draw, desc };
