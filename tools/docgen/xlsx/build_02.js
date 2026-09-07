/*
 * 02_검진_예약접수_기능정의.xlsx 생성기
 *
 * 원본(READ-ONLY): docs/baseline/02_Function_Definition.xlsx
 * 산출물          : docs/baseline/output/02_검진_예약접수_기능정의.xlsx
 *
 * 원본은 exceljs 로 읽을 수 없다(모든 요소에 x: 접두사 + ListObject 6개).
 * → JSZip 으로 SpreadsheetML 을 직접 파싱하고, 쓰기만 exceljs 로 한다.
 *
 * 실행: node tools/docgen/xlsx/build_02.js
 *   빌드 후 검증 3종(구조 / 원본 대비 셀 대조 / 금지문자열)을 그대로 출력한다.
 */
'use strict';

const fs = require('fs');
const path = require('path');

const MODROOT = process.env.HCDOC_MODULES || 'D:/tmp/hcwork/gen/node_modules';
function load(name) {
  try { return require(path.join(MODROOT, name)); } catch (e) { return require(name); }
}
const ExcelJS = load('exceljs');
const JSZip = load('jszip');

const ROOT = path.resolve(__dirname, '..', '..', '..');
const SRC = path.join(ROOT, 'docs', 'baseline', '02_Function_Definition.xlsx');
const OUTDIR = path.join(ROOT, 'docs', 'baseline', 'output');
const OUT = path.join(OUTDIR, '02_검진_예약접수_기능정의.xlsx');

const MIDDOT = '\u00B7'; // 원본 전체가 U+00B7 을 쓴다. 리터럴 대신 코드포인트로 고정.

/* ------------------------------------------------------------------ *
 * 1. 원본 파서 (SpreadsheetML 직접 판독)
 * ------------------------------------------------------------------ */

function unescapeXml(s) {
  return s
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'")
    .replace(/&#x([0-9A-Fa-f]+);/g, (m, h) => String.fromCodePoint(parseInt(h, 16)))
    .replace(/&#(\d+);/g, (m, d) => String.fromCodePoint(parseInt(d, 10)))
    .replace(/&amp;/g, '&'); // &amp; 는 반드시 마지막
}

function colToIdx(ref) {
  const m = /^([A-Z]+)/.exec(ref);
  let n = 0;
  for (const ch of m[1]) n = n * 26 + (ch.charCodeAt(0) - 64);
  return n; // 1-based
}

function parseSharedStrings(xml) {
  if (!xml) return [];
  const out = [];
  const siRe = /<(?:\w+:)?si\b[^>]*>([\s\S]*?)<\/(?:\w+:)?si>/g;
  let m;
  while ((m = siRe.exec(xml))) {
    const tRe = /<(?:\w+:)?t\b[^>]*>([\s\S]*?)<\/(?:\w+:)?t>/g;
    let t, buf = '';
    while ((t = tRe.exec(m[1]))) buf += unescapeXml(t[1]);
    out.push(buf);
  }
  return out;
}

/** 시트 XML -> { cells: Map<rowNum, Map<colIdx, value>>, widths: Map<colIdx, width> } */
function parseSheet(xml, sst) {
  const cells = new Map();
  const rowRe = /<(?:\w+:)?row\b([^>]*)(?:\/>|>([\s\S]*?)<\/(?:\w+:)?row>)/g;
  let rm;
  while ((rm = rowRe.exec(xml))) {
    const rAttr = /\br="(\d+)"/.exec(rm[1]);
    if (!rAttr) continue;
    const rowNum = parseInt(rAttr[1], 10);
    const row = new Map();
    const cRe = /<(?:\w+:)?c\b([^>]*)(?:\/>|>([\s\S]*?)<\/(?:\w+:)?c>)/g;
    let cm;
    while ((cm = cRe.exec(rm[2] || ''))) {
      const attrs = cm[1];
      const inner = cm[2] || ''; // self-closing 셀은 값이 없다
      const refM = /\br="([A-Z]+\d+)"/.exec(attrs);
      if (!refM) continue;
      const tM = /\bt="([^"]+)"/.exec(attrs);
      const t = tM ? tM[1] : 'n';
      let val = null;
      if (t === 'inlineStr') {
        const tRe = /<(?:\w+:)?t\b[^>]*>([\s\S]*?)<\/(?:\w+:)?t>/g;
        let x, buf = '';
        while ((x = tRe.exec(inner))) buf += unescapeXml(x[1]);
        val = buf;
      } else {
        const vM = /<(?:\w+:)?v\b[^>]*>([\s\S]*?)<\/(?:\w+:)?v>/.exec(inner);
        if (vM) {
          const raw = unescapeXml(vM[1]);
          if (t === 's') val = sst[parseInt(raw, 10)];      // 공유문자열 인덱스
          else if (t === 'n') val = raw === '' ? null : Number(raw); // No 열
          else val = raw;                                    // t="str" 직접 <v>
        }
      }
      if (val !== null && val !== '') row.set(colToIdx(refM[1]), val);
    }
    if (row.size) cells.set(rowNum, row);
  }

  const widths = new Map();
  const colRe = /<(?:\w+:)?col\b([^>]*)\/>/g;
  let cw;
  while ((cw = colRe.exec(xml))) {
    const mn = /\bmin="(\d+)"/.exec(cw[1]);
    const mx = /\bmax="(\d+)"/.exec(cw[1]);
    const w = /\bwidth="([\d.]+)"/.exec(cw[1]);
    if (mn && mx && w) for (let i = +mn[1]; i <= +mx[1]; i++) widths.set(i, Number(w[1]));
  }
  return { cells, widths };
}

