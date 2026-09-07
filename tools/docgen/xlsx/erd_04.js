/*
 * erd_04.js — 04_검진_예약접수_DB설계서.xlsx 의 논리 ERD · 물리 ERD 시트.
 *
 * exceljs 4.4 는 도형을 못 그리고 이 환경에는 래스터라이저(sharp·canvas·resvg)가 없어
 * 한글이 든 PNG 를 만들 수 없다. 그래서 셀로 그린다 — 테두리·채움·병합만 쓰므로
 * 이미지와 달리 선택·검색·인쇄가 되고 파일이 커지지 않는다. md.js 의 그리기 킷을 쓴다.
 *
 * [!] **좌표만 손으로 둔다.** 상자 안의 내용은 전부 기준선 04 에서 뽑지만 상자의 위치는
 *     6개 테이블을 전제로 배치했다. §7 이 바뀌면 그림이 조용히 깨지므로 guard() 가 먼저 멈춘다.
 */
'use strict';
const M = require('./md.js');

/* "(수검자ID, 완료일자) Clustered" 와 "수검자ID → 수검자.수검자ID, NO ACTION" 양쪽에서 컬럼을 집는다. */
function firstIdents(def) {
  const m = String(def).match(/^\s*\(([^)]*)\)/);
  if (m) return m[1].split(',').map(x => x.trim());
  return [String(def).trim().split(/[\s→,]/)[0]];
}

const NOTE = { size: 9, italic: true, color: { argb: 'FF44546A' } };
const HEAD = { size: 9, bold: true, color: { argb: 'FFFFFFFF' } };
const CTR = { horizontal: 'center' };

/* 작은 표 하나를 그린다. 반환은 다음 빈 행. */
function table(ws, y, c, title, head, rows) {
  M.put(ws, y, c, title, { font: { size: 11, bold: true, color: { argb: M.NAVY } } });
  y++;
  head.forEach((h, i) => M.put(ws, y, c + i, h, { font: HEAD, fill: M.NAVY, align: CTR }));
  y++;
  for (const r of rows) { r.forEach((v, i) => M.put(ws, y, c + i, v)); y++; }
  return y + 1;
}

/*
 * ctx = { secs, owner, t7, S2, find, tbl }  — build_04.js 가 이미 파싱해 둔 것을 받는다.
 * 같은 문서를 두 번 파싱하지 않는다.
 */
