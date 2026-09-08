'use strict';
// WF-RSV-01 신규 예약 (Main Tab) — 원문 03 §8
// 대표 상태: 수검자 확정 + 예약일/시간대 확정 + TGT 대상.
// 여성 만 60세 → 조건부 2종(이상지질혈증·골밀도) 성립. 골밀도가 NEX에 포함되므로
// AEX-03(국가검진 포함)과 AEX-02(성별) 두 비활성 사유가 한 화면에 모두 보인다.
const S = require('../spec');
const K = require('../kit');

const NEX = [
  ['문진/진찰', '기본'], ['신체계측', '기본'], ['혈압', '기본'], ['시력·청력', '기본'],
  ['흉부 X-ray', '기본'], ['요검사', '기본'], ['혈액검사', '기본'], ['구강검진', '기본'],
  ['이상지질혈증', '조건부'], ['골밀도검사', '조건부'],
];

const AEX = [
  ['☐', '복부초음파', ''],
  ['☑', '갑상선초음파', ''],
  ['☐', '유방초음파', ''],
  { cells: ['✕', '골밀도검사', '일반건강검진에 포함된 검사입니다.'], dim: true },
  { cells: ['✕', 'PSA', '성별 조건 불충족'], dim: true },
  ['☐', 'HbA1c', ''],
  ['☐', 'HPV 검사', ''],
];

function draw(c) {
  const v = K.shell(c, {
    nav: ['수검자 관리', '신규 예약', '예약 관리', '접수 관리'], navActive: 1,
    ribbon: [{ name: '예약', buttons: [{ t: '예약저장', bold: true, mark: '2' }] }],
    tabs: ['수검자 관리', '신규 예약'], tabActive: 1,
    marks: { nav: '1', status: '9' },
  });

  const gap = 0.06, g = 0.10;
  const topH = 1.38, tgtH = 0.30;
  const botH = v.h - topH - tgtH - gap * 3;
  const tY = v.y + gap;
  const patW = (v.w - g) * 0.45, schW = (v.w - g) * 0.55;

  // 수검자 정보 (ReadOnly)
  const p = K.panel(c, v.x, tY, patW, topH, '수검자 정보');
  const fw = (p.w - 0.06) / 2;
  [['차트번호', '2026-000123'], ['이름', '홍길동'], ['생년월일', '1966-03-12'], ['성별', '여']]
    .forEach(([k, val], i) => {
      const r = Math.floor(i / 2), col = i % 2;
      K.field(c, p.x + col * (fw + 0.06), p.y + 0.04 + r * 0.30, 0.64, fw - 0.64, k, val, { readonly: true });
    });
  c.box(p.x, p.y + 0.71, 1.05, 0.28, '수검자 선택', { sw: S.W.panel });
  c.markLeft(v.x, tY, topH, '3');

  // 예약 일정
  const sx = v.x + patW + g;
  const q = K.panel(c, sx, tY, schW, topH, '예약 일정');
  K.field(c, q.x, q.y + 0.04, 0.68, 1.55, '예약일', '2026-09-15 (화)');
  c.box(q.x + 2.25, q.y + 0.04, 0.26, K.FIELD_H, '▦', { size: S.TEXT.small });
  c.markLeft(q.x + 0.68, q.y + 0.04, K.FIELD_H, '4');

  const slotY = q.y + 0.38;
  c.text(q.x, slotY, 0.68, K.FIELD_H, '시간대', { size: S.TEXT.small, align: 'left', color: S.C.hint });
  [['◉  오전', '12 / 20', false], ['○  오후', '20 / 20      마감', true]].forEach(([n, cap, dim], i) => {
    const ry = slotY + i * 0.30;
    c.box(q.x + 0.68, ry, 0.98, K.FIELD_H, n, { size: S.TEXT.gridData, dim, color: dim ? S.C.dimText : S.C.data });
    c.text(q.x + 1.76, ry, 1.80, K.FIELD_H, cap, { size: S.TEXT.gridData, align: 'left', color: dim ? S.C.dimText : S.C.data });
  });
  c.markLeft(q.x + 0.68, slotY, 0.54, '5');

  // TGT 밴드
  const gY = tY + topH + gap;
  c.rect(v.x, gY, v.w, tgtH, { sw: S.W.panel });
  c.text(v.x + 0.14, gY, 6.0, tgtH, '대상판정 :   대상 — 최근 완료연도 2024',
    { size: S.TEXT.gridData, align: 'left', bold: true, color: S.C.data });
  c.markLeft(v.x, gY, tgtH, '6');

  // 하단: NEX 55 : AEX 45 (§8.4)
  const bY = gY + tgtH + gap;
  const nexW = (v.w - g) * 0.55, aexW = (v.w - g) * 0.45;

  const n = K.panel(c, v.x, bY, nexW, botH, '국가검진 검사항목 (NEX) · ReadOnly');
  K.grid(c, n.x, n.y, K.cols(n.w, [{ t: '검사명', flex: 1, align: 'left' }, { t: '구분', w: 1.20 }]), NEX);
  c.markLeft(v.x, bY, botH, '7');

  const ax = v.x + nexW + g;
  const a = K.panel(c, ax, bY, aexW, botH, '추가검사 (AEX) · 본인부담 · 선택');
  K.grid(c, a.x, a.y, K.cols(a.w, [
    { t: '선택', w: 0.44 }, { t: '검사명', w: 1.12, align: 'left' }, { t: '선택불가 사유', flex: 1, align: 'left', size: 8 },
  ]), AEX);
  c.markLeft(ax, bY, botH, '8');
}

const desc = [
  { n: '', text: 'WF-RSV-01  신규 예약 · Main Tab · P02-01~04 · F-RSV-001' },
  { n: '1', text: '상단 업무 Navigation. 각 업무 Tab은 1개만 열린다' },
  { n: '2', text: '화면 준비상태 충족 시 활성. 저장 후 DB가 최종 재검증' },
  { n: '3', text: '[수검자 선택]으로 확정. 전달받은 경우 즉시 표시 (ReadOnly)' },
  { n: '4', text: '과거일·일요일·휴무일은 선택 불가 (CP-01~03, HOL, RP-04)' },
  { n: '5', text: '정원 20 도달 시 선택 불가. 취소 건은 산정 제외 (RP-03)' },
  { n: '6', text: '확정 예약일 기준 판정. 비대상이면 저장 불가 (TGT, RP-07)' },
  { n: '7', text: '기본 8종 + 조건부 0~3종 = 8~11행. 사용자 변경 불가 (NEX)' },
  { n: '8', text: '0개 이상 선택. 성별 불충족·NEX 중복은 비활성 (AEX-02~04)' },
  { n: '9', text: '휴무일·운영시간 외에는 업무 Action 전체 비활성 (CP-04)' },
];

module.exports = { title: '신규 예약', draw, desc };
