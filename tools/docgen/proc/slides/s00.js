'use strict';
// 0 — 문서 안내 · 용어 정의
// 이 문서는 P·CP·EP·RP·RCP 코드를 30여 곳에서 인용하면서 그 정의는 동반 문서에 둔다.
// 어느 문서를 봐야 하는지와, 시점 기준이 달라 오독되기 쉬운 용어만 여기서 못 박는다.
const { A, head, table } = require('../flow');

const DOCS = [
  ['00_검진_예약접수_업무정책',
    'CP · EP · RP · RCP 정책과 TGT · NEX · AEX · HOL Rule의 개별 내용. 각 장 하단 캡션이 인용하는 정책 ID의 뜻은 이 문서에 있다.'],
  ['01_검진_예약접수_업무프로세스',
    '본 문서. P01 ~ P03 프로세스의 단계 · 분기 · 종료점 · 상태전이와 프로세스 ↔ 정책 추적표.'],
  ['02_검진_예약접수_기능정의',
    'Function ID 16개 / 기능행 36개의 기능 계약, 업무 Rule, 개발 범위, DB 추적.'],
  ['03_검진_예약접수_화면설계서',
    '화면 12개의 구성 · 필드 · 검증 · Ribbon Action · 화면 전이.'],
];

const TERMS = [
  ['활성 업무',
    '상태가 예약(RSV) 또는 접수완료(RCP)인 해당 수검자의 모든 업무. 예약일의 과거 · 현재 · 미래를 구분하지 않는다. P01-05의 주민등록번호 변경 차단 판정에 쓰며, 차트번호 변경에는 적용하지 않는다.'],
  ['중복판단 유효예약',
    '예약일 >= DB 현재일이고 상태가 RSV 또는 RCP인 업무. P02-01의 중복예약 판정에 쓴다. 활성 업무와 달리 과거 업무를 제외하며, 예약변경에서는 변경 대상 WorkId 자체를 제외한다.'],
  ['Normal / WalkIn',
    '신규예약 진입 Context. Normal은 예약일을 선택하고, WalkIn(현장 당일예약)은 예약일을 DB 현재일로 고정한다.'],
  ['PatientId / WorkId',
    'PatientId는 수검자 Master 식별키, WorkId는 예약 · 접수 업무 1건의 식별키다. 프로세스 · 화면 간 전달키로 쓴다.'],
  ['RSV / RCP / CNR / CNC',
    '예약 / 접수완료 / 예약취소 / 접수취소 상태 코드. 취소는 물리 삭제하지 않고 같은 업무 행의 상태만 바꾸며 복원하지 않는다.'],
  ['AM / PM',
    '오전 / 오후 시간대 코드. 평일은 오전과 오후, 토요일은 오전만 운영한다.'],
  ['저장 SP',
    '데이터 변경을 수행하는 Stored Procedure. UI의 Enabled/Disabled는 사전 안내이며, 저장 · 상태전이 시점에 DB가 현재 조건을 다시 검증한다.'],
];

const OPT = { size: 9.5, headSize: 9.5, rowH: 0.30, headH: 0.28 };

function draw(c) {
  head(c, A.x, 1.10, A.w, '동반 문서');
  const t1 = table(c, A.x, 1.38, [
    { t: '문서', w: 3.30, align: 'left' },
    { t: '담는 내용', w: A.w - 3.30, align: 'left' },
  ], DOCS, OPT);

  const y2 = t1.bottom + 0.26;
  head(c, A.x, y2, A.w, '용어 정의   ·   같은 상태코드를 쓰지만 시점 기준이 다른 두 개념을 구분한다');
  table(c, A.x, y2 + 0.28, [
    { t: '용어', w: 2.40, align: 'left' },
    { t: '정의', w: A.w - 2.40, align: 'left' },
  ], TERMS, OPT);
}

module.exports = {
  title: '문서 안내  ·  용어 정의',
  caption: '네 문서는 하나의 세트이며 정책 ID · 프로세스 ID · 기능 ID로 서로를 참조한다.',
  draw,
};