async function readSource(file) {
  const zip = await JSZip.loadAsync(fs.readFileSync(file));
  const wbXml = await zip.file('xl/workbook.xml').async('string');
  const relsXml = await zip.file('xl/_rels/workbook.xml.rels').async('string');
  const sstFile = zip.file('xl/sharedStrings.xml');
  const sst = parseSharedStrings(sstFile ? await sstFile.async('string') : null);

  // 속성 순서가 Type,Target,Id 다. 순서 가정 없이 개별 추출한다.
  const rels = new Map();
  const relRe = /<Relationship\b([^>]*)\/>/g;
  let r;
  while ((r = relRe.exec(relsXml))) {
    const id = /\bId="([^"]+)"/.exec(r[1]);
    const tgt = /\bTarget="([^"]+)"/.exec(r[1]);
    if (id && tgt) rels.set(id[1], tgt[1].replace(/^\//, ''));
  }

  const sheets = new Map();
  const shRe = /<(?:\w+:)?sheet\b([^>]*)\/>/g;
  let s;
  while ((s = shRe.exec(wbXml))) {
    const name = unescapeXml(/\bname="([^"]+)"/.exec(s[1])[1]);
    const target = rels.get(/\br:id="([^"]+)"/.exec(s[1])[1]);
    sheets.set(name, parseSheet(await zip.file(target).async('string'), sst));
  }
  return sheets;
}

/* ------------------------------------------------------------------ *
 * 2. 재작성 규칙 (원본 셀 -> 공개용 문구). 매칭 실패 시 즉시 throw.
 * ------------------------------------------------------------------ */

const REWRITES = [
  // 설계근거 — 맨 앞 시트가 되므로 가장 눈에 띄는 자리
  { sheet: '설계근거', row: 4, col: 5,
    find: '10근무일 과제에서 핵심 업무흐름을 완결하고',
    rep: '핵심 예약' + MIDDOT + '접수 업무흐름을 완결하는 범위로 한정하고' },
  { sheet: '설계근거', row: 4, col: 6, find: '과제 목적/정책', rep: '업무 범위 정책' },

  // 업무Rule
  { sheet: '업무Rule', row: 8, col: 5, find: '과제용 완료이력 Seed/Test Data를', rep: '완료이력 초기 데이터를' },
  { sheet: '업무Rule', row: 8, col: 9, find: 'Seed/Test Data', rep: '초기 데이터' },
  { sheet: '업무Rule', row: 39, col: 5, find: 'Seed Data로 제공하고', rep: '초기 데이터로 제공하고' },
  { sheet: '업무Rule', row: 39, col: 5, find: '1건의 테스트 휴무일을 준비한다.', rep: '1건을 등록한다.' },
  { sheet: '업무Rule', row: 39, col: 9, find: 'Seed Data', rep: '초기 데이터' },

  // 개발범위
  { sheet: '개발범위', row: 20, col: 5, find: 'DB Seed/Test Data', rep: 'DB 초기 데이터' },
  { sheet: '개발범위', row: 21, col: 5, find: 'DB Seed/Test Data', rep: 'DB 초기 데이터' },
  { sheet: '개발범위', row: 22, col: 5, find: 'DB Master/Seed', rep: 'DB Master/초기 데이터' },
  { sheet: '개발범위', row: 23, col: 5, find: 'DB Master/Seed', rep: 'DB Master/초기 데이터' },
  { sheet: '개발범위', row: 24, col: 5, find: 'DB Master/Seed', rep: 'DB Master/초기 데이터' },
  { sheet: '개발범위', row: 24, col: 8, find: '평일1+토요일1 테스트', rep: '평일1+토요일1' },
  { sheet: '개발범위', row: 25, col: 6, find: '과제 범위', rep: '범위 정책' },
  { sheet: '개발범위', row: 26, col: 6, find: '과제 범위', rep: '범위 정책' },
  { sheet: '개발범위', row: 27, col: 6, find: '과제 범위', rep: '범위 정책' },
  { sheet: '개발범위', row: 28, col: 6, find: '과제 범위', rep: '범위 정책' },
  { sheet: '개발범위', row: 29, col: 4, find: '과제용 TGT Rule만 사용', rep: '내부 TGT Rule만 사용' },
  { sheet: '개발범위', row: 30, col: 4, find: 'DB Seed로 제공', rep: 'DB 초기 데이터로 제공' },
  { sheet: '개발범위', row: 30, col: 6, find: '과제 범위', rep: '범위 정책' },
];

