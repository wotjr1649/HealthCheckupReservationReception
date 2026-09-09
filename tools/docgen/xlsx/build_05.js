/*
 * 05_검진_예약접수_SP계약서.xlsx 생성기
 *
 * 입력(읽기 전용): docs/baseline/05_DB_Rule_SP_Contract.md
 * 출력           : docs/baseline/output/05_검진_예약접수_SP계약서.xlsx
 *
 * 실행: node tools/docgen/xlsx/build_05.js
 *
 * build_04.js 와 같이 내용을 하드코딩하지 않고 기준선의 표를 읽는다.
 * Parameter 99 · Result Set · ResultCode 개수는 마지막에 대조하고 어긋나면 실패한다.
 */
'use strict';
const M = require('./md.js');

const secs = M.sections(M.read('05_DB_Rule_SP_Contract.md'));

/* 구획을 훑으며 "지금 어느 객체 이야기인가" 를 들고 다닌다.
   SP 제목은 레벨 1 일 수도 2 일 수도 있다(§9 만 레벨 1). 객체 없는 레벨 1 을 만나면 비운다. */
const owned = [];
{
  let cur = null;
  for (const s of secs) {
    const m = s.title.match(/\[dbo\]\.\[(U[SF][PN]_HC_[^\]]+)\]/);
    if (m) cur = m[1];
    else if (s.level === 1) cur = null;
    owned.push({ obj: cur, sec: s });
  }
}
const of = re => owned.filter(o => o.obj && re.test(o.sec.title));

/* ---- 시트 1 : SP 16 + TVF 4 ---- */
const t13 = M.tables(secs.find(s => /^1\.3 외부 호출/.test(s.title)).text)[0];
const t14 = M.tables(secs.find(s => /^1\.4 내부 Inline TVF/.test(s.title)).text)[0];
const code13 = M.tables(secs.find(s => /^13\. SP별 RS0 허용/.test(s.title)).text)[0];
const allowed = {};
for (const r of code13.rows) allowed[r[0]] = r[1];

const S1 = t13.rows.map(r => {
  const name = r[1].replace(/^\[dbo\]\./, '').replace(/[\[\]]/g, '');
  // §13 은 접두사를 뗀 짧은 이름으로 적는다. 뒤에서부터 맞춘다.
  const key = Object.keys(allowed).find(k => name.endsWith(k)) || '';
  return [r[0], name, r[2], r[3], allowed[key] || ''];
});

/* ---- 시트 2 : Parameter ---- */
const S2 = [];
const addParams = (obj, rows, hasMeaning) => {
  let n = 0;
  for (const r of rows) {
    if (!/^@/.test(r[0])) continue;      // '7번째 자리' 같은 다른 표를 걸러낸다
    n++;
    S2.push([obj, n, r[0], r[1], r[2], hasMeaning ? (r[3] || '') : '']);
  }
  return n;
};
for (const o of of(/^(입력|\d+\.\d+\.\d+ 입력|\d+\.\d+ 입력 Signature)$/)) {
  const ts = M.tables(o.sec.text);
  if (ts.length) { addParams(o.obj, ts[0].rows, ts[0].head.length > 3); continue; }
  // 표가 없으면 CREATE PROCEDURE 시그니처 펜스다 (§9.2).
  const f = M.fences(o.sec.text, 'sql')[0] || '';
  let n = 0;
  for (const line of f.split('\n')) {
    const m = line.match(/^\s*(@[^\s,]+)\s+([A-Za-z0-9_()\s]+?)\s*,?\s*$/);
    if (!m || /^\s*(CREATE|AS|BEGIN|END|SET)/i.test(line)) continue;
    S2.push([o.obj, ++n, m[1], m[2].trim(), 'O', '']);
  }
}

/* ---- 시트 3 : Result Set 컬럼 ---- */
const S3 = [];
const rs0 = M.tables(secs.find(s => /^3\.1 RS0/.test(s.title)).text)[0];
const workRs1 = M.tables(secs.find(s => /^11\. 예약 Write SP 계약$/.test(s.title)).text)[0];
const WRITE_RS1 = ['USP_HC_예약_등록', 'USP_HC_예약_변경', 'USP_HC_예약_취소',
                   'USP_HC_접수_완료', 'USP_HC_접수추가검사_변경', 'USP_HC_접수_취소'];

const addRs = (obj, label, t) => {
  // 표 머리글이 [순서|컬럼|…] 인 것과 [컬럼|…] 인 것 두 형태가 있다.
  const off = t.head[0] === '순서' ? 1 : 0;
  const nullIdx = t.head.findIndex(h => h === 'NULL');
  t.rows.forEach((r, i) => S3.push([obj, label, i + 1, r[off], r[off + 1], r[nullIdx] || '']));
};