function build(ctx) {
  const { secs, owner, t7, S2, find, tbl } = ctx;

  /* ---- 재료 ---- */
  const keys = {};
  for (const s of secs) {
    const m = s.title.match(/^8\.(\d+)\.\d+\s+Key \/ Constraint/);
    if (!m) continue;
    const tn = owner[m[1]];
    keys[tn] = keys[tn] || { pk: [], fk: [], uq: [] };
    for (const r of M.tables(s.text)[0].rows) {
      if (r[0] === 'PK') keys[tn].pk.push(...firstIdents(r[2]));
      else if (r[0] === 'UQ') keys[tn].uq.push(...firstIdents(r[2]));
      else if (r[0] === 'FK') keys[tn].fk.push({
        name: r[1],
        col: firstIdents(r[2])[0],
        ref: (String(r[2]).split('→')[1] || '').split(',')[0].trim(),
      });
    }
  }

  const ent41 = tbl(/^4\.1 Entity 목록/);
  const rel44 = tbl(/^4\.4 관계 및 Cardinality/);
  const sec45 = find(/^4\.5 논리 ERD/) || { text: '' };
  const note45 = sec45.text.split('\n')
    .filter(l => l.trim() && !/^```/.test(l) && !/erDiagram|\|\|--|--o\{/.test(l))
    .join(' ').replace(/`/g, '').trim();
  // §4.5 의 mermaid 관계선. 논리 ERD 가 그리는 실선은 정확히 이 줄들이다.
  const mermaid = (M.fences(sec45.text, 'mermaid')[0] || '')
    .split('\n').map(l => l.trim()).filter(l => /\|\|--o\{/.test(l));

  const TABLES = t7.rows.map(r => r[1]);
  const meta = {};
  t7.rows.forEach(r => { meta[r[1]] = { pk: r[2], fkParent: r[3], role: r[4] }; });
  ent41.rows.forEach(r => { if (meta[r[1]]) { meta[r[1]].kind = r[2]; meta[r[1]].duty = r[3]; } });
  const colsOf = t => S2.filter(r => r[0] === t);

  /* ---- guard ---- */
  const EXPECT = ['수검자', '예약접수', '검사코드', '휴무일', '완료이력', '변경이력'];
  const fail = [];
  if (TABLES.join(',') !== EXPECT.join(','))
    fail.push('ERD 좌표는 다음 6개를 전제로 손으로 배치했다\n  기대: ' + EXPECT.join(', ')
              + '\n  실측: ' + TABLES.join(', ') + '\n  §7 이 바뀌었다 — erd_04.js 의 좌표를 함께 고쳐라');
  if (rel44.rows.length !== 2) fail.push('§4.4 관계가 2건이 아니다: ' + rel44.rows.length);
  if (mermaid.length !== 2) fail.push('§4.5 mermaid 관계선이 2건이 아니다: ' + mermaid.length);
  const nFk = TABLES.reduce((a, t) => a + (keys[t] ? keys[t].fk.length : 0), 0);
  if (nFk !== 2) fail.push('FK 가 2건이 아니다: ' + nFk);
  if (fail.length) {
    fail.forEach(f => console.error('FAIL ERD — ' + f));
    process.exit(1);
  }

  /* ================================================================ *
   * 논리 ERD
   * ================================================================ */
  // [!] 너비 9 를 쓰지 않는다 — exceljs 가 자기 기본값과 같다고 보고 <col> 을 생략한다(emit 이 잡는다).
  const LG = [2, 9.5, 26, 3, 3, 9.5, 26, 3, 3, 9.5, 26, 2];  // G1=2..3 · G2=6..7 · G3=10..11
  const G = { 1: 2, 2: 6, 3: 10 };

  function drawLogical(ws) {
    M.merge(ws, 2, 1, 2, 12);
    M.put(ws, 2, 1, '04 §4.1 Entity · §4.4 Cardinality · §4.5 논리 ERD 에서 생성 · '
                  + '실선 = Foreign Key 2개 · 점선 = FK 가 아닌 코드 문자열 참조', { font: NOTE });

    const mk = (t, r, g) => M.entity(ws, {
      r, c: G[g], w: 2, title: t, sub: meta[t].kind, rows: [['PK', meta[t].pk]],
    });

    mk('수검자', 4, 2);
    mk('예약접수', 10, 1);
    mk('완료이력', 10, 3);
    mk('검사코드', 16, 2);
    mk('휴무일', 21, 1);
    mk('변경이력', 21, 3);

    // 수검자 1:N 예약접수 · 수검자 1:N 완료이력 — §4.4 · §4.5 의 두 줄이 이것이다.
    M.vline(ws, G[2], 7, 8);
    M.hline(ws, 8, G[1] + 1, G[3]);
    M.vline(ws, G[1], 9, 9);
    M.vline(ws, G[3], 9, 9);
    M.put(ws, 7, G[2] + 1, '1', { font: { size: 9, bold: true } });
    M.put(ws, 9, G[1] + 1, 'N', { font: { size: 9, bold: true } });
    M.put(ws, 9, G[3] + 1, 'N', { font: { size: 9, bold: true } });

    // 검사구성 문자열 참조. §4.5 산문이 "FK 관계선이 없다" 고 적은 것을 눈에 보이게 옮긴 것이며
    // 관계선이 아니다 — 그래서 점선이고 이름표를 붙인다.
    M.vline(ws, G[1], 13, 13, 'dashed');
    M.vline(ws, G[3], 13, 13, 'dashed');
    M.hline(ws, 14, G[1] + 1, G[3], 'dashed');
    M.vline(ws, G[2], 15, 15, 'dashed');
    M.merge(ws, 13, G[2], 13, G[2] + 1);
    M.put(ws, 13, G[2], '코드 문자열 참조 · FK 아님',
          { font: { size: 8, bold: true, color: { argb: 'FF8B6914' } }, align: CTR });

    M.merge(ws, 24, G[1], 24, G[3] + 1);
    M.put(ws, 24, G[1], '휴무일 · 변경이력 은 관계선이 없다 — 04 §4.5', { font: NOTE, align: CTR });

    let y = table(ws, 27, 2, '관계 (04 §4.4)', ['부모', '자식', '관계', '의미'], rel44.rows);
    y = table(ws, y, 2, 'Entity (04 §4.1)', ['테이블', '구분', 'PK', '책임'],
              TABLES.map(t => [t, meta[t].kind, meta[t].pk, meta[t].duty]));
    y = table(ws, y, 2, '§4.5 mermaid 원문', ['관계선'], mermaid.map(l => [l]));

    if (note45) {
      M.merge(ws, y, 2, y, 11);
      M.put(ws, y, 2, '04 §4.5 — ' + note45,
            { font: { size: 9, color: { argb: 'FF44546A' } }, align: { wrapText: true, vertical: 'top' } });
      ws.getRow(y).height = 32;
    }
  }

  /* ================================================================ *
   * 물리 ERD
   * ================================================================ */
  const PG = [2, 5, 17, 15, 6, 4, 5, 17, 15, 6, 4, 5, 17, 15, 6, 2];  // G1=2..5 · G2=7..10 · G3=12..15
  const P = { 1: 2, 2: 7, 3: 12 };

  function drawPhysical(ws) {
    M.merge(ws, 2, 1, 2, 16);
    M.put(ws, 2, 1, '04 §7 · §8 에서 생성 · 실선 = Foreign Key · '
                  + 'NULL 열은 04 §8 표기 그대로다 (X = NOT NULL · O = NULL 허용)', { font: NOTE });

    const mk = (t, r, g) => {
      const k = keys[t];
      const fkc = new Set(k.fk.map(f => f.col));
      const rows = colsOf(t).map(cr => {
        const name = cr[2];
        const mark = [
          k.pk.includes(name) ? 'PK' : null,
          fkc.has(name) ? 'FK' : null,
          (!k.pk.includes(name) && !fkc.has(name) && k.uq.includes(name)) ? 'UQ' : null,
        ].filter(Boolean).join(',');
        return [mark, name, cr[3], cr[4]];
      });
      return M.entity(ws, {
        r, c: P[g], w: 4, title: t,
        sub: 'PK ' + meta[t].pk + '  ·  컬럼 ' + rows.length, rows,
      });
    };

    mk('수검자', 4, 1);      // 16컬럼 → 4..21
    mk('예약접수', 4, 2);    // 10컬럼 → 4..15
    mk('검사코드', 4, 3);    //  6컬럼 → 4..11
    mk('완료이력', 24, 1);   //  4컬럼 → 24..29
    mk('휴무일', 24, 2);
    mk('변경이력', 24, 3);

    // 물리 모델에 실재하는 관계는 FK 둘뿐이다 (§4.5). 그 둘만 선으로 그린다.
    const fk예약 = keys['예약접수'].fk[0].name;
    const fk완료 = keys['완료이력'].fk[0].name;

    M.hline(ws, 8, P[1] + 4, P[1] + 4);
    M.put(ws, 7, P[1] + 4, '1 : N', { font: { size: 8, bold: true }, align: CTR });
    M.merge(ws, 9, P[1] + 4, 11, P[1] + 4);
    M.put(ws, 9, P[1] + 4, fk예약,
          { font: { size: 7, color: { argb: 'FF44546A' } }, align: { horizontal: 'center', wrapText: true } });

    M.vline(ws, P[1] + 1, 22, 23);
    M.put(ws, 22, P[1] + 2, '1 : N   ' + fk완료, { font: { size: 8, bold: true } });

    let y = 36;
    M.put(ws, y, 2, '관계선이 없는 이유 (04 §4.5)', { font: { size: 11, bold: true, color: { argb: M.NAVY } } });
    y++;
    for (const [t, why] of [
      ['검사코드', '예약접수·완료이력 의 검사구성 문자열이 코드로 참조한다. FK 가 아니다'],
      ['휴무일', '날짜로만 조회되는 독립 Master 다'],
      ['변경이력', 'Foreign Key 를 갖지 않는다 — 감사 기록은 대상 행보다 오래 산다 (04 §8.6.3)'],
    ]) {
      M.put(ws, y, 2, t, { font: { size: 9, bold: true } });
      M.merge(ws, y, 3, y, 12);
      M.put(ws, y, 3, why, { font: { size: 9 } });
      y++;
    }
    y++;

    const fkRows = [];
    for (const t of TABLES) for (const f of keys[t].fk) fkRows.push([t, f.name, f.col, f.ref]);
    table(ws, y, 2, 'Foreign Key (04 §8)', ['자식 테이블', '이름', '컬럼', '참조'], fkRows);
  }

  return {
    sheets: [
      { name: '논리ERD', title: '논리 ERD — Entity 6 · 관계 2', w: LG, rowHeight: 17, draw: drawLogical },
      { name: '물리ERD', title: '물리 ERD — Table 6 · Foreign Key 2', w: PG, rowHeight: 15, draw: drawPhysical },
    ],
    stat: { tables: TABLES.length, rel: rel44.rows.length, mermaid: mermaid.length, fk: nFk },
  };
}

module.exports = { build };