/** REWRITES 를 적용하고 (시트,셀,before,after) 목록을 돌려준다. */
function applyRewrites(sheets) {
  const applied = new Map(); // "시트!셀" -> {before, after}
  for (const rw of REWRITES) {
    const row = sheets.get(rw.sheet).cells.get(rw.row);
    const cur = row && row.get(rw.col);
    if (typeof cur !== 'string' || !cur.includes(rw.find)) {
      throw new Error(`재작성 대상 미발견: ${rw.sheet} R${rw.row}C${rw.col} <- ${JSON.stringify(rw.find)}`);
    }
    const key = `${rw.sheet}!${addr(rw.row, rw.col)}`;
    const next = cur.split(rw.find).join(rw.rep);
    row.set(rw.col, next);
    if (applied.has(key)) applied.get(key).after = next;      // 같은 셀 2단계 치환
    else applied.set(key, { before: cur, after: next });
  }
  return applied;
}

function addr(row, col) {
  let s = '', n = col;
  while (n > 0) { const m = (n - 1) % 26; s = String.fromCharCode(65 + m) + s; n = ((n - m) / 26) | 0; }
  return s + row;
}

/* ------------------------------------------------------------------ *
 * 3. 서식
 * ------------------------------------------------------------------ */

const NAVY = 'FF1F3864';
const BAND = 'FFF2F2F2';           // 그룹 토글용 연회색 (ListObject 줄무늬 대체)
const NEX_BASE = 'FFE8EDF5';       // 업무Rule 기본 8종 그룹
const BORDER = { style: 'thin', color: { argb: 'FFD0D0D0' } };
// 업무영역 구분: 진한 남색 계열 명도 램프 (F-PAT-001 이 수검자/접수 양쪽에 나오는 것을 색으로만 보여준다)
const AREA_TINT = { 수검자: 'FFD9E2F3', 예약: 'FFBDD0EC', 접수: 'FFA9C0E4', 공통: 'FFEDF1F8' };

// 출력 시트 순서 + 밴딩 기준 열(값이 바뀌면 배경 토글; 행마다 고유하면 줄무늬가 된다)
const SHEET_PLAN = [
  { name: '설계근거', groupCol: 1 },
  { name: '기능정의', groupCol: 2 },  // 기능 ID (F-RSV-002 4행 연속 그룹이 보여야 한다)
  { name: '업무Rule', groupCol: 1 },  // Rule Group
  { name: '개발범위', groupCol: 1 },  // Scope
  { name: 'DB추적', groupCol: 1 },
];

const HEADER_ROW = 3;
const FIRST_DATA_ROW = 4;

/* ------------------------------------------------------------------ *
 * 0. 문서 안내 시트
 *   36개 기능행 전부가 EP/CP/RP/RCP·P01~P03 코드를 인용하는데 원본의 문서정보 시트를
 *   빼면서 "그 코드가 어느 문서에 정의돼 있는가"까지 같이 사라졌다. 그 항로 표지를 되살린다.
 *   값은 전부 00~03 기준선에서 옮긴 것이고 이 시트는 원본 대조(검증 2) 대상이 아니다.
 * ------------------------------------------------------------------ */

