'use strict';
// 화면 목록 — 원문 03 §2 를 그대로 옮긴다(5열). 하단에 범례·전제 블록.
const { tablePages } = require('../tablepage');

const ROWS = [
  ['WF-00', 'MainForm Shell', 'MainForm', 'P00', '공통'],
  ['WF-PAT-01', '수검자 관리', 'Main Tab', 'P01-01~05', 'F-PAT-001~003, F-COM-002, F-COM-007'],
  ['DLG-PAT-01', '수검자 등록/수정', 'Modal', 'P01-02~05', 'F-PAT-002~003, F-COM-002, F-COM-007'],
  ['DLG-PAT-02', '수검자 선택', 'Modal', 'P01-01~03', 'F-PAT-001~002, F-COM-002, F-COM-007'],
  ['DLG-PAT-03', '중복 후보 확인', 'Modal', 'P01-03', 'F-COM-002'],
  ['WF-RSV-01', '신규 예약', 'Main Tab', 'P02-01~04', 'F-RSV-001, F-COM-003~005, F-COM-007'],
  ['WF-WRK-01', '예약/접수 공통 Workbench', 'Main Tab', 'P02-05, P03-04', 'F-COM-001, F-COM-007'],
  ['DLG-RSV-01', '예약 변경', 'Modal', 'P02-06', 'F-RSV-002, F-COM-003~005, F-COM-007'],
  ['CNF-RSV-01', '예약 취소 확인', 'Confirm', 'P02-07', 'F-RSV-003, F-COM-007'],
  ['DLG-RCP-01', '접수 처리', 'Modal', 'P03-01~03', 'F-RCP-001, F-COM-006~007'],
  ['DLG-RCP-02', '추가검사 변경', 'Modal', 'P03-05', 'F-RCP-002, F-COM-004, F-COM-007'],
  ['CNF-RCP-01', '접수 취소 확인', 'Confirm', 'P03-06', 'F-RCP-003, F-COM-007'],
  ['DLG-LOG-01', '변경이력 열람', 'Modal', 'P01~P03 공통', 'F-COM-008'],
  ['DLG-HOL-01', '휴무일 관리', 'Modal', '기준정보', 'F-COM-009'],
];

const LEGEND = [
  ['프로그램', '성인 일반건강검진의 예약과 접수를 처리하는 원내 프로그램. 상단 진입점 5개(수검자 관리 / 신규 예약 / 예약 관리 / 접수 관리 / 휴무일 관리), 업무 Tab 3개, Modal 8개, 확인창 2개로 구성한다.'],
  ['동반 문서', '「00_검진_예약접수_업무정책」 CP · EP · RP · RCP 정책과 TGT · NEX · AEX · HOL Rule의 개별 내용   /   「01_검진_예약접수_업무프로세스」 P01~P03 단계 · 분기 · 상태전이   /   「02_검진_예약접수_기능정의」 Function ID 18개의 기능 계약   /   「03_검진_예약접수_화면설계서」 본 문서. 네 문서는 하나의 세트이며 정책 ID · 프로세스 ID · 기능 ID로 서로를 참조한다.'],
  ['화면 용어', 'PatientId 수검자 Master 내부 식별자   ·   WorkId 예약/접수 업무 1건의 식별자   ·   Normal 일반 신규예약 Context   ·   WalkIn 현장 당일예약 Context(예약일 = 오늘 고정 ReadOnly)   ·   RowVersion 저장 시 함께 전달하는 동시성값(수검자 · 업무 공통)'],
  ['업무 상태코드', '예약(RSV) · 접수완료(RCP) · 예약취소(CNR) · 접수취소(CNC). 취소는 물리 삭제하지 않고 같은 행의 상태만 바꾸며 복원하지 않는다. (CP-05)'],
  ['검증 표현 원칙', '미리 판단 가능한 선택불가 → Disabled + 사유  /  입력필드 오류 → Inline 오류  /  저장·상태전이 최종차단 → Blocking Message + 중단  /  동시성 충돌 → Blocking Message + 최신값 Refresh'],
  ['규칙 ID 출처', 'CP / EP / RP / RCP / TGT / NEX / AEX / HOL 은 「업무정책」 문서에 정의된 규칙 ID다. 각 화면 설명의 괄호 안 표기가 그 규칙을 가리킨다.'],
  ['대상 환경', 'C# WinForms · .NET Framework 4.6.1 · DevExpress Components 20.2 · MSSQL. 최소 검증 해상도 1366×768, 권장 1920×1080. Windows 배율 100% / 125%.'],
  ['공통 업무조건', '휴무일이거나 운영시간(09:00~18:00) 밖이면 상태영역에 업무 불가로 표시한다. 화면은 그것으로 Action을 막지 않으며 판정은 저장 시점에 DB가 한다. 이 조건은 예약·접수 업무를 대상으로 하고 기준정보 정비(수검자·휴무일)에는 적용하지 않는다. (CP-01~04)'],
  ['구현 제외', '로그인·권한, 통계 Dashboard, 실제 검사 수행·결과 입력·판독, 공단 대상자 API, 검사 Master·AEX 관리자 CRUD, 법정·대체 공휴일 행의 화면 변경, 가격·할인·수납·결제, 수검자 삭제·복원, 출력·인쇄·엑셀 내보내기, 사용자별 Grid Layout 저장.'],
];

const pages = tablePages('화면 목록', [
  {
    kind: 'table',
    cols: [
      { t: '화면 ID', w: 1.35, align: 'left' }, { t: '화면명', w: 2.60, align: 'left' },
      { t: '형태', w: 1.05 }, { t: '주요 Process', w: 1.80, align: 'left' },
      { t: '주요 Function', flex: 1, align: 'left' },
    ],
    rows: ROWS,
  },
  {
    kind: 'table', caption: '범례 · 전제',
    cols: [{ t: '구분', w: 1.60, align: 'left' }, { t: '내용', flex: 1, align: 'left' }],
    rows: LEGEND, size: 8.5,
  },
], { size: 8.5 });
module.exports = { pages };
