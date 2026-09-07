'use strict';
// DLG-PAT-03 중복 후보 확인 (Modal) — 원문 03 §6.5
const S = require('../spec');
const K = require('../kit');

function draw(c) {
  const m = K.modal(c, {
    w: 8.20, h: 3.60, title: '중복 후보 확인',
    buttons: [{ t: '입력값 수정' }, { t: '별도 수검자로 계속' }, { t: '닫기' }],
    parent: '수검자 등록 / 정보수정 (DLG-PAT-01)',
  });
  const i = m.inner;

  // 입력값
  const ip = K.panel(c, i.x, i.y, i.w, 0.62, '입력값');
  let fx = ip.x;
  [['이름', 0.90], ['생년월일', 1.00], ['주민번호', 1.35], ['휴대전화', 1.20]].forEach(([k, w]) => {
    fx = K.field(c, fx, ip.y, 0.66, w, k, '', { readonly: true }) + 0.14;
  });
  c.markLeft(i.x, i.y, 0.62, '1');

  // 후보 목록
  const gy = i.y + 0.72;
  const gp = K.panel(c, i.x, gy, i.w, 1.42, '중복 후보  ·  이름 + 생년월일 동일 / 주민등록번호 상이');
  K.grid(c, gp.x, gp.y, K.cols(gp.w, [
    { t: '차트번호', w: 1.20, align: 'left' }, { t: '이름', w: 0.80 }, { t: '생년월일', w: 1.05 },
    { t: '성별', w: 0.55 }, { t: '주민번호', w: 1.40 }, { t: '휴대전화', flex: 1 },
  ]), [
    // 이 화면의 존재 이유가 "주민등록번호 상이"이므로 두 후보의 값이 서로 달라야 한다.
    // 7번째 자리 2 = 1900년대 여 → 앞 6자리·성별과 정합한다 (§6.2).
    ['2026-000098', '홍길동', '1966-03-12', '여', '660312-2000035', '010-0000-0098'],
    ['2026-000114', '홍길동', '1966-03-12', '여', '660312-2000043', '010-0000-0114'],
  ]);
  c.markLeft(i.x, gy, 1.42, '2');

  // 안내
  const ny = gy + 1.52;
  c.text(i.x, ny, i.w, 0.26, '주민등록번호 오입력 여부를 확인하십시오.',
    { size: S.TEXT.label, align: 'left', bold: true, color: S.C.data });
  c.markLeft(m.buttons[0].x, m.buttons[0].y, m.buttons[0].h, '3');
  c.markLeft(m.buttons[1].x, m.buttons[1].y, m.buttons[1].h, '4');
}

const desc = [
  { n: '', text: 'DLG-PAT-03  중복 후보 확인 · Modal · P01-03 · F-COM-002' },
  { n: '', text: '동일 주민등록번호는 이 창을 거치지 않는다. 이름이 달라도 신규등록 없이 기존 PatientId를 반환한다 (EP-05, EP-06).' },
  { n: '1', text: '이름 + 생년월일이 같고 주민등록번호가 다를 때만 표시된다 (EP-07)' },
  { n: '2', text: '경고·확인 대상이지 자동 동일인 확정조건이 아니다' },
  { n: '3', text: '[입력값 수정] → Editor로 복귀 후 다시 검증' },
  { n: '4', text: '[별도 수검자로 계속] → 현재 이름+생년월일+주민번호 조합에만 유효' },
  { n: '', text: '세 값 중 하나라도 바뀌면 확인상태를 해제하고 다시 후보검증한다. 같은 확인값으로 저장을 재시도하면 이 창을 반복 표시하지 않는다.' },
  { n: '', text: '최종 고유성은 저장 Transaction이 수검자.SocialNumber 정확값으로 다시 판정한다.' },
];

module.exports = { title: '중복 후보 확인', draw, desc };
