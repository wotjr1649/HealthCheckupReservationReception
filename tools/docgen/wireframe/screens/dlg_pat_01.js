'use strict';
// DLG-PAT-01 수검자 등록 / 정보수정 (Modal) — 원문 03 §6
// 대표 상태: New Mode. Edit Mode에서는 차트번호 자동발급 전환이 없고 기존 ChartNo 수동수정만 허용된다(§6.4).
const S = require('../spec');
const K = require('../kit');

function draw(c) {
  const m = K.modal(c, {
    w: 6.20, h: 5.24, title: '수검자 신규등록  /  정보수정',
    buttons: [{ t: '저장', primary: true }, { t: '닫기' }],
    parent: '수검자 관리 (WF-PAT-01)  ·  수검자 선택 (DLG-PAT-02)',
  });
  const i = m.inner;
  let y = i.y;

  const sect = (t) => { c.text(i.x, y, i.w, 0.19, '[ ' + t + ' ]', { size: S.TEXT.small, align: 'left', bold: true, color: S.C.hint }); y += 0.20; };
  const row = (k, val, o = {}) => { const yy = y; K.field(c, i.x, y, 1.30, i.w - 1.30 - (o.tail ? 1.30 : 0), k, val ?? '', o); y += 0.285; return yy; };

  sect('기본정보');
  // 차트번호 방식 라디오
  c.text(i.x, y, 1.30, K.FIELD_H, '차트번호 방식', { size: S.TEXT.small, align: 'left', color: S.C.hint });
  c.box(i.x + 1.30, y, 1.20, K.FIELD_H, '◉  자동발급', { size: S.TEXT.gridData, color: S.C.data });
  c.box(i.x + 2.58, y, 1.20, K.FIELD_H, '○  수동입력', { size: S.TEXT.gridData, color: S.C.data });
  const modeY = y; y += 0.285;
  const chartY = row('차트번호', '', { dim: true });
  c.text(i.x + i.w - 1.60, chartY, 1.60, K.FIELD_H, '저장 시 자동발급', { size: S.TEXT.small, align: 'right', color: S.C.dimText });
  const nameY = row('이름  *');
  // 주민번호 → 생년월일·성별 파생을 그림이 직접 보여준다. 7번째 자리 2 = 1900년대 여 (§6.2, 부록 F-1).
  const rrnY = row('주민등록번호  *', '990707-2000018');
  const birY = row('생년월일', '1999-07-07', { readonly: true });
  c.text(i.x + i.w - 1.20, birY, 1.20, K.FIELD_H, 'ReadOnly', { size: S.TEXT.small, align: 'right', color: S.C.dimText });
  const genY = row('성별', '여', { readonly: true });
  c.text(i.x + i.w - 1.20, genY, 1.20, K.FIELD_H, 'ReadOnly', { size: S.TEXT.small, align: 'right', color: S.C.dimText });

  sect('연락처');
  row('휴대전화'); row('전화번호'); row('E-mail');

  sect('주소');
  row('우편번호 / 주소'); row('상세주소');

  sect('메모');
  const memoH = Math.max(0.32, i.y + i.h - y - 0.02);
  c.rect(i.x, y, i.w, memoH, { stroke: S.C.ink });

  c.markLeft(i.x + 1.30, modeY, K.FIELD_H, '1');
  c.markLeft(i.x + 1.30, chartY, K.FIELD_H, '2');
  c.markLeft(i.x + 1.30, nameY, K.FIELD_H, '3');
  c.markLeft(i.x + 1.30, rrnY, K.FIELD_H, '4');
  c.markLeft(i.x + 1.30, birY, 0.60, '5');
  c.markLeft(m.buttons[0].x, m.buttons[0].y, m.buttons[0].h, '6');
}

const desc = [
  { n: '', text: 'DLG-PAT-01  수검자 등록 / 정보수정 · Modal · P01-02~05 · F-PAT-002~003' },
  { n: '', text: '그림은 New Mode 기준. Edit Mode는 자동발급 전환 없이 기존 차트번호 수동수정만 허용한다 (§6.4).' },
  { n: '1', text: 'New는 자동발급이 기본값. Edit에서는 이 선택이 없다 (§6.3)' },
  { n: '2', text: '자동발급 번호는 화면 진입 시 점유하지 않고 저장 Transaction에서 확정' },
  { n: '3', text: '이름·주민등록번호가 필수. 미입력이면 저장 비활성 (EP-04)' },
  { n: '4', text: '전체값 표시. `-` 제거 후 숫자 13자리로 정규화하여 저장 (§6.2)' },
  { n: '5', text: '주민등록번호에서 자동 산출. 형식·날짜·파생 실패 시 Clear + 저장 차단' },
  { n: '6', text: '주민번호·차트번호 고유성은 저장 SP가 최종 재검증한다 (EP-03, EP-05)' },
  { n: '', text: '주민등록번호 변경은 해당 수검자에게 예약(RSV)·접수완료(RCP) 업무가 하나라도 있으면 차단된다. 차트번호 변경에는 이 조건을 적용하지 않는다 (EP-08).' },
];

module.exports = { title: '수검자 등록 · 정보수정', draw, desc };
