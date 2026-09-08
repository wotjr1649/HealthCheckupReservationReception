'use strict';
// DLG-PAT-02 수검자 선택 (Modal) — 원문 03 §7
const S = require('../spec');
const K = require('../kit');

function draw(c) {
  const m = K.modal(c, {
    w: 7.60, h: 4.30, title: '수검자 선택',
    buttons: [{ t: '선택', primary: true }, { t: '닫기' }],
    parent: '신규 예약 (WF-RSV-01)  ·  접수 관리 (WF-WRK-01)',
  });
  const i = m.inner;

  const sb = K.searchBand(c, i.x, i.y, i.w, [
    [{ label: '차트번호', w: 1.00 }, { label: '이름', lw: 0.50, w: 0.82 }, { label: '주민번호', w: 1.12 }],
    [{ label: '생년월일', w: 1.15 }, { label: '휴대전화', w: 1.35 }],
  ], { buttons: ['조회', '신규등록'] });
  c.markLeft(i.x, i.y, 0.94, '1');
  c.markLeft(i.x + i.w - 1.05, i.y + 0.30, K.FIELD_H, '2');

  const gy = sb.bottom + 0.08;
  const gp = K.panel(c, i.x, gy, i.w, i.y + i.h - gy - 0.04, '조회 결과');
  K.grid(c, gp.x, gp.y, K.cols(gp.w, [
    { t: '차트번호', w: 1.15, align: 'left' }, { t: '이름', w: 0.80 },
    { t: '주민번호', w: 1.35 }, { t: '생년월일', w: 1.00 }, { t: '성별', w: 0.50 }, { t: '휴대전화', flex: 1 },
  ]), [
    ['2026-000123', '홍길동', '660312-2000019', '1966-03-12', '여', '010-0000-0003'],
    ['2026-000121', '수검자1', '800511-1000015', '1980-05-11', '남', '010-0000-0001'],
  ]);
  c.rect(gp.x, gp.y + K.ROW, gp.w, K.ROW, { stroke: S.C.ink, sw: S.W.panel });
  c.markLeft(i.x, gy, i.y + i.h - gy - 0.04, '3');
  c.markLeft(m.buttons[0].x, m.buttons[0].y, m.buttons[0].h, '4');
}

const desc = [
  { n: '', text: 'DLG-PAT-02  수검자 선택 · Modal · P01-01~03 · F-PAT-001~002' },
  { n: '1', text: '조회계약은 수검자 관리 화면과 동일하다' },
  { n: '2', text: '결과가 없으면 [신규등록]으로 DLG-PAT-01 New Mode 진입' },
  { n: '', text: '신규저장 또는 동일 기존수검자 확정 후 재검색을 요구하지 않고 PatientId를 즉시 호출 화면에 반환한다.' },
  { n: '3', text: '단일 행 선택. 주민등록번호는 전체값을 표시한다' },
  { n: '4', text: '[선택]으로 PatientId를 호출 화면에 반환한다' },
  { n: '', text: 'DLG-PAT-01을 취소해도 이 창은 유지된다.' },
];

module.exports = { title: '수검자 선택', draw, desc };
