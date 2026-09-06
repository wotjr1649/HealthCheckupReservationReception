const x = require('../tools/xlsx-edit.js');
const fs = require('fs');
const SRC = '../docs/baseline/02_Function_Definition.xlsx';
const items = x.read(SRC);
const MAP = [
  ['예약접수, 검사항목 + TGT/NEX/AEX/HOL', '예약접수(검사구성) + TGT/NEX/AEX/HOL', 2],
  ['예약접수, 검사항목 + HOL/마감',        '예약접수(검사구성) + HOL/마감',        1],
  ['예약접수, 검사항목 + AEX',             '예약접수(검사구성) + AEX',             1],
  ['예약접수, 검사항목, 수검자, 검사코드',  '예약접수(검사구성), 수검자, 검사코드',  1],
  ['검사코드, 수검자.HepatitisBExcluded, 검사항목', '검사코드, 수검자.B형간염제외여부, 예약접수(검사구성)', 1],
  ['조회조건/WorkId → Work/Detail',        '조회조건/WorkId → Work + 검사구성',    1],
  ['Work Aggregate Transaction',           'Work 1행 Transaction',                 1],
  ['NEX 실제 8~11행 / EX012 공통코드 중복차단', 'NEX 실제 8~11종 / EX012 공통코드 중복차단', 1],
];
let total = 0;
for (const it of items) {
  if (it.name.indexOf('sheet6.xml') < 0) continue;
  let t = x.text(it), n = 0;
  for (const [o, v, want] of MAP) {
    const c = t.split(o).length - 1;
    if (c !== want) throw new Error('앵커 ' + c + '건 (기대 ' + want + '): ' + o);
    t = t.split(o).join(v); n += c;
  }
  x.setText(it, t);
  console.log('  sheet6.xml  ' + n + '곳 치환');
  total += n;
}
if (!total) throw new Error('sheet6 를 찾지 못했다');
x.write(SRC, items);
console.log('02_Function_Definition.xlsx 재작성 완료');