const GUIDE_SHEET = '문서 안내';
const GUIDE_TITLE = '문서 안내  —  동반 문서 · 시트 · 코드 체계 · 용어';
const GUIDE_SUB = '본 파일이 인용하는 정책 ID · 프로세스 ID의 정의는 동반 문서에 있다.';
const GUIDE_COLS = [{ w: 16 }, { w: 30 }, { w: 96 }];
const GUIDE_HEAD = ['구분', '항목', '내용'];

const GUIDE_ROWS = [
  ['동반 문서', '00_검진_예약접수_업무정책', 'CP · EP · RP · RCP 정책과 TGT · NEX · AEX · HOL Rule의 개별 내용. 본 파일 「관련 Policy·Rule」 열이 가리키는 문서다.'],
  ['동반 문서', '01_검진_예약접수_업무프로세스', 'P01 ~ P03 프로세스의 단계 · 분기 · 종료점 · 상태전이. 본 파일 「관련 Process」 열이 가리키는 문서다.'],
  ['동반 문서', '02_검진_예약접수_기능정의', '본 파일. Function ID 16개 / 기능행 36개의 기능 계약, 업무 Rule, 개발 범위, DB 추적.'],
  ['동반 문서', '03_검진_예약접수_화면설계서', '화면 13개의 구성 · 필드 · 검증 · Ribbon Action · 화면 전이.'],
  ['동반 문서', '읽는 순서', '업무정책 → 업무프로세스 → 기능정의 → 화면설계서. 네 문서는 하나의 세트이며 정책 ID · 프로세스 ID · 기능 ID로 서로를 참조한다.'],

  ['시트 안내', '설계근거', '기능 정의를 그렇게 확정한 판단 근거 22행.'],
  ['시트 안내', '기능정의', 'Function ID 16개 / 기능행 36행. 본 파일의 본체다.'],
  ['시트 안내', '업무Rule', 'TGT 5 / NEX 7 / AEX 5 / HOL 5 및 검사코드 Master(EX001~EX019, OPT01~OPT07).'],
  ['시트 안내', '개발범위', '구현 포함 21행 / 제외 8행.'],
  ['시트 안내', 'DB추적', 'Function ID별 주요 테이블과 핵심 입출력 16행.'],

  ['코드 체계', 'CP-01~06', '공통 정책 — 업무 가능일 · 운영요일 · 휴무일 · 운영시간 · 취소 보존 · 변경 이력.'],
  ['코드 체계', 'EP-01~10', '수검자 정책 — 관리 단위 · 내부 식별자 · 차트번호 · 필수정보 · 중복등록 · 변경 제한 · 삭제 범위.'],
  ['코드 체계', 'RP-01~10', '예약 정책 — 예약 단위 · 일정 · 정원 · 당일/현장예약 · 중복예약 · 검진 대상 · 검사 구성 · 변경 · 취소.'],
  ['코드 체계', 'RCP-01~06', '접수 정책 — 예약 전제 · 접수 가능조건 · 중복접수 · 접수 처리 · 접수 변경 · 접수 취소.'],
  // P03은 「접수 관리」다. 「접수 처리」는 하위 단계 P03-01의 이름이라 섞으면 안 된다. (01_Process_Definition.md P03)
  ['코드 체계', 'P01 / P02 / P03', '수검자 확인·관리 / 예약 관리 / 접수 관리 프로세스. 하위 단계는 P01-01 형식으로 표기한다.'],
  ['코드 체계', 'F-PAT / F-RSV / F-RCP / F-COM', '수검자 / 예약 / 접수 / 공통 기능. 본 파일이 정의하는 16개 Function ID의 계열이다.'],

  ['용어', 'TGT', '일반건강검진 대상판정 Rule (Target Eligibility). 대상판정 기준일은 예약일이다.'],
  ['용어', 'NEX', '국가검진 검사오더 생성 Rule (National Examination). 기본 8종 + 조건부 0~3종 = 실제 8~11종.'],
  ['용어', 'AEX', '본인부담 추가검사 Master / Rule (Additional Examination). Master 7종 OPT01~OPT07.'],
  ['용어', 'HOL', '휴무일 Rule (Holiday). 일요일은 항상 업무 불가이고, 월~토라도 활성 휴무일 Master에 등록된 날짜이면 업무 불가다.'],
  ['용어', 'RSV / RCP / CNR / CNC', '예약 / 접수완료 / 예약취소 / 접수취소 상태 코드. 취소는 물리 삭제하지 않고 같은 업무 행의 상태만 바꾸며, 취소 시점에 따라 예약취소(CNR)와 접수취소(CNC)로 나뉘고 복원하지 않는다.'],
  ['용어', 'AM / PM', '오전 / 오후 시간대 코드. 평일은 오전과 오후, 토요일은 오전만 운영한다.'],
  ['용어', 'PatientId / WorkId', 'PatientId는 수검자 Master 내부 식별자, WorkId는 예약 · 접수 업무 1건의 식별자다.'],
  // 비트마스크 한 값이 아니라 OPT01~OPT07 각각의 BIT 파라미터 7개다. 묶인 값으로 적으면 SP 시그니처를 잘못 설계하게 된다.
  ['용어', 'AEX7BIT', 'AEX 7종(OPT01~OPT07)의 선택 여부를 저장 SP에 전달하는 입력 경계. 각 항목마다 BIT 1개씩 총 7개 파라미터로 전달하며 NULL을 허용하지 않고 모두 명시한다. DB추적 시트의 F-RSV-001 · F-RCP-002 · F-COM-004 입출력에 나온다.'],
  ['용어', 'Normal / WalkIn', '신규예약 진입 Context. Normal은 예약일을 선택하고, WalkIn(현장 당일예약)은 예약일을 DB 현재일로 고정한다.'],
  ['용어', '저장 SP', '데이터 변경을 수행하는 Stored Procedure. UI 검증은 사전안내이며 저장 · 상태전이 시점에 DB가 현재 조건을 다시 검증한다.'],
  ['용어', '운영시간', '09:00 <= 현재시각 < 18:00. 18:00부터 업무 불가다.'],
  ['용어', '시간대 정원', '예약일 + 시간대별 20명. 예약(RSV)과 접수완료(RCP)는 각각 1건으로 산정하고 취소(CNR·CNC)는 제외한다.'],
  ['용어', '마감시간 정책', '당일예약 마감 — 평일 오전 10:00 / 평일 오후 15:00 / 토요일 오전 10:00 / 토요일 오후 예약 불가.   접수 마감 — 평일 오전 11:00 / 평일 오후 16:00 / 토요일 오전 11:00 / 토요일 오후 해당 없음.   마감시각 이전(<)만 허용하며 마감시각과 같은 시각부터 불가다.'],
];

