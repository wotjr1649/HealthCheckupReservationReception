'use strict';
// 화면 흐름도 — 원문 03 §3 Navigation. 12개 화면 전부와 전달키(PatientId / WorkId)를 한 장에.
// 경로는 노드·그룹 박스를 관통하지 않도록 전용 통로(corridor)로만 우회시킨다.
const S = require('../spec');
const { F } = require('../canvas');

function path(c, pts, o = {}) {
  for (let i = 0; i < pts.length - 1; i++) {
    const last = i === pts.length - 2;
    c.line(pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1],
      { stroke: o.stroke ?? S.C.ink, sw: o.sw ?? 1.0, dash: o.dash, arrow: last });
  }
}
function tag(c, x, y, t, o = {}) {
  const w = F.widthIn(t, 8) + 0.16;
  c.rect(x - w / 2, y - 0.10, w, 0.20, { fill: S.C.white, stroke: S.C.white, sw: 0.25 });
  c.text(x - w / 2, y - 0.10, w, 0.20, t, { size: 8, color: o.color ?? S.C.hint, bold: !!o.bold });
}
function node(c, x, y, w, h, id, name, o = {}) {
  c.rect(x, y, w, h, { sw: o.sw ?? S.W.panel, fill: o.fill ?? S.C.white, stroke: o.stroke ?? S.C.ink });
  c.text(x, y + 0.04, w, 0.18, id, { size: 8, bold: true, color: S.C.hint });
  c.text(x, y + 0.20, w, h - 0.24, name, { size: o.size ?? 9.5, bold: true, color: S.C.data });
  return { x, y, w, h, cx: x + w / 2, cy: y + h / 2, r: x + w, b: y + h };
}

