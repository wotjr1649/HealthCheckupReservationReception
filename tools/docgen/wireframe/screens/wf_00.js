'use strict';
// WF-00 MainForm Shell — 원문 03 §4
const S = require('../spec');
const K = require('../kit');

function draw(c) {
  const v = K.shell(c, {
    navActive: 2,
    ribbon: [
      { name: '검색', buttons: [{ t: '조회' }] },
      { name: '현재 업무 Action', buttons: [{ t: '예약변경' }, { t: '예약취소' }, { t: '접수' }] },
      { name: '보기', buttons: [{ t: '컬럼설정' }] },
    ],
    tabs: ['수검자 관리', '신규 예약', '예약 관리'], tabActive: 2,
    marks: { nav: '1', ribbon: '2', status: '5' },
  });

  // 업무 Tab 스트립 콜아웃
  c.markLeft(v.x0, v.y - S.CHROME.tab, S.CHROME.tab, '3');

  // 현재 업무 View
  c.rect(v.x, v.y + 0.08, v.w, v.h - 0.16, { sw: S.W.panel, dash: 'dash', stroke: S.C.dim });
  c.text(v.x, v.y + v.h / 2 - 0.40, v.w, 0.30, '현재 선택된 업무 Tab의 View',
    { size: 12, bold: true, color: S.C.hint });
  c.text(v.x, v.y + v.h / 2 - 0.02, v.w, 0.26, '수검자 관리  /  신규 예약  /  예약·접수 공통 Workbench',
    { size: S.TEXT.label, color: S.C.hint });
  c.text(v.x, v.y + v.h / 2 + 0.30, v.w, 0.26, '데이터 변경은 Main View에서 직접 편집하지 않고 Modal · Confirm으로 분리한다',
    { size: S.TEXT.small, color: S.C.dimText });
  c.markLeft(v.x, v.y + 0.08, v.h - 0.16, '4');
}

const desc = [
  { n: '', text: 'WF-00  MainForm Shell · MainForm · P00 · 공통' },
  { n: '1', text: '진입점 5개. 예약 관리·접수 관리는 같은 Tab을 공유하고 휴무일 관리는 Tab 없이 Modal을 연다' },
  { n: '2', text: 'Ribbon Group 순서는 검색 → 현재 업무 Action → 보기' },
  { n: '3', text: '동일 업무 Tab은 중복 생성하지 않는다 (Single Instance)' },
  { n: '4', text: '주요 Action은 하단이 아니라 Context Ribbon에 둔다' },
  { n: '5', text: '휴무일 · 운영시간 외(09:00~18:00)면 업무 불가로 표시한다. Action은 막지 않는다' },
  { n: '', text: '상태 표시는 안내값이며 저장 성공을 보장하지 않는다. 저장·상태전이는 Stored Procedure/Transaction이 현재 조건을 다시 검증한다.' },
];

module.exports = { title: 'MainForm 구조', draw, desc };
