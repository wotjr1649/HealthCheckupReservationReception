'use strict';
// 부록 D — Ribbon Action Matrix + 당일 Context 마감 (원문 03 §9.6 / §9.7 / §8.6 / §5.2 / §8.10)
const { tablePages } = require('../tablepage');

const pages = tablePages('부록 D.  Ribbon Action · 마감 Matrix', [
  {
    kind: 'note',
    lines: ['예약 관리와 접수 관리는 같은 Workbench 화면을 Context만 바꿔 공유한다. 두 Context의 Ribbon 구성이 다르므로 아래 두 표를 함께 읽어야 한다. Reception이 Reservation의 상위집합이 아니다 — [예약취소]는 예약 Context 전용이다.'],
  },
  {
    kind: 'table', caption: 'D-1.  예약 Context Ribbon  ·  [조회] [예약변경] [예약취소] [접수] [컬럼설정] (§9.6)',
    cols: [
      { t: '선택 상태', w: 2.60, align: 'left' },
      { t: '예약변경', flex: 1 }, { t: '예약취소', flex: 1 }, { t: '접수', flex: 1 },
    ],
    rows: [
      ['미선택', 'X', 'X', 'X'],
      ['예약 (RSV)', 'O', 'O', '조건부 O'],
      ['접수완료 (RCP)', 'X', 'X', 'X'],
      ['예약취소 (CNR)', 'X', 'X', 'X'],
      ['접수취소 (CNC)', 'X', 'X', 'X'],
      ['공통 업무불가', 'X', 'X', 'X'],
    ],
  },
  {
    kind: 'note',
    lines: ['[접수]는 상태변경 버튼이 아니라 접수 Context / P03으로 연결하는 Shortcut이다.'],
  },
  {
    kind: 'table', caption: 'D-2.  접수 Context Ribbon  ·  [조회] [현장 당일예약] [예약변경] [접수] [추가검사변경] [접수취소] [컬럼설정] (§9.7)',
    cols: [
      { t: '선택 상태', w: 2.60, align: 'left' },
      { t: '현장 당일예약', flex: 1 }, { t: '예약변경', flex: 1 }, { t: '접수', flex: 1 },
      { t: '추가검사변경', flex: 1 }, { t: '접수취소', flex: 1 },
    ],
    rows: [
      ['미선택', 'O', 'X', 'X', 'X', 'X'],
      ['예약 (RSV)', 'O', 'O', '조건부 O', 'X', 'X'],
      ['접수완료 (RCP)', 'O', 'X', 'X', 'O', 'O'],
      ['예약취소 (CNR)', 'O', 'X', 'X', 'X', 'X'],
      ['접수취소 (CNC)', 'O', 'X', 'X', 'X', 'X'],
      ['공통 업무불가', 'X', 'X', 'X', 'X', 'X'],
    ],
  },
  {
    kind: 'note',
    lines: ['[현장 당일예약]은 선택행과 무관한 독립 Action이다. 이 버튼 하나가 「수검자 확정 → 당일 업무 확인 → 신규예약 WalkIn(예약일 = 오늘 ReadOnly) → Workbench 자동선택 → 접수 처리」 흐름의 유일한 진입점이다 (§9.8).'],
  },
  {
    kind: 'table', caption: 'D-3.  수검자 관리 Ribbon (§5.2)',
    cols: [
      { t: '선택 상태', w: 2.60, align: 'left' },
      { t: '조회', flex: 1 }, { t: '신규등록', flex: 1 }, { t: '정보수정', flex: 1 },
      { t: '신규예약', flex: 1 }, { t: '컬럼설정', flex: 1 },
    ],
    rows: [
      ['행 미선택', 'O', 'O', 'X', 'X', 'O'],
      ['행 선택', 'O', 'O', 'O', 'O', 'O'],
      ['공통 업무불가', 'X', 'X', 'X', 'X', 'O'],
    ],
  },
  {
    kind: 'table', caption: 'D-4.  당일 예약 · 접수 마감 (§8.6)',
    cols: [{ t: '구분', w: 3.00, align: 'left' }, { t: '오전 (AM)', flex: 1 }, { t: '오후 (PM)', flex: 1 }],
    rows: [
      ['일반 당일예약  ·  평일', '10:00 전', '15:00 전'],
      ['현장 당일예약(WalkIn)  ·  평일', '11:00 전', '16:00 전'],
      ['일반 당일예약  ·  토요일', '10:00 전', '불가'],
      ['현장 당일예약(WalkIn)  ·  토요일', '11:00 전', '불가'],
    ],
  },
  {
    kind: 'note',
    lines: ['마감시각 이전(<)만 허용하며 마감시각과 같은 시각부터 불가하다. 모든 마감은 운영시간(09:00 ≤ 현재시각 < 18:00) 안에서만 적용한다.'],
  },
  {
    kind: 'table', caption: 'D-5.  신규 예약 Single Instance · Reset (§8.10)',
    cols: [
      { t: '이벤트', w: 2.20, align: 'left' }, { t: '예약일/시간대', flex: 1, align: 'left' },
      { t: 'TGT', flex: 1, align: 'left' }, { t: 'NEX', flex: 1, align: 'left' },
      { t: 'AEX', flex: 1, align: 'left' }, { t: '저장상태', flex: 1, align: 'left' },
    ],
    rows: [
      ['PatientId 변경', 'Clear', '미판정', 'Clear', 'Clear / Disabled', 'Disabled'],
      ['Context 전환', '기본값 Reset', '미판정', 'Clear', 'Clear / Disabled', 'Disabled'],
      ['예약일 변경', '새 날짜, 시간대 재선택', '재판정', '재구성', '유효 선택만 유지', '재평가'],
      ['시간대만 변경', '새 시간대', '유지', '유지', '유지', '일정검증 후 재평가'],
      ['저장 성공', '전체 Clear', '미판정', 'Clear', 'Clear / Disabled', 'Disabled'],
    ],
  },
  {
    kind: 'note',
    lines: ['미저장 내용이 있는 상태에서 다른 PatientId·Context로 재호출하거나 Tab을 닫으면 폐기 확인창을 띄운다 — 「미저장 예약 내용이 있습니다. 현재 입력을 폐기하고 새 업무를 시작하시겠습니까?  [확인] [취소]」'],
  },
], { size: 8.5, pad: 0.035 });

module.exports = { pages };
