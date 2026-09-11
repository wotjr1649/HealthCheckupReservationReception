'use strict';
// 부록 F — 구현 기준 (원문 03 §6.2 주민등록번호 파생 / §1.4 DevExpress 20.2 구현 기준)
// 이 두 표가 없으면 DLG-PAT-01의 Birthday/Gender 자동산출과 Control 선정을 문서만으로 결정할 수 없다.
// 매핑·Control·원칙은 원문 그대로다. '예시' 열만 그 규칙으로 만든 검증용 값이다.
const { tablePages } = require('../tablepage');

const pages = tablePages('부록 F.  구현 기준', [
  {
    kind: 'table', caption: 'F-1.  주민등록번호 입력 · 파생값',
    cols: [
      { t: '7번째 자리', w: 1.60 }, { t: '세기', w: 1.60 }, { t: '성별', w: 1.20 },
      { t: '예시 (입력 → 파생)', flex: 1, align: 'left' },
    ],
    rows: [
      ['9', '1800년대', '남', '890704-9000012   →   1889-07-04  /  남'],
      ['0', '1800년대', '여', '890704-0000011   →   1889-07-04  /  여'],
      ['1, 5', '1900년대', '남', '800511-1000015   →   1980-05-11  /  남'],
      ['2, 6', '1900년대', '여', '660312-2000019   →   1966-03-12  /  여'],
      ['3, 7', '2000년대', '남', '070704-3000013   →   2007-07-04  /  남'],
      ['4, 8', '2000년대', '여', '070704-4000012   →   2007-07-04  /  여'],
    ],
  },
  {
    kind: 'note',
    lines: [
      "처리 순서 :   입력 / 변경  →  '-' 제거, 숫자 13자리  →  생년월일 실제 날짜 검증  →  "
        + '7번째 자리 세기 · 성별 해석  →  Birthday / Gender 자동산출  →  중복 · 변경조건 검증',
      '· 실제 행정번호 존재 여부와 체크디지트 검증은 하지 않는다.',
      '· 형식 · 날짜 · 파생 실패 시 Birthday / Gender를 Clear하고 저장을 비활성화한다.',
      '· 화면에는 전체값을 표시하고 DB에는 - 를 제거한 숫자 13자리를 저장한다 (SocialNumber).',
    ],
  },
  {
    kind: 'table', caption: 'F-2.  DevExpress 20.2 구현 기준',
    cols: [{ t: '영역', w: 3.40, align: 'left' }, { t: '기준 Control / 구현 방식', flex: 1, align: 'left' }],
    rows: [
      ['MainForm', 'RibbonForm + RibbonControl 1개'],
      ['Navigation / Context Action', 'RibbonPage, RibbonPageGroup, BarButtonItem'],
      ['Main 업무 Tab', 'XtraTabControl + 업무별 XtraUserControl'],
      ['화면 배치', 'LayoutControl, SplitContainerControl, Dock'],
      ['목록', 'GridControl + GridView'],
      ['일반 입력', 'TextEdit, MemoEdit, DateEdit, RadioGroup, CheckEdit'],
      ['Modal', 'XtraForm'],
      ['입력 검증', 'DXValidationProvider, DXErrorProvider, Blocking Message'],
      ['상태표시', 'RibbonStatusBar 또는 MainForm 하단 상태영역'],
    ],
  },
  {
    kind: 'table', caption: 'F-3.  구현 원칙',
    cols: [{ t: '원칙', flex: 1, align: 'left' }],
    rows: [
      ['고정 Pixel 중심 배치를 피하고 Layout / Dock / Splitter를 사용한다.'],
      ['MainForm은 기본 크기를 권장 기준으로 열고 최대화를 지원한다. 화면이 그보다 작으면 작업 영역에 맞춘다.'],
      ['최소 검증 해상도는 1366×768, 권장 기준은 1920×1080이다. 창의 최소 크기는 최소 검증 해상도다.'],
      ['Windows 배율 100%와 125%에서 Label · Grid Header · Modal 하단버튼 잘림을 확인한다.'],
      ['Ribbon과 별도 Main Menu Bar를 중복 구성하지 않는다.'],
      ['DevExpress 20.2에서 제공되지 않는 최신 API에 의존하지 않는다.'],
    ],
  },
]);

module.exports = { pages };