function addGuideSheet(wb) {
  const ws = wb.addWorksheet(GUIDE_SHEET, { views: [{ state: 'frozen', ySplit: HEADER_ROW }] });
  GUIDE_COLS.forEach((c, i) => { ws.getColumn(i + 1).width = c.w; });

  const title = ws.getCell(1, 1);
  title.value = GUIDE_TITLE;
  title.font = { bold: true, size: 14, color: { argb: NAVY } };
  title.alignment = { vertical: 'middle' };
  ws.getRow(1).height = 24;

  const sub = ws.getCell(2, 1);
  sub.value = GUIDE_SUB;
  sub.font = { size: 10, italic: true, color: { argb: 'FF595959' } };
  sub.alignment = { vertical: 'middle' };
  ws.getRow(2).height = 18;

  GUIDE_HEAD.forEach((h, i) => {
    const cell = ws.getCell(HEADER_ROW, i + 1);
    cell.value = h;
    cell.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 10 };
    cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: NAVY } };
    cell.alignment = { vertical: 'middle', horizontal: 'center', wrapText: true };
    cell.border = { top: BORDER, left: BORDER, bottom: BORDER, right: BORDER };
  });
  ws.getRow(HEADER_ROW).height = 28;

  let band = false, prevKey = null;
  GUIDE_ROWS.forEach((row, i) => {
    const rn = FIRST_DATA_ROW + i;
    if (prevKey !== null && row[0] !== prevKey) band = !band;
    prevKey = row[0];
    row.forEach((v, ci) => {
      const cell = ws.getCell(rn, ci + 1);
      cell.value = v;
      cell.font = { size: 10 };
      cell.alignment = { vertical: 'top', wrapText: true, horizontal: ci === 0 ? 'center' : 'left' };
      cell.border = { top: BORDER, left: BORDER, bottom: BORDER, right: BORDER };
      if (band) cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: BAND } };
    });
  });

  ws.autoFilter = {
    from: { row: HEADER_ROW, column: 1 },
    to: { row: FIRST_DATA_ROW + GUIDE_ROWS.length - 1, column: GUIDE_HEAD.length },
  };
}

