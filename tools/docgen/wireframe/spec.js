'use strict';
// 화면설계서 페이지 규격 — 레퍼런스 PNG 실측 비율 기반 (1663px 기준 → 13.333in 환산)
//   좌여백 3.4% / 화면 69.2% / 설명표 25.1%

const SLIDE = { w: 13.333, h: 7.5 };

const PAGE = {
  title: { x: 0.46, y: 0.18, w: 9.2, h: 0.55, size: 30, bold: true, color: '000000' },
  // 화면 그림 영역. 16:9(1.778)에 근접한 1.65 — 창으로 읽히면서 내부 콘텐츠가 10pt로 들어가는 하한
  frame: { x: 0.46, y: 1.02, w: 9.23, h: 5.85 },
  // 설명표: 위치·폭 고정, 세로는 내용만큼만 (레퍼런스는 늘려 채우지 않음)
  desc: { x: 9.73, y: 1.02, wNum: 0.40, wText: 2.95, maxH: 6.10 },
};

// 색 — 화면 내부는 남색 단색 + 회색(비활성). 빨강은 주석 레이어 전용.
const C = {
  ink: '1F3864',       // 화면 선·라벨
  inkText: '1F3864',
  data: '000000',      // 그리드 데이터·제목
  dim: '8C9BB5',       // 비활성 선
  dimText: '767676',   // 비활성 텍스트 (대비 4.5:1 확보)
  hint: '595959',      // 보조 텍스트
  red: 'FF0000',       // 콜아웃·영역강조 전용
  white: 'FFFFFF',
  band: 'F2F2F2',      // 헤더/스트립 옅은 채움
  parent: 'C8C8C8',    // 모달 뒤 부모 화면 외곽
};

// 선 굵기 3단계
const W = { win: 2.0, panel: 1.5, ctrl: 1.0, hair: 0.75 };

// 창 크롬 — 스키매틱 스트립(96DPI 실측값이 아님). 실측 238px는 프레임의 42%를 먹어 콘텐츠가 압살됨.
const CHROME = { nav: 0.30, ribbon: 0.58, tab: 0.26, status: 0.24 };

const FONT = 'Malgun Gothic';
const FONT_PATH = 'C:/Windows/Fonts/malgun.ttf';

// 줄상자 배수 — malgun.ttf OS/2 fsSelection bit7(USE_TYPO_METRICS)=false → Windows는 hhea/win 메트릭 사용
const LINE = 1.3301;

const TEXT = {
  label: 9.5,      // 박스 안 라벨
  gridHead: 9,     // 그리드 헤더
  gridData: 9,     // 그리드 데이터
  small: 8,        // 리본 그룹 라벨·상태바
  desc: 10,        // 설명표
  descNum: 10,
  callout: 10,
};

// 설명표 제약 — 실측: 2.95in 텍스트열, 10pt 한글 1em → 줄당 20자
const DESC_RULE = { maxChars: 44, maxCallouts: 9, pad: 0.055 };

module.exports = { SLIDE, PAGE, C, W, CHROME, FONT, FONT_PATH, LINE, TEXT, DESC_RULE };
