'use strict';
// 부록 C — Validation Matrix 26행 (원문 03 §15.2). 원문 표를 그대로 옮긴다.
const { tablePages } = require('../tablepage');

const ROWS = [
  ['필수정보 누락', 'Control 인접 Inline', '저장 불가'],
  ['주민번호 13자리/날짜/파생 오류', 'Inline, Birthday/Gender Clear', '저장 불가'],
  ['주민번호 중복', 'Blocking + 기존 수검자 확인', '신규등록/수정 중단'],
  ['차트번호 중복', 'Blocking', '저장 중단'],
  ['동일 주민번호+이름 불일치', '기존 전체값 확인', '신규등록 없이 기존 PatientId'],
  ['이름+생년월일 후보', 'DLG-PAT-03', '수정/별도등록/중단'],
  ['주민번호 변경+RSV/RCP 존재', 'Blocking', '주민번호 변경 불가'],
  ['수검자 RowVersion 불일치', 'Blocking+최신 수검자 Refresh', '수정 Rollback'],
  ['기존 유효예약 존재', '안내+WorkId 연결', '신규예약 중단'],
  ['예약변경 다른 유효업무', '현재 WorkId 제외 후 DB 재검증', '자기 자신은 충돌로 판단하지 않음'],
  ['과거 예약일', '일정 Inline', '선택 불가'],
  ['일요일/HOL', '날짜/일정 Disabled+사유', '예약 불가'],
  ['토요일 PM', '시간대 Disabled', '선택 불가'],
  ['정원 20/20', '시간대 Disabled+정원 마감', '선택 불가'],
  ['Normal 당일예약 마감', '시간대 Disabled+사유', '저장 불가'],
  ['WalkIn 접수마감', 'Inline/Blocking', '현장예약 불가'],
  ['TGT 비대상', '판정결과+저장 Disabled', '예약 불가'],
  ['AEX 성별 제한', 'Checkbox Disabled+사유', '선택 불가'],
  ['AEX NEX 중복', 'Disabled+국가검진 포함 사유', '선택 불가'],
  ['접수일 불일치', 'Modal 사유+버튼 Disabled', '접수 불가'],
  ['접수마감', 'Modal 사유+버튼 Disabled', '접수 불가'],
  ['RCP/CNR/CNC 재접수', 'Action/Modal 차단', '접수 불가'],
  ['Work RowVersion 불일치', 'Blocking+최신 Work Refresh', '변경 Rollback'],
  ['Single Instance 재호출+Dirty', '폐기 Confirm', '확인 Reset / 취소 유지'],
  ['저장 직전 정원·중복·상태 변경', 'Blocking+Refresh', 'Transaction Rollback'],
  ['취소', 'Confirm 후 DB 재검증', 'CNR/CNC 상태전이'],
  ['휴무일 날짜 중복', 'DLG-HOL-01 Blocking', '등록 불가'],
  ['법정·대체 공휴일 행 선택', '입력행·[수정][삭제] Disabled', '변경 불가 (HOL-05)'],
  ['휴무일명 공백', 'Inline', '저장 불가'],
];

const pages = tablePages('부록 C.  Validation Matrix', [
  {
    kind: 'table',
    cols: [
      { t: 'Validation', w: 4.10, align: 'left' },
      { t: 'UI 표현', w: 4.10, align: 'left' },
      { t: '결과', flex: 1, align: 'left' },
    ],
    rows: ROWS,
  },
], { size: 8, pad: 0.03 });

module.exports = { pages };