function build(sheets, noteText) {
  const wb = new ExcelJS.Workbook();
  wb.creator = '검진 예약·접수 기능정의';
  wb.created = new Date();

  addGuideSheet(wb);

  for (const plan of SHEET_PLAN) {
    const src = sheets.get(plan.name);
    const rowNums = [...src.cells.keys()].sort((a, b) => a - b);
    const lastRow = rowNums[rowNums.length - 1];
    const lastCol = Math.max(...rowNums.map(n => Math.max(...src.cells.get(n).keys())));

    const ws = wb.addWorksheet(plan.name, { views: [{ state: 'frozen', ySplit: HEADER_ROW }] });
    for (let c = 1; c <= lastCol; c++) ws.getColumn(c).width = src.widths.get(c) || 14;

    // 값 이식 (원본 행/열 위치 그대로)
    for (const rn of rowNums) {
      for (const [c, v] of src.cells.get(rn)) ws.getCell(rn, c).value = v;
    }

    // 1행 제목 (병합하지 않는다 - 빈 셀을 값으로 오염시키지 않고 원본과 동일하게 흘러넘치게 둔다)
    const title = ws.getCell(1, 1);
    title.font = { bold: true, size: 14, color: { argb: NAVY } };
    title.alignment = { vertical: 'middle' };
    ws.getRow(1).height = 24;

    // 개발범위: 삭제되는 문서정보 시트의 '논리 기능' 값을 2행에 보존
    if (plan.name === '개발범위') {
      const note = ws.getCell(2, 1);
      note.value = noteText;
      note.font = { size: 10, italic: true, color: { argb: 'FF595959' } };
      note.alignment = { vertical: 'middle' };
      ws.getRow(2).height = 18;
    }

    // 3행 머리글
    for (let c = 1; c <= lastCol; c++) {
      const cell = ws.getCell(HEADER_ROW, c);
      cell.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 10 };
      cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: NAVY } };
      cell.alignment = { vertical: 'middle', horizontal: 'center', wrapText: true };
      cell.border = { top: BORDER, left: BORDER, bottom: BORDER, right: BORDER };
    }
    ws.getRow(HEADER_ROW).height = 28;

    // 4행부터 데이터: 그룹 변경 지점마다 배경 토글
    let band = false, prevKey = null;
    for (let rn = FIRST_DATA_ROW; rn <= lastRow; rn++) {
      const key = String(ws.getCell(rn, plan.groupCol).value ?? '');
      if (prevKey !== null && key !== prevKey) band = !band;
      prevKey = key;

      const isNexBase = plan.name === '업무Rule'
        && String(ws.getCell(rn, 1).value) === 'NEX'
        && String(ws.getCell(rn, 3).value) === '기본';

      for (let c = 1; c <= lastCol; c++) {
        const cell = ws.getCell(rn, c);
        cell.font = { size: 10 };
        cell.alignment = { vertical: 'top', wrapText: true, horizontal: c === 1 ? 'center' : 'left' };
        cell.border = { top: BORDER, left: BORDER, bottom: BORDER, right: BORDER };
        // 기능정의 업무영역 열은 값별 색이 밴딩보다 우선한다
        const areaTint = (plan.name === '기능정의' && c === 3) ? AREA_TINT[String(cell.value)] : null;
        const argb = areaTint || (isNexBase ? NEX_BASE : (band ? BAND : null));
        if (argb) cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb } };
      }
    }

    ws.autoFilter = {
      from: { row: HEADER_ROW, column: 1 },
      to: { row: lastRow, column: lastCol },
    };
  }
  return wb;
}

/* ------------------------------------------------------------------ *
 * 4. 검증
 * ------------------------------------------------------------------ */

const FORBIDDEN = ['과제', 'Seed', 'Test Data', '테스트', 'FINAL', 'READ-ONLY', '기준선', 'HC-RSV-RCP', 'PASS', 'v1.2'];

function norm(v) {
  if (v === null || v === undefined) return '';
  if (typeof v === 'object' && v.richText) return v.richText.map(t => t.text).join('');
  if (typeof v === 'object' && 'result' in v) return String(v.result);
  return String(v);
}

