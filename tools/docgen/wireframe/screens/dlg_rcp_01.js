'use strict';
// DLG-RCP-01 접수 처리 (Modal) — 원문 03 §11. 전 영역 ReadOnly.
const S = require('../spec');
const K = require('../kit');

const NEX = [
  ['문진/진찰', '기본'], ['신체계측', '기본'], ['혈압', '기본'], ['시력·청력', '기본'],
  ['흉부 X-ray', '기본'], ['요검사', '기본'], ['혈액검사', '기본'], ['구강검진', '기본'],
  ['이상지질혈증', '조건부'], ['골밀도검사', '조건부'],
];
const AEX = [['☑', '갑상선초음파']];

function draw(c) {
  const m = K.modal(c, {
    w: 8.60, h: 5.10, title: '접수 처리',
    buttons: [{ t: '접수처리', primary: true }, { t: '닫기' }],
    parent: '예약/접수 공통 Workbench (WF-WRK-01)  ·  접수 Context',
  });
  const i = m.inner;
  const g = 0.10, topH = 1.02, okH = 0.34, gap = 0.06;
  const botH = i.h - topH - okH - gap * 2;

  const patW = (i.w - g) * 0.45, rsvW = (i.w - g) * 0.55;
  const p = K.panel(c, i.x, i.y, patW, topH, '수검자 정보 · ReadOnly');
  K.field(c, p.x, p.y + 0.02, 1.10, p.w - 1.10, '차트번호 / 이름', '2026-000123 / 홍길동', { readonly: true });
  K.field(c, p.x, p.y + 0.32, 1.10, p.w - 1.10, '생년월일 / 성별', '1966-03-12  /  여', { readonly: true });
  c.markLeft(i.x, i.y, topH, '1');

  const sx = i.x + patW + g;
  const q = K.panel(c, sx, i.y, rsvW, topH, '예약 정보 · ReadOnly');
  K.field(c, q.x, q.y + 0.02, 1.10, q.w - 1.10, '예약일 / 시간대', '2026-09-15 (화)   /   오전', { readonly: true });
  K.field(c, q.x, q.y + 0.32, 1.10, q.w - 1.10, '현재 상태', '예약 (RSV)', { readonly: true });
  c.markLeft(i.x + patW + g, i.y, topH, '2');

  const bY = i.y + topH + gap;
  const nexW = (i.w - g) * 0.52, aexW = (i.w - g) * 0.48;
  const n = K.panel(c, i.x, bY, nexW, botH, 'NEX · ReadOnly');
  K.grid(c, n.x, n.y, K.cols(n.w, [{ t: '검사명', flex: 1, align: 'left' }, { t: '구분', w: 1.05 }]), NEX);

  const ax = i.x + nexW + g;
  const a = K.panel(c, ax, bY, aexW, botH, 'AEX · ReadOnly');
  K.grid(c, a.x, a.y, K.cols(a.w, [{ t: '선택', w: 0.44 }, { t: '검사명', flex: 1, align: 'left' }]), AEX);
  c.text(a.x, a.y + K.ROW * 2 + 0.10, a.w, 0.24, '접수 단계에서는 검사구성을 수정하지 않는다',
    { size: S.TEXT.small, align: 'left', color: S.C.dimText });
  c.markLeft(i.x, bY, botH, '3');
  c.markLeft(ax, bY, botH, '4');

  // 접수 가능 여부
  const oY = bY + botH + gap;
  c.rect(i.x, oY, i.w, okH, { sw: S.W.panel });
  c.text(i.x + 0.14, oY, i.w - 0.28, okH, '접수 가능 여부 :   가능', { size: S.TEXT.label, align: 'left', bold: true, color: S.C.data });
  c.markLeft(i.x, oY, okH, '5');
  c.markLeft(m.buttons[0].x, m.buttons[0].y, m.buttons[0].h, '6');
}

const desc = [
  { n: '', text: 'DLG-RCP-01  접수 처리 · Modal · P03-01~03 · F-RCP-001, F-COM-006' },
  { n: '1', text: '수검자 · 예약일 · 시간대 · NEX · AEX 전 영역이 ReadOnly' },
  { n: '2', text: '예약(RSV) 상태의 당일 업무만 접수 대상이 된다 (RCP-01~03)' },
  { n: '3', text: '접수 성공 시 예약일·시간대·검사구성은 그대로 유지된다 (RCP-04)' },
  { n: '4', text: '접수 전 AEX 변경이 필요하면 예약변경을 먼저 완료한다' },
  { n: '5', text: '상태=RSV · 예약일=DB 현재일 · 업무 가능일 · 09:00~18:00 · 해당 시간대 접수 마감 전 (RCP-02, CP-01~04, HOL)' },
  { n: '6', text: '불가 사유가 있으면 사유를 표시하고 [접수처리]를 비활성한다' },
  { n: '', text: '화면에 접수 가능으로 보였더라도 저장 시 Stored Procedure가 상태·시각·RowVersion을 다시 확인한다. 동시에 두 사용자가 접수해도 하나만 성공한다.' },
];

module.exports = { title: '접수 처리', draw, desc };
