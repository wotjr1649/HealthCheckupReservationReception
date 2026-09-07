'use strict';
// 부록 A — 화면 동작 규칙 요약 (원문 03 §15.1 / §14.2 / §14.3 / §16 / §15.3)
const { tablePages } = require('../tablepage');

const pages = tablePages('부록 A.  화면 동작 규칙', [
  {
    kind: 'table', caption: 'A-1.  검증 표현 원칙 (§15.1)',
    cols: [{ t: '상황', w: 4.20, align: 'left' }, { t: 'UI 표현', flex: 1, align: 'left' }],
    rows: [
      ['미리 판단 가능한 선택불가', 'Disabled + 사유 표시'],
      ['입력필드 오류', 'Inline 오류'],
      ['저장 / 상태전이 최종차단', 'Blocking Message + 중단'],
      ['동시성 충돌', 'Blocking Message + 최신값 Refresh'],
    ],
  },
  {
    kind: 'table', caption: 'A-2.  화면별 NEX / AEX 편집 가부 (§14.2)',
    cols: [{ t: '화면', w: 4.20, align: 'left' }, { t: 'NEX', flex: 1 }, { t: 'AEX', flex: 1 }],
    rows: [
      ['Workbench 상세 (WF-WRK-01)', 'ReadOnly', 'ReadOnly'],
      ['신규예약 (WF-RSV-01)', 'ReadOnly', 'Editable'],
      ['예약변경 (DLG-RSV-01)', 'ReadOnly', 'Editable'],
      ['접수처리 (DLG-RCP-01)', 'ReadOnly', 'ReadOnly'],
      ['접수완료 추가검사변경 (DLG-RCP-02)', 'ReadOnly', 'Editable'],
      ['취소 상태(CNR·CNC)', 'ReadOnly', 'ReadOnly'],
    ],
  },
  {
    kind: 'table', caption: 'A-3.  업무 상태별 Action 가부 (§14.3)',
    cols: [
      { t: '상태', w: 2.20, align: 'left' }, { t: '예약변경', flex: 1 }, { t: '예약취소', flex: 1 },
      { t: '접수', flex: 1 }, { t: '접수완료 AEX 변경', flex: 1 }, { t: '접수취소', flex: 1 },
    ],
    rows: [
      ['예약 (RSV)', 'O', 'O', '조건부 O', 'X', 'X'],
      ['접수완료 (RCP)', 'X', 'X', 'X', 'O', 'O'],
      ['예약취소 (CNR)', 'X', 'X', 'X', 'X', 'X'],
      ['접수취소 (CNC)', 'X', 'X', 'X', 'X', 'X'],
    ],
  },
  {
    kind: 'table', caption: 'A-4.  Refresh · 동시성 · No-op 계약 (§16)',
    cols: [{ t: '항목', w: 3.20, align: 'left' }, { t: '계약', flex: 1, align: 'left' }],
    rows: [
      ['수검자 Edit Modal', '원본 LastEditDate를 숨은 값으로 유지한다.'],
      ['Work 관련 Modal', '원본 RowVersion을 숨은 값으로 유지한다.'],
      ['저장 성공', '응답의 새 동시성값으로 화면 모델을 교체한다.'],
      ['저장 실패', '기존 RowVersion으로 재시도하지 않는다.'],
      ['AEX 실제 변경', 'Work RowVersion이 변경된 것으로 처리한다.'],
      ['실제 변경 없음', '성공적인 No-op 안내 후 불필요한 Refresh를 최소화한다.'],
      ['Targeted Navigation', '항상 최신 DB 데이터를 조회한다.'],
    ],
  },
  {
    kind: 'table', caption: 'A-5.  DB 실패 후 UI 처리 (§15.3)',
    cols: [{ t: '원칙', flex: 1, align: 'left' }],
    rows: [
      ['DB 오류 ResultCode를 화면 업무문구로 매핑한다.'],
      ['실패한 입력을 무조건 폐기하지 않는다.'],
      ['정원 · 마감 · 상태 · 동시성처럼 최신값이 필요한 실패는 관련 영역을 Refresh한다.'],
      ['Transaction이 실패하면 성공 메시지나 로컬 상태전이를 적용하지 않는다.'],
    ],
  },
]);

module.exports = { pages };
