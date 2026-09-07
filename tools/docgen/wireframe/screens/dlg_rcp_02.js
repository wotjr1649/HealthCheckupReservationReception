'use strict';
// DLG-RCP-02 접수완료 추가검사 변경 (Modal) — 원문 03 §12. AEX만 Editable.
const S = require('../spec');
const K = require('../kit');

const NEX = [
  ['문진/진찰', '기본'], ['신체계측', '기본'], ['혈압', '기본'], ['시력·청력', '기본'],
  ['흉부 X-ray', '기본'], ['요검사', '기본'], ['혈액검사', '기본'], ['구강검진', '기본'],
  ['이상지질혈증', '조건부'], ['골밀도검사', '조건부'],
];
const AEX = [
  ['☑', '복부초음파', ''], ['☑', '갑상선초음파', ''], ['☐', '유방초음파', ''],
  { cells: ['✕', '골밀도검사', '일반건강검진에 포함된 검사입니다.'], dim: true },
  { cells: ['✕', 'PSA', '성별 조건 불충족'], dim: true },
  ['☐', 'HbA1c', ''], ['☐', 'HPV 검사', ''],
];

function draw(c) {
  const m = K.modal(c, {
    w: 8.60, h: 5.10, title: '추가검사 변경',
    buttons: [{ t: '저장', primary: true }, { t: '닫기' }],
    parent: '예약/접수 공통 Workbench (WF-WRK-01)  ·  접수 Context',
  });
  const i = m.inner;
  const g = 0.10, topH = 1.02, gap = 0.06;
  const botH = i.h - topH - gap;

  const patW = (i.w - g) * 0.45, rsvW = (i.w - g) * 0.55;
  const p = K.panel(c, i.x, i.y, patW, topH, '수검자 정보 · ReadOnly');
  K.field(c, p.x, p.y + 0.02, 1.10, p.w - 1.10, '차트번호 / 이름', '2026-000123 / 홍길동', { readonly: true });
  K.field(c, p.x, p.y + 0.32, 1.10, p.w - 1.10, '생년월일 / 성별', '1966-03-12  /  여', { readonly: true });

  const sx = i.x + patW + g;
  const q = K.panel(c, sx, i.y, rsvW, topH, '예약 정보 · ReadOnly');
  K.field(c, q.x, q.y + 0.02, 1.10, q.w - 1.10, '예약일 / 시간대', '2026-09-15 (화)   /   오전', { readonly: true });
  K.field(c, q.x, q.y + 0.32, 1.10, q.w - 1.10, '현재 상태', '접수완료 (RCP)', { readonly: true });
  c.markLeft(q.x + 1.10, q.y + 0.32, K.FIELD_H, '1');

  const bY = i.y + topH + gap;
  const nexW = (i.w - g) * 0.52, aexW = (i.w - g) * 0.48;
  const n = K.panel(c, i.x, bY, nexW, botH, 'NEX · ReadOnly');
  K.grid(c, n.x, n.y, K.cols(n.w, [{ t: '검사명', flex: 1, align: 'left' }, { t: '구분', w: 1.05 }]), NEX);
  c.markLeft(i.x, bY, botH, '2');

  const ax = i.x + nexW + g;
  const a = K.panel(c, ax, bY, aexW, botH, 'AEX · Editable');
  K.grid(c, a.x, a.y, K.cols(a.w, [
    { t: '선택', w: 0.44 }, { t: '검사명', w: 1.10, align: 'left' }, { t: '선택불가 사유', flex: 1, align: 'left', size: 8 },
  ]), AEX);
  c.markLeft(ax, bY, botH, '3');
  c.markLeft(m.buttons[0].x, m.buttons[0].y, m.buttons[0].h, '4');
}

const desc = [
  { n: '', text: 'DLG-RCP-02  접수완료 추가검사 변경 · Modal · P03-05 · F-RCP-002, F-COM-004' },
  { n: '1', text: '접수완료(RCP) 상태에서만 진입한다 (RCP-05)' },
  { n: '2', text: '예약일 · 시간대 · NEX는 변경할 수 없다 (RCP-05)' },
  { n: '3', text: '예약 상태와 동일한 AEX-01~05 Rule을 적용한다' },
  { n: '4', text: '실제 변경 시 상태는 RCP를 유지하고 Work RowVersion만 갱신' },
  { n: '', text: 'AEX 선택집합이 동일하면 Detail과 Work를 갱신하지 않고 No-op으로 처리한다 (§12, §16).' },
  { n: '', text: '가격·할인·수납·결제는 이 시스템의 범위가 아니다 (AEX-05).' },
];

module.exports = { title: '추가검사 변경', draw, desc };