async function verify(srcSheets, applied, noteText) {
  const wb = new ExcelJS.Workbook();
  await wb.xlsx.readFile(OUT); // 산출물은 exceljs 로 다시 읽힌다
  let fail = 0;
  const say = (ok, msg) => { if (!ok) fail++; console.log(`  ${ok ? 'OK  ' : 'FAIL'} ${msg}`); };

  console.log('\n[검증 1] 구조');
  const names = wb.worksheets.map(w => w.name);
  const wantNames = [GUIDE_SHEET, ...SHEET_PLAN.map(p => p.name)];
  say(names.length === wantNames.length, `시트 ${wantNames.length}개 = ${names.join(' / ')}`);
  say(names.join(',') === wantNames.join(','), '시트 순서');
  say(!names.includes('문서정보') && !names.includes('최종검수'), '문서정보·최종검수 삭제됨');
  const guide = wb.getWorksheet(GUIDE_SHEET);
  say(guide.rowCount - (FIRST_DATA_ROW - 1) === GUIDE_ROWS.length,
    `문서 안내 데이터 ${GUIDE_ROWS.length}행 (실측 ${guide.rowCount - (FIRST_DATA_ROW - 1)})`);
  // 동반 문서 안내가 실제로 나머지 3종을 전부 가리키는지 (오타로 하나 빠지면 안내의 의미가 없다)
  const guideText = GUIDE_ROWS.map(r => r.join(' ')).join('\n');
  say(['00_검진_예약접수_업무정책', '01_검진_예약접수_업무프로세스', '03_검진_예약접수_화면설계서']
    .every(d => guideText.includes(d)), '동반 문서 3종 명시');
  // 본문이 정의 없이 인용하는 코드 계열이 전부 해설되는지
  say(['CP-', 'EP-', 'RP-', 'RCP-', 'P01', 'P02', 'P03', 'TGT', 'NEX', 'AEX', 'HOL']
    .every(k => guideText.includes(k)), '인용 코드 계열 전부 해설됨');

  for (const ws of wb.worksheets) {
    const lastRow = ws.actualRowCount ? ws.rowCount : 0;
    const dataRows = ws.rowCount - (FIRST_DATA_ROW - 1);
    const cols = ws.getRow(HEADER_ROW).cellCount;
    const frozen = ws.views && ws.views[0] && ws.views[0].state === 'frozen' && ws.views[0].ySplit === HEADER_ROW;
    // exceljs 는 읽을 때 autoFilter 를 "A3:I39" 문자열로 되돌려준다(쓸 때는 객체).
    const af = ws.autoFilter;
    const afRef = typeof af === 'string' ? af
      : af ? `${addr(af.from.row, af.from.column)}:${addr(af.to.row, af.to.column)}` : '';
    const want = `A${HEADER_ROW}:${addr(ws.rowCount, cols)}`;
    console.log(`  - ${ws.name}: 데이터 ${dataRows}행 x ${cols}열 (마지막행 ${lastRow})`);
    say(frozen, `  ${ws.name} 틀고정 ySplit=3`);
    say(afRef === want, `  ${ws.name} autoFilter ${afRef || 'none'} (기대 ${want})`);
    say([...Array(cols)].every((_, i) => (ws.getColumn(i + 1).width || 0) > 0), `  ${ws.name} 열 너비 지정됨`);
  }

  const fn = wb.getWorksheet('기능정의');
  const fnRows = fn.rowCount - (FIRST_DATA_ROW - 1);
  const ids = new Set();
  for (let r = FIRST_DATA_ROW; r <= fn.rowCount; r++) ids.add(norm(fn.getCell(r, 2).value));
  say(fnRows === 37, `기능정의 데이터 37행 (실측 ${fnRows})`);
  say(ids.size === 17, `기능정의 고유 Function ID 17개 (실측 ${ids.size})`);

  console.log('\n[검증 2] 원본 대비 셀 단위 대조 (원본을 다시 파싱해 무손상 값과 비교)');
  const diffs = [];
  for (const plan of SHEET_PLAN) {
    const ws = wb.getWorksheet(plan.name);
    const src = srcSheets.get(plan.name); // 무손상 원본 재파싱본
    const rowNums = [...src.cells.keys()];
    const lastRow = Math.max(...rowNums);
    const lastCol = Math.max(...rowNums.map(n => Math.max(...src.cells.get(n).keys())));
    for (let r = 1; r <= lastRow; r++) {
      for (let c = 1; c <= lastCol; c++) {
        const want = norm((src.cells.get(r) || new Map()).get(c));
        const got = norm(ws.getCell(r, c).value);
        if (want !== got) diffs.push({ sheet: plan.name, cell: addr(r, c), want, got });
      }
    }
  }
  // 허용되는 차이는 딱 둘: (a) 재작성 19셀 (b) 개발범위 A2 보존행
  const extra = diffs.filter(d => d.sheet === '개발범위' && d.cell === 'A2' && d.got === noteText && d.want === '');
  const rewritten = diffs.filter(d => {
    const a = applied.get(`${d.sheet}!${d.cell}`);
    return a && a.before === d.want && a.after === d.got;
  });
  const unexpected = diffs.filter(d => !extra.includes(d) && !rewritten.includes(d));
  say(extra.length === 1, `개발범위 A2 보존행 1건 추가: ${noteText}`);
  say(rewritten.length === applied.size, `재작성 차이 ${rewritten.length}건 = 규칙 적용 셀 ${applied.size}건`);
  say(unexpected.length === 0, `그 외 차이 ${unexpected.length}건 (총 비교 차이 ${diffs.length}건)`);
  unexpected.slice(0, 20).forEach(d => console.log(`       ${d.sheet}!${d.cell}  원본=${JSON.stringify(d.want)}  산출=${JSON.stringify(d.got)}`));

  // 재작성문에 넣은 가운뎃점이 원본과 같은 코드포인트(U+00B7)인지 바이트 확인
  const e4 = norm(wb.getWorksheet('설계근거').getCell(4, 5).value);
  say(e4.includes('예약' + MIDDOT + '접수') && !/[‧・･]/.test(e4),
    `설계근거!E4 가운뎃점 U+00B7 확인 (코드포인트 ${[...e4].filter(c => c.codePointAt(0) > 0x2000 || c === MIDDOT).map(c => 'U+' + c.codePointAt(0).toString(16).toUpperCase()).join(',') || '-'})`);

  console.log('\n[검증 3] 금지 문자열');
  const hits = [];
  for (const ws of wb.worksheets) {
    ws.eachRow({ includeEmpty: false }, (row, rn) => {
      row.eachCell({ includeEmpty: false }, (cell, cn) => {
        const s = norm(cell.value);
        for (const bad of FORBIDDEN) if (s.includes(bad)) hits.push(`${ws.name}!${addr(rn, cn)} [${bad}] ${s.slice(0, 60)}`);
      });
    });
  }
  say(hits.length === 0, `금지 문자열 ${hits.length}건`);
  hits.slice(0, 20).forEach(h => console.log('       ' + h));

  console.log('\n[재작성 셀 전수]');
  for (const [key, v] of applied) console.log(`  ${key}\n     before: ${v.before}\n     after : ${v.after}`);

  console.log(`\n===== ${fail === 0 ? '검증 통과' : `검증 실패 ${fail}건`} =====`);
  return fail;
}

