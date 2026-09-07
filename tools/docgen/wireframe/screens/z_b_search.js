'use strict';
// 부록 B — 조회·검색 계약 (원문 03 §5.3 / §9.3 / §5.5 / §9.4 / §18)
const { tablePages } = require('../tablepage');

const pages = tablePages('부록 B.  조회 · 검색 계약', [
  {
    kind: 'table', caption: 'B-1.  수검자 조회 (WF-PAT-01 · DLG-PAT-02, §5.3)',
    cols: [{ t: '항목', w: 3.20, align: 'left' }, { t: '계약', flex: 1, align: 'left' }],
    rows: [
      ['조회조건', '차트번호 / 이름 / 주민번호 / 생년월일 / 휴대전화'],
      ['최소 조건', '최소 1개 조건이 있어야 조회한다.'],
      ['결합', '입력된 조건은 AND로 결합한다.'],
      ['정확검색', '차트번호 · 주민번호 · 생년월일 · 휴대전화는 정규화 후 정확검색한다.'],
      ['접두검색', "이름은 앞부분 일치(Name LIKE @Name + '%')로 검색한다."],
      ['빈 값', '빈 문자열은 미입력으로 처리한다.'],
      ['정규화', '주민번호와 전화번호는 `-`를 제거하여 조회한다.'],
      ['재조회', '재조회 시 선택행과 우측 상세를 초기화한다.'],
    ],
  },
  {
    kind: 'table', caption: 'B-2.  예약·접수 업무내역 조회 (WF-WRK-01, §9.3)',
    cols: [{ t: '항목', w: 3.20, align: 'left' }, { t: '계약', flex: 1, align: 'left' }],
    rows: [
      ['조회조건', '예약/접수일 From ~ To  ·  상태(전체 / 예약 / 접수완료 / 예약취소 / 접수취소)  ·  차트번호  ·  이름'],
      ['최소 조건', '최소 하나의 실질 조건이 필요하다. 상태 `전체`만 선택한 경우는 조건으로 보지 않는다.'],
      ['결합', '입력조건은 AND로 결합한다.'],
      ['날짜 범위', '양끝을 포함한다. From만 있으면 이후, To만 있으면 이전. From > To는 Inline 오류.'],
      ['검색 방식', '차트번호는 정확검색, 이름은 접두검색.'],
      ['제외', '시간대는 조회조건에서 제외한다.'],
      ['범위', '과거 · 현재 · 미래를 모두 조회할 수 있다.'],
    ],
  },
  {
    kind: 'table', caption: 'B-3.  Grid 컬럼 (§5.5 · §9.4)',
    cols: [{ t: '화면', w: 2.60, align: 'left' }, { t: '기본 컬럼', w: 4.20, align: 'left' },
      { t: '선택 컬럼', w: 2.40, align: 'left' }, { t: '전 Grid 제외', flex: 1, align: 'left' }],
    rows: [
      ['수검자 관리\n(WF-PAT-01)', '차트번호 → 이름 → 생년월일 → 성별 → 휴대전화번호',
        '주민등록번호 / 전화번호 / E-mail / 우편번호 / 주소',
        '내부키(PatientId · WorkId), 정규화 컬럼, 동시성값(LastEditDate), 생성·수정시각, 업무 미사용 컬럼'],
      ['예약·접수 Workbench\n(WF-WRK-01)', '예약/접수일 → 시간대 → 상태 → 이름 → 차트번호',
        '성별 / 생년월일 / 휴대전화번호',
        '위와 동일. Workbench에는 주민등록번호를 표시하지 않으며 선택 컬럼 후보로도 제공하지 않는다. 정원현황·NEX/AEX 상세·주소·E-mail·메모도 제외.'],
    ],
  },
  {
    kind: 'table', caption: 'B-4.  Column Chooser 정책 (§18)',
    cols: [{ t: '구분', w: 1.60, align: 'left' }, { t: '내용', flex: 1, align: 'left' }],
    rows: [
      ['제공', '표시 / 숨김,  기본값 복원'],
      ['제외', '사용자별 Layout DB 저장,  로그인별 개인화,  복잡한 Column Profile 관리자'],
      ['원칙', '허용 컬럼 후보만 제공한다. 수검자 관리에서는 SocialNumber를 선택 컬럼으로 허용하고, Workbench에서는 후보로 제공하지 않는다.'],
    ],
  },
  {
    kind: 'table', caption: 'B-5.  Grid 공통 동작',
    cols: [{ t: '항목', w: 3.20, align: 'left' }, { t: '동작', flex: 1, align: 'left' }],
    rows: [
      ['선택', 'Single Row Selection. Multi-Select는 사용하지 않는다.'],
      ['행 선택', '행을 선택하면 즉시 우측 상세를 갱신한다.'],
      ['Double Click', '업무 Action을 실행하지 않는다.'],
      ['재조회', 'SelectedRow를 해제하고 상세와 Transaction Action을 모두 Clear한다.'],
    ],
  },
]);

module.exports = { pages };