function draw(c) {
  c.text(S.PAGE.title.x, S.PAGE.title.y, 12.4, S.PAGE.title.h, '화면 흐름도',
    { size: S.PAGE.title.size, bold: true, color: '000000', align: 'left' });

  // ── 그룹 1: 수검자 확정 계열 ─────────────────────────────────────
  c.rect(5.00, 1.16, 7.72, 1.66, { stroke: S.C.dim, sw: S.W.hair, dash: 'dash' });
  const PATSEL = node(c, 5.20, 1.52, 2.20, 0.55, 'DLG-PAT-02', '수검자 선택');
  const PATEDIT = node(c, 7.85, 1.52, 2.20, 0.55, 'DLG-PAT-01', '수검자 등록 / 수정');
  const PATDUP = node(c, 10.45, 1.52, 2.10, 0.55, 'DLG-PAT-03', '중복 후보 확인');
  c.text(5.12, 2.58, 4.0, 0.20, '수검자 확정 계열  ·  Modal', { size: 8, align: 'left', bold: true, color: S.C.hint });

  // ── 메인 축 ──────────────────────────────────────────────────────
  const MAIN = node(c, 0.55, 3.50, 1.62, 0.62, 'WF-00', 'MainForm', { fill: S.C.band });
  const PAT = node(c, 2.80, 3.34, 1.95, 0.55, 'WF-PAT-01', '수검자 관리');
  const NEW = node(c, 2.80, 4.14, 1.95, 0.55, 'WF-RSV-01', '신규 예약');
  const WORK = node(c, 2.80, 4.94, 1.95, 0.62, 'WF-WRK-01', '예약 · 접수 Workbench', { size: 9 });

  // ── 그룹 2: 업무 Transaction ────────────────────────────────────
  c.rect(5.00, 5.74, 7.72, 1.14, { stroke: S.C.dim, sw: S.W.hair, dash: 'dash' });
  const NW = 1.40, NG = 0.16;
  const tr = ['DLG-RSV-01|예약 변경', 'CNF-RSV-01|예약 취소', 'DLG-RCP-01|접수 처리', 'DLG-RCP-02|추가검사 변경', 'CNF-RCP-01|접수 취소']
    .map((s, i) => {
      const [id, nm] = s.split('|');
      return node(c, 5.20 + i * (NW + NG), 5.88, NW, 0.52, id, nm, { size: 8.5 });
    });
  c.text(5.12, 6.62, 7.4, 0.20, '업무 Transaction  ·  Modal / Confirm      선택한 Work의 상태에 따라 활성된다',
    { size: 8, align: 'left', bold: true, color: S.C.hint });

  // ── MainForm → 업무 Tab 3개 ─────────────────────────────────────
  const TR = 2.50;
  path(c, [[MAIN.r, MAIN.cy], [TR, MAIN.cy]]);
  c.line(TR, PAT.cy, TR, WORK.cy, { stroke: S.C.ink, sw: 1.0 });
  [PAT, NEW, WORK].forEach(n => path(c, [[TR, n.cy], [n.x, n.cy]]));

  // ── 업무 Tab 사이 ────────────────────────────────────────────────
  path(c, [[PAT.cx, PAT.b], [PAT.cx, NEW.y]]);
  tag(c, PAT.cx + 0.72, (PAT.b + NEW.y) / 2, '선택 PatientId');
  path(c, [[NEW.cx, NEW.b], [NEW.cx, WORK.y]]);
  tag(c, NEW.cx + 1.02, (NEW.b + WORK.y) / 2, '저장 WorkId  /  기존 유효예약 연결');

  // ── 수검자 계열 진입 · 복귀 ─────────────────────────────────────
  // (a) 수검자 관리 → 등록/수정 : 그룹 박스 위 통로(y=1.00)로 우회
  path(c, [[PAT.r, PAT.cy - 0.10], [4.86, PAT.cy - 0.10], [4.86, 1.00], [PATEDIT.cx, 1.00], [PATEDIT.cx, PATEDIT.y]], { dash: 'dash' });
  tag(c, 6.60, 1.00, '신규등록 · 정보수정');
  // (b) 신규 예약 → 수검자 선택 (PatientId 미확정)
  path(c, [[NEW.r, NEW.cy - 0.10], [PATSEL.cx + 0.42, NEW.cy - 0.10], [PATSEL.cx + 0.42, PATSEL.b]]);
  tag(c, 5.95, NEW.cy - 0.10, 'PatientId 미확정');
  // (c) 수검자 선택 → 신규 예약 (PatientId 반환)
  path(c, [[PATSEL.cx - 0.42, PATSEL.b], [PATSEL.cx - 0.42, NEW.cy + 0.14], [NEW.r, NEW.cy + 0.14]]);
  tag(c, 5.20, NEW.cy + 0.14, 'PatientId 반환');
  // (d) 그룹 내부 연쇄
  path(c, [[PATSEL.r, PATSEL.cy], [PATEDIT.x, PATEDIT.cy]]);
  tag(c, (PATSEL.r + PATEDIT.x) / 2, PATSEL.cy - 0.16, '신규등록');
  path(c, [[PATEDIT.r, PATEDIT.cy], [PATDUP.x, PATDUP.cy]]);
  tag(c, (PATEDIT.r + PATDUP.x) / 2, PATEDIT.cy - 0.16, '중복 후보');
  // (e) 복귀 2종 — 노드 아래 통로(y=2.28 / 2.44)
  path(c, [[PATDUP.cx, PATDUP.b], [PATDUP.cx, 2.28], [PATEDIT.cx + 0.35, 2.28], [PATEDIT.cx + 0.35, PATEDIT.b]], { dash: 'dash' });
  tag(c, 11.10, 2.28, '입력값 수정 · 별도 수검자로 계속');
  path(c, [[PATEDIT.cx - 0.35, PATEDIT.b], [PATEDIT.cx - 0.35, 2.44], [PATSEL.cx + 0.85, 2.44], [PATSEL.cx + 0.85, PATSEL.b]], { dash: 'dash' });
  tag(c, 8.05, 2.44, '신규저장 · 기존 수검자 확정');

  // ── 현장 당일예약 순환 (WalkIn) — 오른쪽 바깥 통로 ───────────────
  path(c, [[WORK.r, WORK.cy - 0.12], [12.92, WORK.cy - 0.12], [12.92, 0.86], [PATSEL.cx - 0.85, 0.86], [PATSEL.cx - 0.85, PATSEL.y]], { sw: 1.5 });
  tag(c, 8.60, 0.86, '현장 당일예약   →   수검자 확정 → WalkIn 신규예약 → 접수', { color: S.C.data, bold: true });

  // ── Workbench → 업무 Transaction ────────────────────────────────
  const BT = 5.72;
  path(c, [[WORK.cx, WORK.b], [WORK.cx, BT], [tr[0].cx, BT]]);
  c.line(tr[0].cx, BT, tr[4].cx, BT, { stroke: S.C.ink, sw: 1.0 });
  tr.forEach(n => path(c, [[n.cx, BT], [n.cx, n.y]]));
  tag(c, 4.62, BT - 0.16, '선택 Work + RowVersion');

  // ── 범례 ────────────────────────────────────────────────────────
  c.line(0.60, 6.98, 1.02, 6.98, { stroke: S.C.ink, sw: 1.0, arrow: true });
  c.text(1.10, 6.88, 1.20, 0.20, '주 경로', { size: 8, align: 'left', color: S.C.hint });
  c.line(2.30, 6.98, 2.72, 6.98, { stroke: S.C.ink, sw: 1.0, dash: 'dash', arrow: true });
  c.text(2.80, 6.88, 2.40, 0.20, '복귀 · 보조 경로', { size: 8, align: 'left', color: S.C.hint });
  c.text(0.60, 7.16, 12.2, 0.20, '전달키는 PatientId 또는 WorkId 두 가지뿐이며 화면에 노출하지 않는다. WorkId를 전달받으면 조회조건과 무관하게 최신 한 건을 직접 조회해 자동 선택한다.',
    { size: 8, align: 'left', color: S.C.hint });
}

module.exports = { pages: [{ title: '화면 흐름도', fullWidth: true, draw }] };
