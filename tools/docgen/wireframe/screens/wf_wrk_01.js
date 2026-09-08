'use strict';
// WF-WRK-01 예약/접수 공통 Workbench (Main Tab) — 원문 03 §9
// Ribbon은 Tab Caption과 같은 Context 하나만 그린다. 여기 Tab은 「접수 관리」이므로 §9.7 Reception 구성이다.
// 두 Context는 상하위 관계가 아니다 — [예약취소]는 §9.6 Reservation에만, [현장 당일예약]·[추가검사변경]·
// [접수취소]는 §9.7 Reception에만 있다. 합집합을 한 그림에 그리면 Tab과 어긋난 Ribbon을 보여주게 된다.
const S = require('../spec');
const K = require('../kit');

function draw(c) {
  const v = K.shell(c, {
    navActive: 3,
    ribbon: [
      { name: '검색', buttons: [{ t: '조회' }] },
      { name: '접수 업무', buttons: [{ t: '현장 당일예약' }, { t: '예약변경' }, { t: '접수' }, { t: '추가검사변경' }, { t: '접수취소' }] },
      { name: '보기', buttons: [{ t: '변경이력' }, { t: '컬럼설정' }] },
    ],
    tabs: ['접수 관리'], tabActive: 0,
    marks: { nav: '1', ribbon: '2', status: '8' },
  });

  const gap = 0.06;
  const sb = K.searchBand(c, v.x, v.y + gap, v.w, [[
    { label: '예약/접수일', lw: 0.80, w: 0.95 }, { label: '~', lw: 0.14, w: 0.95 },
    { label: '상태', lw: 0.44, w: 0.88, value: '전체' },
    { label: '차트번호', w: 1.00 }, { label: '이름', lw: 0.50, w: 0.80 },
  ]], { buttons: ['조회'] });
  c.markLeft(v.x, v.y + gap, 0.62, '3');

  const bodyY = sb.bottom + gap;
  const bodyH = v.y + v.h - bodyY - 0.04;
  const g = 0.10, gw = (v.w - g) * 0.60, dw = (v.w - g) * 0.40;

  // 좌: 업무내역 Grid (§9.4)
  const gp = K.panel(c, v.x, bodyY, gw, bodyH, '예약 · 접수 업무내역');
  K.grid(c, gp.x, gp.y, K.cols(gp.w, [
    { t: '예약/접수일', w: 1.20 }, { t: '시간대', w: 0.75 }, { t: '상태', w: 0.90 },
    { t: '이름', w: 0.85 }, { t: '차트번호', flex: 1 },
  ]), [
    ['2026-09-15', '오전', '예약', '수검자1', '2026-000121'],
    ['2026-09-15', '오전', '접수완료', '홍길동', '2026-000123'],
    { cells: ['2026-09-15', '오후', '예약취소', '수검자4', '2026-000124'], dim: true },
    ['2026-09-16', '오전', '예약', '수검자2', '2026-000122'],
  ]);
  c.rect(gp.x, gp.y + K.ROW * 2, gp.w, K.ROW, { stroke: S.C.ink, sw: S.W.panel });
  c.markLeft(v.x, bodyY, bodyH, '4');

  // 우: 상세 (ReadOnly)
  const dx = v.x + gw + g;
  const dp = K.panel(c, dx, bodyY, dw, bodyH, '업무 상세 · ReadOnly');
  let y = dp.y;
  const sect = (t) => { c.text(dp.x, y, dp.w, 0.19, '[ ' + t + ' ]', { size: S.TEXT.small, align: 'left', bold: true, color: S.C.hint }); y += 0.20; };
  const row = (k, val) => { K.field(c, dp.x, y, 1.10, dp.w - 1.10, k, val, { readonly: true }); y += 0.26; };
  sect('수검자');
  row('차트번호 / 이름', '2026-000123 / 홍길동');
  row('생년월일 / 성별', '1966-03-12  /  여');
  sect('예약');
  row('예약일 / 시간대', '2026-09-15 (화)  /  오전');
  const capY = y; row('현재 정원 / 상태', '12 / 20     ·     접수완료');
  sect('NEX  ·  실제 저장 구성');
  const nexY = y;
  c.rect(dp.x, y, dp.w, 0.62, { stroke: S.C.dim, fill: S.C.band });
  c.text(dp.x + 0.08, y + 0.02, dp.w - 0.16, 0.58, '', {
    size: S.TEXT.small, align: 'left', valign: 'top', color: S.C.data,
    lines: ['문진/진찰 · 신체계측 · 혈압 · 시력·청력 · 흉부 X-ray', '요검사 · 혈액검사 · 구강검진 · 이상지질혈증', '골밀도검사        (10종)'],
  });
  y += 0.66;
  sect('AEX  ·  실제 저장 구성');
  const aexH = Math.max(0.30, dp.y + dp.h - y - 0.02);
  c.rect(dp.x, y, dp.w, aexH, { stroke: S.C.dim, fill: S.C.band });
  c.text(dp.x + 0.08, y + 0.02, dp.w - 0.16, 0.30, '갑상선초음파        (1종)', { size: S.TEXT.small, align: 'left', valign: 'top', color: S.C.data });
  c.markLeft(dx, bodyY, 0.60, '5');
  c.markLeft(dp.x + 1.10, capY, K.FIELD_H, '6');
  c.markLeft(dp.x, nexY, 0.62, '7');
}

const desc = [
  { n: '', text: 'WF-WRK-01  예약/접수 공통 Workbench · Main Tab · P02-05, P03-04 · F-COM-001' },
  { n: '1', text: '[예약 관리]·[접수 관리]가 같은 Tab을 Context만 바꿔 공유한다' },
  { n: '2', text: 'Tab Caption과 같은 접수 Context 구성이다' },
  { n: '', text: '예약 Context는 [현장 당일예약]·[추가검사변경]·[접수취소] 없이 [예약취소]가 대신 있다. [변경이력]은 두 Context 공통이며 조회 Action이라 공통 업무불가에도 열린다. 상태별 활성/비활성은 부록 D 참조. 접수 Context 미선택 상태에서 활성인 것은 [조회]·[컬럼설정]·[현장 당일예약]뿐이다.' },
  { n: '3', text: '최소 1개 실질 조건 필요. 상태 `전체`만은 조건으로 보지 않는다' },
  { n: '4', text: '날짜는 양끝 포함. From>To는 Inline 오류. 시간대는 조회조건 제외' },
  { n: '5', text: '단일 행 선택. 재조회 시 선택·상세·Action을 모두 초기화' },
  { n: '6', text: '정원은 현재 조회값이며 저장 Snapshot 컬럼이 아니다' },
  { n: '7', text: 'NEX/AEX는 실제 저장된 구성. 이 화면에서는 ReadOnly' },
  { n: '8', text: 'WorkId·PatientId·주민등록번호는 이 Grid에 노출하지 않는다' },
];

module.exports = { title: '예약 · 접수 공통 Workbench', draw, desc };
