'use strict';
// 1 — 전체 업무 흐름. 원문 P00 mermaid(시작→업무구분→P01/P02/P03→종료)는 구조만 담고 있어
// 그 아래 `핵심 정상업무 연계` 7단계를 가로 흐름으로 그리고 `보조업무` 4항목을 분기로 붙인다.
const { A, TS, node, head, ar, S, wrap, lineH } = require('../flow');

const MAIN = [
  '수검자 확인 또는\n신규등록',
  '예약일·시간대 확정',
  '대상판정 및 검사구성',
  '예약 저장',
  '수검자 내원',
  '예약·예정검사 확인',
  '접수',
];

const AUX = [
  '수검자 정보수정',
  '예약변경 / 예약취소',
  '예약·접수 내역조회',
  '접수완료 AEX 변경 / 접수취소',
];

const RULES = [
  'P00은 수검자 관리·예약 관리·접수 관리의 최상위 구조다.',
  'P01은 조회/확정, 신규등록, 중복검증, 정보수정, 식별정보 변경검증의 5개 프로세스다.',
  'P02 신규예약은 일정 확정 → 일정검증 → TGT → NEX → AEX → 저장 순서다.',
  '기존 유효예약이 있으면 신규예약을 생성하지 않고 기존 Work를 연다.',
  '예약변경의 중복판정에서는 변경 대상 Work 자체를 제외한다.',
  '예약일 변경은 TGT/NEX/AEX를 영향 재검증하고 시간대만 변경하면 일정 Rule만 검증한다.',
  'P03은 예약 기반 접수만 허용하며 현장 내원도 WalkIn 당일예약 후 접수한다.',
  '접수 단계에서는 예약정보와 검사구성을 확인하며 직접 편집하지 않는다.',
  '예약과 접수는 동일 업무 행의 RSV/RCP/CNR/CNC 상태전이다.',
  '취소는 보존하되 복원하지 않는다.',
  '모든 Write는 DB에서 현재상태·정원·중복·Rule·동시성을 최종 검증한다.',
];

const NW = 1.50, PITCH = 1.818;
const NY = 1.74, NH = 0.66;
const cx = i => A.x + i * PITCH + NW / 2;

function draw(c) {
  head(c, A.x, 1.06, A.w, '핵심 정상업무 연계   —   수검자 확정에서 접수까지의 정상 경로');

  // 업무 구분 띠 (P00 mermaid 의 3분기)
  const spans = [
    { t: 'P01 수검자 관리', a: 0, b: 0 },
    { t: 'P02 예약 관리', a: 1, b: 3 },
    { t: 'P03 접수 관리', a: 4, b: 6 },
  ];
  spans.forEach(s => {
    const x = A.x + s.a * PITCH, w = (s.b - s.a) * PITCH + NW;
    c.rect(x, 1.42, w, 0.24, { fill: null, stroke: S.C.ink, sw: S.W.panel });
    c.text(x, 1.42, w, 0.24, s.t, { size: 9, bold: true, color: S.C.data });
  });

  // 7단계 가로 흐름
  MAIN.forEach((t, i) => {
    const x = A.x + i * PITCH;
    node(c, x, NY, NW, NH, t, { tag: `M${i + 1}` });
    if (i > 0) ar(c, [[x - (PITCH - NW) + 0.02, NY + NH / 2], [x - 0.03, NY + NH / 2]]);
  });

  // 보조업무 — 정상 흐름에서 갈라지는 4항목
  const railY = 2.70, boxY = 3.02, boxH = 0.44;
  const stubs = [cx(0), (A.x + 1 * PITCH + A.x + 3 * PITCH + NW) / 2, (A.x + 4 * PITCH + A.x + 6 * PITCH + NW) / 2];
  const aw = 2.90, ag = (A.w - aw * 4) / 3;
  const acx = i => A.x + i * (aw + ag) + aw / 2;
  stubs.forEach(x => c.line(x, NY + NH, x, railY, { dash: 'dash', stroke: S.C.dim, sw: S.W.ctrl }));
  c.line(Math.min(stubs[0], acx(0)), railY, Math.max(stubs[2], acx(3)), railY, { dash: 'dash', stroke: S.C.dim, sw: S.W.ctrl });
  AUX.forEach((t, i) => {
    ar(c, [[acx(i), railY], [acx(i), boxY - 0.03]], { dash: 'dash', stroke: S.C.dim });
    node(c, A.x + i * (aw + ag), boxY, aw, boxH, t, { dash: 'dash', stroke: S.C.dim, color: S.C.hint, tag: `X${i + 1}` });
  });
  c.text(A.x, railY - 0.24, 1.6, 0.20, '보조업무', { size: TS.small, bold: true, align: 'left', color: S.C.hint });

  // 이 문서가 확정한 업무 규칙 11개
  head(c, A.x, 3.66, A.w, '이 문서가 확정한 업무 규칙');
  const colW = (A.w - 0.21) / 2, size = 9, lh = lineH(size);
  RULES.forEach((t, i) => {
    const col = i < 6 ? 0 : 1, row = i < 6 ? i : i - 6;
    const x = A.x + col * (colW + 0.21), y = 4.06 + row * 0.42;
    c.text(x, y, 0.30, lh, `${i + 1}.`, { size, align: 'left', color: S.C.hint });
    const lines = wrap(t, size, colW - 0.34);
    if (lines.length > 2) c.warnings.push(`규칙 ${i + 1} ${lines.length}줄`);
    c.text(x + 0.30, y, colW - 0.30, lines.length * lh, '', { size, align: 'left', valign: 'top', color: S.C.data, lines });
  });
}

module.exports = {
  title: '전체 업무 흐름',
  // CP-01~04와 HOL은 P02에도 붙는다(원문 서두). 계열을 프로세스별로만 나누면 P02에 안 붙는 것처럼 읽힌다.
  // 쪽번호로 가리키지 않는다 — 앞에 장이 추가되면 조용히 틀린 참조가 된다.
  caption: '적용 정책: P01~P03 공통 CP-01~04 · HOL / P01 EP / P02 RP · TGT · NEX · AEX / P03 RCP — 프로세스별 대응은 「프로세스 ↔ 정책 추적표」 장 참조',
  draw,
};
