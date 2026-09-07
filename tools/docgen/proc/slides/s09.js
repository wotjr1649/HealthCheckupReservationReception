'use strict';
// 9 — 상태전이(RSV/RCP/CNR/CNC) + P03-05 No-op + 동시성 및 잠금 경계
const { A, node, gate, term, head, note, ar, elab, elabN, table, S, F, wrap } = require('../flow');

const NS = { size: 8 };

function state(c, x, y, w, h, label, sub) {
  c.ellipse(x, y, w, h, { stroke: S.C.ink, sw: S.W.win, fill: S.C.white });
  c.text(x, y + 0.12, w, h * 0.48, label, { size: 12, bold: true, color: S.C.data });
  c.text(x, y + h * 0.56, w, h * 0.30, sub, { size: 8, color: S.C.hint });
  return { cx: x + w / 2, cy: y + h / 2, x, y, w, h, r: x + w, b: y + h };
}

function draw(c) {
  // ── 상태전이도 ───────────────────────────────────────────────────
  head(c, A.x, 1.06, 6.20, '상태전이   —   동일 업무 행의 RSV / RCP / CNR / CNC 전이');

  node(c, 0.50, 2.05, 1.40, 0.40, '신규예약', { size: 9 });
  const RSV = state(c, 2.40, 1.80, 1.70, 0.90, 'RSV', '예약');
  const RCP = state(c, 5.20, 1.80, 1.70, 0.90, 'RCP', '접수완료');
  const CNR = state(c, 2.40, 3.60, 1.70, 0.90, 'CNR', '예약취소');
  const CNC = state(c, 5.20, 3.60, 1.70, 0.90, 'CNC', '접수취소');

  ar(c, [[1.90, 2.25], [RSV.x - 0.03, 2.25]]);
  ar(c, [[RSV.r, 2.25], [RCP.x - 0.03, 2.25]]);
  elab(c, (RSV.r + RCP.x) / 2, 2.10, '접수');
  // 자기전이
  ar(c, [[RSV.cx - 0.22, RSV.y + 0.06], [RSV.cx - 0.22, 1.56], [RSV.cx + 0.22, 1.56], [RSV.cx + 0.22, RSV.y + 0.03]]);
  c.text(RSV.cx - 0.85, 1.40, 1.70, 0.16, '예약변경', { size: 8, color: S.C.hint });
  ar(c, [[RCP.cx - 0.22, RCP.y + 0.06], [RCP.cx - 0.22, 1.56], [RCP.cx + 0.22, 1.56], [RCP.cx + 0.22, RCP.y + 0.03]]);
  c.text(RCP.cx - 0.85, 1.40, 1.70, 0.16, 'AEX 변경', { size: 8, color: S.C.hint });
  // 취소 — 예약 단계 취소는 CNR, 접수 단계 취소는 CNC 로 갈라져 합류점이 없다.
  // 각각 RSV / RCP 와 같은 cx 바로 아래에 두고 세로로 내린다 — 도착 상태 이름이 곧 간선 라벨이다.
  ar(c, [[RSV.cx, RSV.b], [RSV.cx, CNR.y - 0.03]]);
  ar(c, [[RCP.cx, RCP.b], [RCP.cx, CNC.y - 0.03]]);
  ar(c, [[CNR.cx, CNR.b], [CNR.cx, 4.80]], { dash: 'dash', stroke: S.C.dim });
  ar(c, [[CNC.cx, CNC.b], [CNC.cx, 4.80]], { dash: 'dash', stroke: S.C.dim });
  node(c, 2.80, 4.84, 3.70, 0.34, 'CNR · CNC → 모든 상태   허용하지 않음 (복원 경로 없음)',
    { size: 9, dash: 'dash', stroke: S.C.dim, color: S.C.hint });

  // ── 상태전이 규칙 ────────────────────────────────────────────────
  head(c, 7.00, 1.06, A.r - 7.00, '상태전이 규칙');
  table(c, 7.00, 1.40, [
    { t: '전이', w: 1.35 },
    { t: '업무', w: 1.35 },
    { t: '비고', w: A.r - 7.00 - 2.70, align: 'left' },
  ], [
    ['RSV → RSV', '예약변경', '일정·검사구성의 영향범위만 저장'],
    ['RSV → RCP', '접수', '예약일·시간대·검사구성 유지, 상태만 변경'],
    ['RSV → CNR', '예약취소', '동일 업무 행을 CNR로 변경'],
    ['RCP → RCP', 'AEX 변경', '접수 변경(P03-05). RowVersion 갱신'],
    ['RCP → CNC', '접수취소', 'RSV로 복원하지 않는다'],
    ['CNR → *', '허용하지 않음', '재진행은 신규예약을 생성한다'],
    ['CNC → *', '허용하지 않음', '재진행은 신규예약을 생성한다'],
  ], { size: 9.5, headSize: 9.5, rowH: 0.28, headH: 0.30 });
  note(c, 7.00, 3.68, A.r - 7.00, ['순환 상태전이와 복원 경로는 없다.'], { size: 9.5, color: S.C.hint, stroke: S.C.dim });

  // ── P03-05 접수 변경 : No-op ─────────────────────────────────────
  head(c, 7.00, 4.06, A.r - 7.00, 'P03-05  접수 변경   —   동일 AEX 집합 저장은 No-op');
  const NX = [7.00, 9.06, 11.12], NW = 1.75, NH = 0.44;
  const ncx = i => NX[i] + NW / 2, nr = i => NX[i] + NW;
  const NR = [4.40, 5.02];
  gate(c, NX[0], NR[0], NW, NH, '현재 상태 = RCP?', NS);
  node(c, NX[1], NR[0], NW, NH, 'AEX 추가 / 제거 ·\nAEX 검사구성 검증', NS);
  gate(c, NX[2], NR[0], NW, NH, '실제 AEX 변경 있음?', NS);
  term(c, NX[0], NR[1], NW, NH, '접수 변경 불가', NS);
  term(c, NX[1], NR[1], NW, NH, '변경 없음 / No-op', NS);
  node(c, NX[2], NR[1], NW, NH, 'AEX Detail 변경 +\nWork 동시성 갱신', NS);
  ar(c, [[nr(0), NR[0] + NH / 2], [NX[1] - 0.03, NR[0] + NH / 2]]);
  elab(c, (nr(0) + NX[1]) / 2, NR[0] + NH / 2, 'Yes');
  ar(c, [[nr(1), NR[0] + NH / 2], [NX[2] - 0.03, NR[0] + NH / 2]]);
  ar(c, [[ncx(0), NR[0] + NH], [ncx(0), NR[1] - 0.03]]);
  elab(c, ncx(0) + 0.24, (NR[0] + NH + NR[1]) / 2, 'No');
  ar(c, [[ncx(2) + 0.36, NR[0] + NH], [ncx(2) + 0.36, NR[1] - 0.03]]);
  elab(c, ncx(2) + 0.62, (NR[0] + NH + NR[1]) / 2, 'Yes');
  ar(c, [[ncx(2) - 0.36, NR[0] + NH], [ncx(2) - 0.36, 4.93], [ncx(1), 4.93], [ncx(1), NR[1] - 0.03]]);
  elab(c, ncx(2) - 0.62, 4.93, 'No');
  c.text(7.00, 5.54, A.r - 7.00, 0.20, '예약일·시간대·NEX는 ReadOnly. 실제 AEX 변경 시에만 Work의 RowVersion이 바뀐다.',
    { size: 9, align: 'left', color: S.C.hint });

  // P03 전체 프로세스(업무 구분)는 상태전이의 진입점이므로 여기에 한 줄로 둔다.
  note(c, A.x, 5.20, 6.20, [
    'P03 업무 구분   —   접수 → P03-01 접수 처리   ·   조회 → P03-04 접수 조회',
    '변경 → P03-05 접수 변경   ·   취소 → P03-06 접수 취소',
  ], { size: 9, color: S.C.hint, stroke: S.C.dim });

  // ── 동시성 및 잠금 경계 ──────────────────────────────────────────
  head(c, A.x, 5.80, A.w, '동시성 및 잠금 경계');
  note(c, A.x, 6.12, A.w, [
    '· 수검자 수정과 같은 Patient의 신규예약 생성이 경합할 수 있으므로 Patient 단위 직렬화가 필요하다.',
    '· 신규예약·예약이동은 Patient와 대상 Slot을 같은 Transaction에서 잠그고 정원·중복을 재조회한다.',
    '· 예약이동의 중복조회에서는 변경 대상 WorkId를 제외하고 다른 유효업무만 확인한다.',
    '· 예약이동에서 기존 Slot과 신규 Slot을 함께 잠글 경우 정렬된 순서로 획득하여 교착 위험을 줄인다.',
  ], { size: 9.5 });
}

module.exports = {
  title: '상태전이   ·   동시성 경계',
  caption: '적용 정책: P03-05 RP-08, AEX, RCP-05 / P03-06 CP-05, RCP-06 — 상태 해석과 취소 보존은 CP-05, 전이 조건은 RP-09·RP-10, RCP-02~06',
  draw,
};
