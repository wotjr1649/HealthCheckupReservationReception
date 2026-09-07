'use strict';
// WF-PAT-01 수검자 관리 (Main Tab) — 원문 03 §5
// 대표 상태: 조회 결과 있음 + 1행 선택. 행 선택 시 [정보수정]·[신규예약]·[변경이력]이 활성된다(§5.2).
const S = require('../spec');
const K = require('../kit');

function draw(c) {
  const v = K.shell(c, {
    nav: ['수검자 관리', '신규 예약', '예약 관리', '접수 관리'], navActive: 0,
    ribbon: [
      { name: '검색', buttons: [{ t: '조회' }] },
      { name: '수검자', buttons: [{ t: '신규등록' }, { t: '정보수정' }, { t: '신규예약' }] },
      { name: '보기', buttons: [{ t: '변경이력' }, { t: '컬럼설정' }] },
    ],
    tabs: ['수검자 관리'], tabActive: 0,
    marks: { ribbon: '1', status: '7' },
  });

  const gap = 0.06;
  const sb = K.searchBand(c, v.x, v.y + gap, v.w, [
    [{ label: '차트번호', w: 1.25 }, { label: '이름', w: 1.05 }, { label: '주민번호', w: 1.60 }],
    [{ label: '생년월일', w: 1.25 }, { label: '휴대전화', w: 1.55 }],
  ], { buttons: ['조회'] });
  c.markLeft(v.x, v.y + gap, 0.94, '2');

  const bodyY = sb.bottom + gap;
  const bodyH = v.y + v.h - bodyY - 0.04;
  const g = 0.10, gw = (v.w - g) * 0.62, dw = (v.w - g) * 0.38;

  // 좌: 수검자 Grid
  const gp = K.panel(c, v.x, bodyY, gw, bodyH, '수검자 목록');
  K.grid(c, gp.x, gp.y, K.cols(gp.w, [
    { t: '차트번호', w: 1.15, align: 'left' }, { t: '이름', w: 0.80 },
    { t: '생년월일', w: 1.00 }, { t: '성별', w: 0.55 }, { t: '휴대전화번호', flex: 1 },
  ]), [
    ['2026-000121', '수검자1', '1980-05-11', '남', '010-0000-0001'],
    ['2026-000122', '수검자2', '1972-11-03', '여', '010-0000-0002'],
    ['2026-000123', '홍길동', '1966-03-12', '여', '010-0000-0003'],
    ['2026-000124', '수검자4', '1995-08-27', '남', '010-0000-0004'],
  ]);
  // 선택행 표시 (3번째 데이터 행)
  c.rect(gp.x, gp.y + K.ROW * 3, gp.w, K.ROW, { stroke: S.C.ink, sw: S.W.panel });
  c.markLeft(v.x, bodyY, bodyH, '3');

  // 우: 상세 (ReadOnly)
  const dx = v.x + gw + g;
  const dp = K.panel(c, dx, bodyY, dw, bodyH, '수검자 상세 · ReadOnly');
  let y = dp.y, rrnY = 0;
  const sect = (t) => { c.text(dp.x, y, dp.w, 0.19, '[ ' + t + ' ]', { size: S.TEXT.small, align: 'left', bold: true, color: S.C.hint }); y += 0.20; };
  const row = (k, val) => { K.field(c, dp.x, y, 1.02, dp.w - 1.02, k, val, { readonly: true }); y += 0.26; };
  sect('기본정보');
  row('차트번호', '2026-000123'); row('이름', '홍길동');
  rrnY = y; row('주민등록번호', '660312-2000019'); row('생년월일 / 성별', '1966-03-12  /  여');
  sect('연락처');
  row('휴대전화', '010-0000-0003'); row('전화번호 / E-mail', '');
  sect('주소');
  row('우편번호 / 주소', ''); row('상세주소', '');
  sect('메모');
  const memoH = Math.max(0.30, dp.y + dp.h - y - 0.02);
  c.rect(dp.x, y, dp.w, memoH, { stroke: S.C.dim, fill: S.C.band });
  c.markLeft(dx, bodyY, bodyH, '4');
  c.markLeft(dp.x + 1.02, rrnY, K.FIELD_H, '5');
  c.markLeft(dp.x, y, memoH, '6');
}

const desc = [
  { n: '', text: 'WF-PAT-01  수검자 관리 · Main Tab · P01-01~05 · F-PAT-001~003' },
  { n: '1', text: '행 미선택이면 [정보수정]·[신규예약]·[변경이력] 비활성 (§5.2)' },
  { n: '', text: '[변경이력]은 조회 Action이라 공통 업무불가에도 열린다 (§23.4)' },
  { n: '2', text: '최소 1개 조건 필요. 조건은 AND 결합 (§5.3)' },
  { n: '3', text: '차트번호·주민번호·생년월일·휴대전화는 정확검색, 이름은 앞부분 일치' },
  { n: '4', text: '단일 행 선택. 재조회 시 선택행과 상세를 초기화한다 (§5.5)' },
  { n: '5', text: '주민등록번호는 마스킹 없이 전체값을 표시한다 (§5.6 SocialNumber)' },
  { n: '6', text: '삭제는 제공하지 않는다. 정보 오류는 [정보수정]으로 처리 (EP-10)' },
  { n: '7', text: 'PatientId·정규화 컬럼·생성/수정시각은 Grid에 노출하지 않는다 (§5.5)' },
];

module.exports = { title: '수검자 관리', draw, desc };
