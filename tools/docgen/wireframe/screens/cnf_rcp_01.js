'use strict';
// CNF-RCP-01 접수 취소 확인 — 원문 03 §13.2. 문구는 원문 그대로 인용한다.
const S = require('../spec');
const K = require('../kit');

function draw(c) {
  const F = S.PAGE.frame;
  c.rect(F.x, F.y, F.w, F.h, { stroke: S.C.parent, sw: S.W.panel });
  c.rect(F.x, F.y, F.w, 0.28, { stroke: S.C.parent, sw: S.W.hair, fill: S.C.band });
  c.text(F.x + 0.14, F.y, 7.0, 0.28, '예약/접수 공통 Workbench (WF-WRK-01)  ·  접수 Context   —   비활성(Confirm)',
    { size: S.TEXT.small, align: 'left', color: S.C.dimText });
  c.box(F.x + 0.30, F.y + 0.52, 1.15, 0.30, '접수취소', { stroke: S.C.parent, color: S.C.dimText, sw: S.W.hair });
  c.text(F.x + 1.55, F.y + 0.52, 3.6, 0.30, '← 선택행 상태 = 접수완료(RCP) 일 때만 활성',
    { size: S.TEXT.small, align: 'left', color: S.C.dimText });
  c.markLeft(F.x + 0.30, F.y + 0.52, 0.30, '1');

  const w = 5.90, h = 1.90;
  const x = F.x + (F.w - w) / 2, y = F.y + (F.h - h) / 2;
  K.confirm(c, x, y, w, h, {
    title: '접수 취소',
    lines: ['선택한 접수를 취소하시겠습니까?', '예약 상태로 되돌아가지 않으며 해당 업무 전체가 취소됩니다.'],
    buttons: [{ t: '확인', primary: true }, { t: '닫기' }],
  });
  c.markLeft(x, y + 0.30, 0.30, '2');
  c.markLeft(x + w - 2.10, y + h - 0.42, 0.28, '3');
}

const desc = [
  { n: '', text: 'CNF-RCP-01  접수 취소 확인 · Confirm · P03-06 · F-RCP-003' },
  { n: '1', text: '접수완료(RCP) 상태에서만 호출된다 (RCP-06)' },
  { n: '2', text: '문구는 원문 확정본을 그대로 사용한다' },
  { n: '3', text: '[확인] 시 DB에서 상태·RowVersion을 재확인한 후 CNC로 전이' },
  { n: '', text: '예약(RSV)으로 되돌리지 않는다. 해당 예약·접수 업무 전체가 취소된 것으로 판단한다 (RCP-06).' },
  { n: '', text: '취소 건은 보존·조회하되 후속 변경 Action을 허용하지 않는다 (CP-05).' },
];

module.exports = { title: '접수 취소 확인', draw, desc };