/* ------------------------------------------------------------------ */

(async () => {
  const sheets = await readSource(SRC);

  // 원본 사실 확인 (진행 전 필수 게이트)
  const fn = sheets.get('기능정의');
  const fnRows = [...fn.cells.keys()].filter(r => r >= FIRST_DATA_ROW);
  const fnIds = new Set(fnRows.map(r => fn.cells.get(r).get(2)));
  const fnCols = Math.max(...fnRows.map(r => Math.max(...fn.cells.get(r).keys())));
  console.log(`[원본] 시트 ${sheets.size}개 / 기능정의 ${fnRows.length}행 x ${fnCols}열 / 고유 Function ID ${fnIds.size}개`);
  // F-COM-008(변경기록 열람) 등재로 36행 16개 -> 37행 17개가 되었다 (00 CP-06 · 03 §23).
  if (fnRows.length !== 37 || fnCols !== 9 || fnIds.size !== 17) throw new Error('원본 파싱 결과가 기대와 다르다');

  // 삭제되는 문서정보 시트에만 있는 '논리 기능' 값을 개발범위 상단에 보존
  const logical = sheets.get('문서정보').cells.get(4).get(6);
  const noteText = `Function ID ${fnIds.size}개 = ${logical}`;

  const applied = applyRewrites(sheets);

  fs.mkdirSync(OUTDIR, { recursive: true });
  await build(sheets, noteText).xlsx.writeFile(OUT);
  console.log(`[생성] ${OUT}`);

  // 대조는 손대지 않은 원본을 다시 읽어서 한다 (in-memory 치환본과 비교하면 순환 검증이 된다)
  const pristine = await readSource(SRC);
  process.exit(await verify(pristine, applied, noteText) === 0 ? 0 : 1);
})().catch(e => { console.error(e); process.exit(1); });