for (const sp of t13.rows.map(r => r[1].replace(/^\[dbo\]\./, '').replace(/[\[\]]/g, ''))) {
  addRs(sp, 'RS0 처리결과', rs0);
  const own = owned.filter(o => o.obj === sp);
  let n = 0;
  for (const o of own) {
    const m = o.sec.title.match(/^(?:\d+(?:\.\d+)*\s+)?(?:Result Set(?: 순서)?|RS(\d) ?(.*))$/);
    if (!m) continue;
    const ts = M.tables(o.sec.text);
    if (!ts.length) continue;
    n++;
    const label = m[1] ? ('RS' + m[1] + ' ' + (m[2] || '').trim()) : ('RS' + n + ' ' +
      ((M.fences(o.sec.text, 'text')[0] || '').split('\n').find(l => new RegExp('RS' + n + '\\b').test(l)) || '')
        .replace(/^RS\d\s*/, '').trim());
    addRs(sp, label.trim(), ts[0]);
  }
  if (n === 0 && WRITE_RS1.includes(sp)) addRs(sp, 'RS1 Work결과', workRs1);
}

/* ---- 시트 4 : ResultCode ---- */
const S4 = M.tables(secs.find(s => /^4\.2 코드 목록/.test(s.title)).text)[0].rows;

/* ---- 시트 5 : TVF ---- */
const S5 = [];
for (const r of t14.rows) {
  const name = r[1].replace(/^\[dbo\]\./, '').replace(/[\[\]]/g, '');
  S5.push([name, r[0], '책임', '', '', r[2]]);
  for (const o of owned.filter(o => o.obj === name)) {
    const t = M.tables(o.sec.text)[0];
    if (!t) continue;
    if (/입력$/.test(o.sec.title))
      t.rows.filter(x => /^@/.test(x[0]))
            .forEach((x, i) => S5.push([name, '', '입력 ' + (i + 1), x[0], x[1], x[2] === 'X' ? 'NOT NULL' : 'NULL 허용']));
    if (/반환$/.test(o.sec.title)) {
      const off = t.head[0] === '순서' ? 1 : 0;
      t.rows.forEach((x, i) => S5.push([name, '', '반환 ' + (i + 1), x[off], x[off + 1], x[off + 3] || '']));
    }
  }
}

const wb = M.workbook('검진 예약·접수 SP 계약서', [
  // [R14] 제목의 수를 손으로 적지 않는다 — R7 이 휴무일 SP 4개를 더해 20개가 된 뒤에도
  //       이 줄은 '16개' 였고 공개본이 그대로 나갔다 (ROOT AGENTS.md §6).
  { name: 'SP', title: '외부 호출 Stored Procedure ' + S1.length + '개', ctr: [2],
    head: ['ID', '객체명', '구분', '책임', '허용 결과코드'],
    w: [12, 30, 8, 46, 52], rows: S1 },

  { name: 'Parameter', title: 'Parameter 전건', group: 0, ctr: [1, 4],
    head: ['SP / TVF', '순번', 'Parameter', '타입', 'NULL', '의미'],
    w: [28, 6, 24, 16, 7, 44], rows: S2 },

  { name: 'ResultSet', title: 'Result Set 컬럼 전건', group: 0, ctr: [2, 5],
    head: ['SP', 'Result Set', '순서', '컬럼', '타입', 'NULL'],
    w: [28, 22, 6, 22, 18, 7], rows: S3 },

  { name: 'ResultCode', title: 'ResultCode Catalog', ctr: [0],
    head: ['결과코드', 'C# Enum', '기본 결과메시지', '기본 오류항목'],
    w: [10, 26, 56, 22], rows: S4 },

  // [X] S5.length 는 TVF 수가 아니라 입력·반환까지 편 행 수다(실측 51). t14 가 TVF 목록표다.
  { name: 'TVF', title: '내부 Inline TVF ' + t14.rows.length + '개', group: 0,
    head: ['TVF', 'ID', '구분', '이름', '타입', '비고'],
    w: [24, 12, 10, 22, 16, 52], rows: S5 },
]);

const spParams = S2.filter(r => /^USP_/.test(r[0])).length;
const tvfParams = S2.filter(r => /^UFN_/.test(r[0])).length;

/* 기대값은 기준선 05 를 손으로 더한 값이다. 생성기가 표를 하나라도 놓치면 여기서 걸린다.
     RS0        20 SP x 5 컬럼                                                   = 100
     RS1~RS5    10+11+15+7+11+(15+4+3+4)+6+(14+12+5+4+8)+8+3+(3 x 6)             = 158
                + R7 휴무일 (6+3) + (2) + (2)                                     =  13   -> 171
     Parameter  99 + R7 휴무일 (3+4+5+2) 14                                       = 113
     ResultCode 3 + 5 + 7 + 10 + 5 + 4 + 1 + 2 + 3 (05 §4.2 · §16.1 Enum 과 같다)   =  40
                600 폐지로 -1, 800~802 신설로 +3
     TVF 시트   책임 4 + 입력 19 + 반환 (11+5+4+8) 28                              =  51            */
M.emit(wb, '05_검진_예약접수_SP계약서.xlsx', [
  ['SP 20개', S1.length, 20],
  ['SP Parameter 전건', spParams, 113],
  ['TVF Parameter 전건', tvfParams, 19],
  ['Result Set 행 (RS0 100 + RS1~RS5 171)', S3.length, 271],
  ['ResultCode 종수', S4.length, 40],
  ['TVF 시트 행 (책임 4 + 입력 19 + 반환 28)', S5.length, 51],
]).catch(e => { console.error(e); process.exit(1); });
