'use strict';
// DLG-RSV-01 예약 변경 (Modal) — 원문 03 §10
const S = require('../spec');
const K = require('../kit');

const NEX = [
  ['문진/진찰', '기본'], ['신체계측', '기본'], ['혈압', '기본'], ['시력·청력', '기본'],
  ['흉부 X-ray', '기본'], ['요검사', '기본'], ['혈액검사', '기본'], ['구강검진', '기본'],
  ['이상지질혈증', '조건부'], ['골밀도검사', '조건부'],
];
const AEX = [
  ['☐', '복부초음파', ''], ['☑', '갑상선초음파', ''], ['☐', '유방초음파', ''],
  { cells: ['✕', '골밀도검사', '일반건강검진에 포함된 검사입니다.'], dim: true },
  { cells: ['✕', 'PSA', '성별 조건 불충족'], dim: true },
  ['☐', 'HbA1c', ''], ['☐', 'HPV 검사', ''],
];

function draw(c) {
  const m = K.modal(c, {
    w: 8.60, h: 5.30, title: '예약 변경',
    buttons: [{ t: '저장', primary: true }, { t: '닫기' }],
    parent: '예약/접수 공통 Workbench (WF-WRK-01)  ·  예약 Context',
  });
  const i = m.inner;
  const g = 0.10, topH = 1.02, tgtH = 0.28, gap = 0.06;
  const botH = i.h - topH - tgtH - gap * 2;

  // 수검자 (ReadOnly)
  const patW = (i.w - g) * 0.42, schW = (i.w - g) * 0.58;
  const p = K.panel(c, i.x, i.y, patW, topH, '수검자 정보 · ReadOnly');
  K.field(c, p.x, p.y + 0.02, 1.10, p.w - 1.10, '차트번호 / 이름', '2026-000123 / 홍길동', { readonly: true });
  K.field(c, p.x, p.y + 0.32, 1.10, p.w - 1.10, '생년월일 / 성별', '1966-03-12  /  여', { readonly: true });
  c.markLeft(i.x, i.y, topH, '1');

  // 예약일 / 시간대 (Editable)
  const sx = i.x + patW + g;
  const q = K.panel(c, sx, i.y, schW, topH, '예약일 · 시간대 · Editable');
  K.field(c, q.x, q.y + 0.02, 0.68, 1.50, '예약일', '2026-09-22 (화)');
  c.box(q.x + 2.20, q.y + 0.02, 0.26, K.FIELD_H, '▦', { size: S.TEXT.small });
  c.text(q.x, q.y + 0.32, 0.68, K.FIELD_H, '시간대', { size: S.TEXT.small, align: 'left', color: S.C.hint });
  c.box(q.x + 0.68, q.y + 0.32, 0.90, K.FIELD_H, '◉  오전', { size: S.TEXT.gridData, color: S.C.data });
  c.box(q.x + 1.62, q.y + 0.32, 0.90, K.FIELD_H, '○  오후', { size: S.TEXT.gridData, color: S.C.data });
  c.text(q.x + 2.60, q.y + 0.32, 1.60, K.FIELD_H, '8 / 20', { size: S.TEXT.gridData, align: 'left', color: S.C.data });
  c.markLeft(q.x + 0.68, q.y + 0.02, K.FIELD_H, '2');
  c.markLeft(q.x + 0.68, q.y + 0.32, K.FIELD_H, '3');

  // TGT
  const gY = i.y + topH + gap;
  c.rect(i.x, gY, i.w, tgtH, { sw: S.W.panel });
  c.text(i.x + 0.14, gY, 6.0, tgtH, '대상판정 :   대상 — 최근 완료연도 2024',
    { size: S.TEXT.gridData, align: 'left', bold: true, color: S.C.data });
  c.markLeft(i.x, gY, tgtH, '4');

  // NEX / AEX
  const bY = gY + tgtH + gap;
  const nexW = (i.w - g) * 0.52, aexW = (i.w - g) * 0.48;
  const n = K.panel(c, i.x, bY, nexW, botH, 'NEX · ReadOnly');
  K.grid(c, n.x, n.y, K.cols(n.w, [{ t: '검사명', flex: 1, align: 'left' }, { t: '구분', w: 1.05 }]), NEX);
  c.markLeft(i.x, bY, botH, '5');

  const ax = i.x + nexW + g;
  const a = K.panel(c, ax, bY, aexW, botH, 'AEX · Editable');
  K.grid(c, a.x, a.y, K.cols(a.w, [
    { t: '선택', w: 0.44 }, { t: '검사명', w: 1.10, align: 'left' }, { t: '선택불가 사유', flex: 1, align: 'left', size: 8 },
  ]), AEX);
  c.markLeft(ax, bY, botH, '6');
  c.markLeft(m.buttons[0].x, m.buttons[0].y, m.buttons[0].h, '7');
}

const desc = [
  { n: '', text: 'DLG-RSV-01  예약 변경 · Modal · P02-06 · F-RSV-002' },
  { n: '1', text: '예약(RSV) 상태에서만 진입한다. 수검자는 변경할 수 없다 (RP-09)' },
  { n: '2', text: '예약일 변경 → 일정 → TGT → NEX → AEX 순으로 전부 재평가' },
  { n: '3', text: '시간대만 변경 → 일정·마감·정원만 재평가. TGT/NEX/AEX는 유지' },
  { n: '4', text: '예약일 변경 후 비대상이면 저장하지 않고 기존 예약을 유지한다' },
  { n: '5', text: '예약일이 바뀌면 자동 재구성된다. 사용자 변경 불가 (NEX-07)' },
  { n: '6', text: 'AEX만 변경해도 Work의 RowVersion이 갱신된다' },
  { n: '7', text: '원본 RowVersion을 보관했다가 저장 시 함께 전달한다' },
  { n: '', text: '일정·중복 검증에서 현재 WorkId 자체는 제외하고 다른 RSV/RCP 업무만 충돌로 판단한다. 실제 변경이 없으면 No-op으로 처리한다.' },
];

module.exports = { title: '예약 변경', draw, desc };
