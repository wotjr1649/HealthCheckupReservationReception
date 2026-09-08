'use strict';
// 부록 E — `수검자` 17개 컬럼 UI·저장 계약 (원문 03 §5.6). 원문 표를 그대로 옮긴다.
// 4번 SocialNumber 행의 '테스트 전체값 표시' 는 「검증용 전체값 표시」로 문구만 다듬는다.
const { tablePages } = require('../tablepage');

const ROWS = [
  ['1', 'PatientId', '화면 미표시', 'DB 자동생성, 저장 성공 시 호출 화면 반환, 불변'],
  ['2', 'CreationDate', '화면 미표시', 'INSERT 시 DB 서버시각'],
  ['3', 'LastEditDate', '화면 미표시', 'INSERT 시 생성시각, UPDATE 성공 시 DB 서버시각. 감사 정보이며 동시성 기준이 아니다'],
  ['4', 'RowVersion', '화면 미표시', 'DB 자동생성. 수정·삭제 요청에 함께 실어 보내는 동시성값'],
  ['5', 'ChartNo', '등록/수정 Editor', 'New 자동발급 또는 수동입력, Edit 수동수정, DB 최종 고유성 검증'],
  ['6', 'Name', '등록/수정, 조회/Grid/상세', '필수'],
  ['7', 'SocialNumber', '등록/수정, 조회, 상세, 선택 컬럼', "검증용 전체값 표시; '-' 제거 후 숫자 13자리 저장; DB 최종 고유성 검증"],
  ['8', 'Birthday', 'ReadOnly', 'SocialNumber에서 yyyyMMdd 자동산출'],
  ['9', 'Gender', 'ReadOnly', 'SocialNumber에서 M/F 자동산출; UI 남/여'],
  ['10', 'Email', '등록/수정, 상세/선택 컬럼', '선택, 미입력 NULL'],
  ['11', 'MobilePhone', '등록/수정, 조회/Grid/상세', '선택, 미입력 NULL'],
  ['12', 'Phone', '등록/수정, 상세/선택 컬럼', '선택, 미입력 NULL'],
  ['13', 'Zipcode', '등록/수정, 상세/선택 컬럼', '선택, 미입력 NULL'],
  ['14', 'Address', '등록/수정, 상세/선택 컬럼', '선택, 미입력 NULL'],
  ['15', 'AddressDetail', '등록/수정, 상세', '선택, 미입력 NULL'],
  ['16', 'HepatitisBExcluded', '등록/수정 Editor', '신규 기본값 0; NEX-03 제외 판정 입력이며 1이 제외다. Grid·조회조건에는 제공하지 않는다'],
  ['17', 'Memo', '등록/수정, 상세', '선택, 미입력 NULL'],
];

const pages = tablePages('부록 E.  수검자 17개 컬럼 UI·저장 계약', [
  {
    kind: 'table',
    cols: [
      { t: 'No', w: 0.50 }, { t: '컬럼', w: 2.10, align: 'left' },
      { t: 'UI 노출 / 편집', w: 3.40, align: 'left' }, { t: '값 생성 · 저장 기준', flex: 1, align: 'left' },
    ],
    rows: ROWS,
  },
  {
    kind: 'note',
    lines: [
      'UI 미사용 NOT NULL 컬럼은 DB Default 또는 저장 Stored Procedure가 값을 보장한다.',
      'PatientId는 내부 연결키이며 화면에 표시하지 않는다.',
      '주민등록번호(SocialNumber)는 실제 값이 아닌 검증용 값으로 운용한다.',
    ],
  },
], { size: 8, pad: 0.03 });

module.exports = { pages };
