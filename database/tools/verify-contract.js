// 의존성 없음. node 표준 라이브러리만 사용.
const fs = require('fs');

const [, , outPath, key] = process.argv;
if (!outPath || !key) { console.error('usage: node verify-contract.js <sqlcmd출력파일> <SP키>'); process.exit(2); }

// [X] FAIL 을 stdout 으로 낸다. 초안은 console.error 만 써서 증거파일에 구조적으로 PASS 만 남았다.
//     stdout·stderr 양쪽에 쓰면 러너의 >> "$OUT" 2>&1 때문에 같은 FAIL 이 두 번 찍힌다(실측).
//     한 곳으로만 낸다. 판정은 exit code 가 한다.
const say = (m) => console.log(m);
const bad = (m) => console.log(m);

// sqlcmd -u 출력은 UTF-16LE + BOM
const buf = fs.readFileSync(outPath);
const text = buf.slice(0, 2).equals(Buffer.from([0xff, 0xfe]))
  ? buf.slice(2).toString('utf16le')
  : buf.toString('utf8');

const lines = text.split(/\r?\n/);
const isSep = (s) => /^-+( +-+)*\s*$/.test(s) || /^-+(\|-+)*\s*$/.test(s);

// 구분선을 마커로 Result Set 을 분리한다.
// 구분선 바로 위 줄이 헤더, 다음 구분선(또는 EOF)까지가 데이터행.
// RS0 의 첫 데이터행은 따로 보관한다 — 스펙 §36.4 는 형상이 아니라 Code 값 일치를 요구한다.
const sets = [];
for (let i = 0; i < lines.length; i++) {
  if (!isSep(lines[i]) || i === 0) continue;
  const columns = lines[i - 1].split('|').map((s) => s.trim()).filter((s) => s.length);
  let rows = 0, firstRow = null;
  for (let j = i + 1; j < lines.length; j++) {
    if (j + 1 < lines.length && isSep(lines[j + 1])) break;   // 다음 RS 의 헤더
    if (!lines[j].trim()) continue;
    if (/^\(\d+ /.test(lines[j].trim())) continue;             // "(N rows affected)"
    if (rows === 0) firstRow = lines[j].split('|').map((s) => s.trim());
    rows++;
  }
  sets.push({ columns, rows, firstRow });
}

const expected = JSON.parse(fs.readFileSync(`${__dirname}/expected-contracts.json`, 'utf8'))[key];
if (!expected) { bad(`FAIL 기대 계약 없음: ${key}`); process.exit(1); }

let fail = 0;

// (1) RS0 의 Success·Code 값
const rs0 = sets[0];
const pick = (name) => {
  if (!rs0 || !rs0.firstRow) return undefined;
  const k = rs0.columns.indexOf(name);
  return k < 0 ? undefined : rs0.firstRow[k];
};
const obsSuccess = pick('Success'), obsCode = pick('Code');
if (expected.rs0Success !== undefined && Number(obsSuccess) !== expected.rs0Success) {
  bad(`FAIL ${key} RS0.Success 관측 ${obsSuccess} != 기대 ${expected.rs0Success}`); fail++;
}
if (expected.rs0Code !== undefined && Number(obsCode) !== expected.rs0Code) {
  bad(`FAIL ${key} RS0.Code 관측 ${obsCode} != 기대 ${expected.rs0Code}`); fail++;
}

// (2) 허용집합 대조는 tools/allowed-codes.json 이 있을 때만 한다 (T36 Step 5 가 만든다).
//     없으면 조용히 통과시키지 않고 NOT RUN 으로 남긴다 — 미실행은 PASS 가 아니다 (CLAUDE.md §10).
const allowedPath = `${__dirname}/allowed-codes.json`;
if (fs.existsSync(allowedPath)) {
  const allowed = JSON.parse(fs.readFileSync(allowedPath, 'utf8'))[expected.sp];
  if (Array.isArray(allowed) && !allowed.includes(Number(obsCode))) {
    bad(`FAIL ${key} RS0.Code ${obsCode} 가 ${expected.sp} 의 허용집합 밖`); fail++;
  }
} else {
  say(`NOT RUN ${key} 허용집합 대조 — tools/allowed-codes.json 이 아직 없다 (T36)`);
}

// (3) RS 개수·컬럼·행수
if (sets.length !== expected.resultSets.length) {
  bad(`FAIL ${key} Result Set 개수 ${sets.length} != 기대 ${expected.resultSets.length}`);
  fail++;
}
expected.resultSets.forEach((e, i) => {
  const a = sets[i];
  if (!a) { bad(`FAIL ${key} RS${i} 누락`); fail++; return; }
  if (a.columns.join(',') !== e.columns.join(',')) {
    bad(`FAIL ${key} RS${i} 컬럼 불일치\n  실측: ${a.columns.join(',')}\n  기대: ${e.columns.join(',')}`);
    fail++;
  }
  const lo = e.rows !== undefined ? e.rows : e.rowsMin;
  const hi = e.rows !== undefined ? e.rows : e.rowsMax;
  if (a.rows < lo || a.rows > hi) {
    bad(`FAIL ${key} RS${i} 행수 ${a.rows} 가 기대 ${lo}~${hi} 밖`);
    fail++;
  }
});

if (fail === 0) say(`PASS ${key} Result Set 계약 일치 (${sets.length}개 RS · RS0 Code=${obsCode})`);
process.exit(fail === 0 ? 0 : 1);
