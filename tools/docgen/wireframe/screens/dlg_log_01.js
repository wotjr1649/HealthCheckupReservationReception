'use strict';
// DLG-LOG-01 변경이력 열람 (Modal) — 원문 03 §23.
// 읽기 전용이고 SP-LOG-01 하나만 호출한다. 편집·삭제·복원 Action을 그리지 않는다.
const S = require('../spec');
const K = require('../kit');

// §23.3 스케치의 두 행. 값은 예시이며 계약이 아니다.
const LOG = [
  ['2026-09-07 14:20', '접수1번데스크', '휴대전화', '010-0000-0098', '010-0000-0123'],
  ['2026-09-06 09:11', '접수1번데스크', '주소', '서울특별시 …', '경기도 …'],
];

// SP-LOG-01 RS1 그대로 — RecordedAt · OperatorName · ColumnName · BeforeValue · AfterValue.
// LogId는 내부키라 컬럼을 두지 않는다 (§23.3, §18).
const COLS = [
  { t: '기록일시', w: 1.35 },
  { t: '조작자(자기신고)', w: 1.50, align: 'left' },
  { t: '항목', w: 1.05, align: 'left' },
  { t: '변경전', flex: 1, align: 'left' },
  { t: '변경후', flex: 1, align: 'left' },
];

function draw(c) {
  const m = K.modal(c, {
    w: 8.60, h: 3.60, title: '변경이력 — 수검자 홍길동 (2026-000123)',
    buttons: [{ t: '닫기', primary: true }],
    parent: '수검자 관리 (WF-PAT-01)  ·  예약/접수 공통 Workbench (WF-WRK-01)',
  });
  const i = m.inner;

  // 대상 — §23.2 진입점 두 줄. 왼쪽이 지금 열린 인스턴스, 오른쪽이 다른 진입점.
  const tgtH = 0.28;
  c.rect(i.x, i.y, i.w, tgtH, { sw: S.W.panel });
  c.text(i.x + 0.14, i.y, 4.10, tgtH, '대상 :   TargetTable = 수검자     TargetKey = PatientId',
    { size: S.TEXT.gridData, align: 'left', bold: true, color: S.C.data });
  c.text(i.x + 4.30, i.y, i.w - 4.44, tgtH, '업무 행에서 열면  TargetTable = 예약접수 · TargetKey = WorkId',
    { size: S.TEXT.small, align: 'left', color: S.C.hint });
  c.markLeft(i.x, i.y, tgtH, '1');

  // 변경기록 Grid
  const gY = i.y + tgtH + 0.10, gH = 1.24;
  const p = K.panel(c, i.x, gY, i.w, gH, '변경기록  ·  읽기 전용  ·  기록일시 최신순 고정');
  const cols = K.cols(p.w, COLS);
  K.grid(c, p.x, p.y, cols, LOG);
  c.markLeft(i.x, gY, gH, '2');
  c.markLeft(p.x + cols[0].w, p.y, K.ROW, '3');
  c.markLeft(p.x + cols[0].w + cols[1].w + cols[2].w, p.y, K.ROW, '4');

  // 기록 0건 — 빈 Grid + 안내. 오류가 아니다 (§23.4).
  const eY = gY + gH + 0.12, eH = 0.92;
  const e = K.panel(c, i.x, eY, i.w, eH, '기록 0건일 때');
  K.grid(c, e.x, e.y, cols, []);
  c.text(e.x, e.y + K.ROW + 0.06, e.w, 0.30, '표시할 변경기록이 없습니다.',
    { size: S.TEXT.label, color: S.C.hint });
  c.markLeft(i.x, eY, eH, '5');
}

const desc = [
  { n: '', text: 'DLG-LOG-01  변경이력 열람 · Modal · F-COM-008' },
  { n: '1', text: '두 진입점이 같은 Modal을 연다' },
  { n: '2', text: 'SP-LOG-01 RS1 그대로. LogId는 표시하지 않는다' },
  { n: '3', text: '인증되지 않은 자기신고 값이다' },
  { n: '4', text: '4000자에서 잘렸을 수 있다. 복원 근거가 아니다' },
  { n: '5', text: '0건은 오류가 아니다. Code=0을 돌려준다' },
  { n: '', text: '읽기 전용이다. 편집·삭제·재적용 Action이 없다.' },
  { n: '', text: '정렬은 기록일시 최신순 고정이고 사용자 정렬이 없다.' },
  { n: '', text: '페이징을 두지 않는다. SP-LOG-01만 호출한다.' },
  { n: '', text: '수검자 행에서는 수검자 변경만 보인다.' },
  { n: '', text: '열람은 변경이력을 남기지 않는다.' },
];

module.exports = { title: '변경이력 열람', draw, desc };
