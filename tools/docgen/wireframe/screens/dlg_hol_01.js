'use strict';
// DLG-HOL-01 휴무일 관리 (Modal) — 원문 03 §24.
// 업무 Tab 이 아니라 기준정보 관리다. 상단 Navigation 에서 바로 열린다.
// 법정·대체 공휴일은 보여주기만 하고 자체휴무일만 편집한다 (00 HOL-05).
const S = require('../spec');
const K = require('../kit');

// §24.3 스케치의 행. 값은 예시이며 계약이 아니다.
// dim 행이 법정·대체 공휴일 — 회색으로 그려 편집 대상이 아님을 보인다.
const ROWS = [
  { cells: ['2026-12-25', '성탄절', '법정공휴일', 'Y', ''], dim: true },
  ['2026-12-26', '센터 휴진일', '자체휴무일', 'Y', '정기 휴진'],
  { cells: ['2027-01-01', '신정', '법정공휴일', 'Y', ''], dim: true },
];

// SP-HOL-01 RS1 그대로 — HolidayDate · HolidayName · HolidayType · IsActive · Memo.
const COLS = [
  { t: '휴무일자', w: 1.20 },
  { t: '휴무일명', w: 1.90, align: 'left' },
  { t: '휴무구분', w: 1.20 },
  { t: '사용여부', w: 0.90 },
  { t: '비고', flex: 1, align: 'left' },
];

function draw(c) {
  const m = K.modal(c, {
    w: 8.60, h: 4.10, title: '휴무일 관리',
    buttons: [{ t: '닫기', primary: true }],
    parent: 'MainForm 상단 Navigation [휴무일 관리] (WF-00)',
  });
  const i = m.inner;

  // 조회조건 — 기본 기간은 업무정책이 정한 공휴일 등재 범위다.
  const sb = K.searchBand(c, i.x, i.y, i.w, [[
    { label: '조회기간', w: 1.05, value: '2026-01-01' },
    { label: '~', lw: 0.16, w: 1.05, value: '2027-12-31' },
    { label: '구분', lw: 0.42, w: 1.10, value: '전체' },
  ]], { buttons: ['조회'] });
  c.markLeft(i.x, i.y, sb.bottom - i.y, '1');

  // 만료 경고 띠 — 잔여가 임계 미만일 때만 나타난다. 편집을 막지 않는다.
  const wY = sb.bottom + 0.10, wH = 0.30;
  c.rect(i.x, wY, i.w, wH, { fill: S.C.band, sw: S.W.panel });
  c.text(i.x + 0.14, wY, i.w - 0.28, wH,
    '!   공휴일 등재가 2027-12-31 에 끝납니다.  남은 기간 480일 — 갱신이 필요합니다',
    { size: S.TEXT.label, align: 'left', bold: true, color: S.C.data });
  c.markLeft(i.x, wY, wH, '2');

  // 목록
  const gY = wY + wH + 0.12, gH = 1.42;
  const p = K.panel(c, i.x, gY, i.w, gH, '휴무일 목록  ·  휴무일자 오름차순 고정');
  const cols = K.cols(p.w, COLS);
  K.grid(c, p.x, p.y, cols, ROWS);
  c.markLeft(i.x, gY, gH, '3');
  c.markLeft(p.x + cols[0].w + cols[1].w, p.y, K.ROW, '4');

  // 입력행 — 자체휴무일 전용. 법정·대체 행을 고르면 통째로 Disabled 된다.
  const eY = gY + gH + 0.12, eH = 1.00;
  const e = K.panel(c, i.x, eY, i.w, eH, '자체휴무일 입력  ·  법정·대체 공휴일 행을 고르면 비활성');
  let fx = e.x;
  fx = K.field(c, fx, e.y, 0.68, 1.15, '휴무일자', '2027-03-14') + 0.18;
  fx = K.field(c, fx, e.y, 0.68, 2.05, '휴무일명', '센터 정기 휴진일') + 0.18;
  K.field(c, fx, e.y, 0.68, 0.55, '사용여부', 'Y');
  K.field(c, e.x, e.y + 0.34, 0.68, 4.55, '비고', '설비 점검');

  let bx = e.x + e.w - 0.14;
  ['삭제', '수정', '추가'].forEach(t => {
    bx -= 0.86;
    c.box(bx, e.y + 0.34, 0.86, K.FIELD_H + 0.02, t, { sw: S.W.panel });
    bx -= 0.08;
  });
  c.markLeft(i.x, eY, eH, '5');
}

const desc = [
  { n: '', text: 'DLG-HOL-01  휴무일 관리 · Modal · F-COM-009' },
  { n: '1', text: '기본 기간은 업무정책이 정한 공휴일 등재 범위다' },
  { n: '2', text: '잔여가 임계 미만일 때만 나타난다. 편집을 막지 않는다' },
  { n: '3', text: 'SP-HOL-01 RS1 그대로. 일요일은 목록에 없다' },
  { n: '4', text: '법정공휴일 · 대체공휴일 · 자체휴무일 세 값이다' },
  { n: '5', text: '추가하면 휴무구분은 항상 자체휴무일이다' },
  { n: '', text: '법정 · 대체 공휴일 행은 회색이며 선택해도 입력행에 싣지 않는다.' },
  { n: '', text: '수정은 휴무일명 · 사용여부 · 비고만 바꾼다. 휴무일자는 바꾸지 않는다.' },
  { n: '', text: '삭제는 물리 삭제다. 사용여부 0 은 일시 무효화이며 날짜를 계속 점유한다.' },
  { n: '', text: '날짜 중복과 공백 휴무일명의 최종 판단은 저장 프로시저가 한다.' },
  { n: '', text: '업무 Tab 을 열지 않으므로 열려 있는 업무 화면의 상태를 바꾸지 않는다.' },
];

module.exports = { title: '휴무일 관리', draw, desc };
