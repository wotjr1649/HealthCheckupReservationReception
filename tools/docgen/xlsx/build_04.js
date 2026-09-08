/*
 * 04_검진_예약접수_DB설계서.xlsx 생성기
 *
 * 입력(읽기 전용): docs/baseline/04_DB_Design.md
 * 출력           : docs/baseline/output/04_검진_예약접수_DB설계서.xlsx
 *
 * 실행: node tools/docgen/xlsx/build_04.js
 *
 * 내용을 하드코딩하지 않는다 — 기준선의 표를 그대로 읽는다. 기준선을 고치면 이 파일을
 * 고치지 않아도 산출물이 따라온다. 개수는 마지막에 실측과 대조하고 어긋나면 실패한다.
 */
'use strict';
const M = require('./md.js');

const src = M.read('04_DB_Design.md');
const secs = M.sections(src);
const find = re => secs.find(s => re.test(s.title));
const tbl = (re, i) => { const s = find(re); return s ? (M.tables(s.text)[i || 0] || null) : null; };

/* 8.N 구획의 테이블 이름을 먼저 모은다 — 하위 8.N.M 이 어느 테이블 것인지 알아야 한다. */
const owner = {};
for (const s of secs) {
  const m = s.title.match(/^8\.(\d+)\s+(\S+)$/);
  if (m) owner[m[1]] = m[2];
}

/* 시트 1 — 테이블 요약 */
const t7 = tbl(/^7\. 물리 테이블 요약/);
const S1 = t7.rows.map(r => r);

/* 시트 2 — 컬럼 전건 */
const S2 = [];
for (const s of secs) {
  const m = s.title.match(/^8\.(\d+)\.\d+\s+컬럼 \((\d+)\)$/);
  if (!m) continue;
  const t = M.tables(s.text)[0];
  for (const r of t.rows) S2.push([owner[m[1]], r[0], r[1], r[2], r[3], r[4], r[5]]);
}

/* 시트 3 — 제약. 구획마다 머리글이 조금씩 다르므로 [테이블·구분·이름·정의] 로 맞춘다. */
const S3 = [];
for (const s of secs) {
  const m = s.title.match(/^8\.(\d+)\.\d+\s+(Key \/ Constraint.*|Default Constraint)$/);
  if (!m) continue;
  const t = M.tables(s.text)[0];
  const isDf = /^Default/.test(m[2]);
  for (const r of t.rows) {
    if (isDf) S3.push([owner[m[1]], 'DF', r[0], r[1] + ' = ' + r[2]]);
    else      S3.push([owner[m[1]], r[0], r[1], r[2]]);
  }
}

/* 시트 4 — 인덱스. Key/Constraint/Index 합본 구획의 UX 행도 인덱스다. */
const S4 = [];
for (const s of secs) {
  const m = s.title.match(/^8\.(\d+)\.\d+\s+Index$/);
  if (!m) continue;
  const t = M.tables(s.text)[0];
  for (const r of t.rows) S4.push([owner[m[1]], r[0], r[1], r[2], r[3]]);
}
for (const s of secs) {
  const m = s.title.match(/^8\.(\d+)\.\d+\s+Key \/ Constraint \/ Index$/);
  if (!m) continue;
  for (const r of M.tables(s.text)[0].rows)
    if (r[0] === 'UX') S4.push([owner[m[1]], r[1], r[2], '', '필터형 고유 인덱스']);
}

/* 시트 5 — 명명규칙 */
const t33 = tbl(/^3\.3 명명규칙/);
const S5 = t33.rows.map(r => r);

/* 시트 A·B — 논리 ERD · 물리 ERD.
   같은 문서를 두 번 파싱하지 않도록 여기서 파싱한 것을 넘긴다. */
const erd = require('./erd_04.js').build({ secs, owner, t7, S2, find, tbl });

const wb = M.workbook('검진 예약·접수 DB 설계서', [
  ...erd.sheets,

  { name: '테이블', title: '물리 테이블 6개', group: 0, ctr: [0],
    head: ['No', '테이블', 'PK', '주요 FK', '핵심 고유성 / 역할'],
    w: [6, 14, 24, 12, 46], rows: S1 },

  { name: '컬럼', title: '컬럼 전건', group: 0, ctr: [1, 3],
    head: ['테이블', 'No', '컬럼', '타입', 'NULL', 'Default / 생성', '설명'],
    w: [12, 5, 20, 22, 7, 30, 56], rows: S2 },

  { name: '제약', title: 'PK / FK / UQ / UX / CK / DF', group: 0, ctr: [1],
    head: ['테이블', '구분', '이름', '정의'],
    w: [12, 7, 34, 78], rows: S3 },

  { name: '인덱스', title: '인덱스', group: 0,
    head: ['테이블', '이름', 'Key', 'INCLUDE / Filter', '목적'],
    w: [12, 30, 34, 40, 34], rows: S4 },

  { name: '명명규칙', title: '명명규칙', head: ['객체', '규칙', '예시'],
    w: [24, 34, 30], rows: S5 },
]);

/* 개수는 기준선 §7·§8 이 스스로 선언한 값과 맞아야 한다. 어긋나면 생성기를 실패시킨다. */
const declared = secs
  .map(s => s.title.match(/^8\.\d+\.\d+\s+컬럼 \((\d+)\)$/))
  .filter(Boolean).reduce((a, m) => a + Number(m[1]), 0);

M.emit(wb, '04_검진_예약접수_DB설계서.xlsx', [
  ['ERD Entity', erd.stat.tables, 6],
  ['ERD 관계 (§4.4)', erd.stat.rel, 2],
  ['ERD 관계선 (§4.5 mermaid)', erd.stat.mermaid, 2],
  ['ERD Foreign Key (§8)', erd.stat.fk, 2],
  ['테이블 수', S1.length, 6],
  // [!] 여기 `['컬럼 전건 (04 §8 실측)', S2.length, 48]` 이 한 줄 더 있었다. 바로 윗줄이
  //     `declared`(§8 소제목의 (N) 합계)로 이미 같은 것을 보고 있었으므로 48 은 같은 값의
  //     손으로 유지하는 사본이었다(ROOT AGENTS.md §6). R7 에서 53 이 되자 윗줄은 스스로
  //     따라왔고 이 줄만 깨졌다. 컬럼 집합의 진짜 방어는 DOC-001(04 §8 <-> 실제 DB)이다.
  ['컬럼 전건 (§8 선언 합계와 대조)', S2.length, declared],
  ['제약 전건', S3.length, 47],
  ['인덱스 전건', S4.length, 8],
  ['명명규칙 행', S5.length, 12],
]).catch(e => { console.error(e); process.exit(1); });
