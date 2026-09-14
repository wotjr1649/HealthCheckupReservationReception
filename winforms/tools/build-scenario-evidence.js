#!/usr/bin/env node
/*
 * 시나리오 화면 증빙을 산출물로 옮긴다 — `docs/phase5/output/screens/`
 *
 * 과제 브리프 §9 가 화면 측 증빙을 요구한다. 그림을 뜨는 것은 시험이고
 * (`tests/.../Visual/ScenarioCaptureTests.cs`), 이 생성기는 **무엇이 증빙인지**를 문서에서
 * 읽어 그것만 산출물로 옮긴다.
 *
 *   docs/phase5/2026-09-14-Test-Scenarios.md  §2.5 의 표   -> 무엇이 증빙인가
 *   winforms/artifacts/logs/scenario/*.png                 -> 시험이 뜬 그림 (.gitignore)
 *   docs/phase5/output/screens/*.png                       -> 산출물 (커밋한다)
 *
 * `[!]` **목록의 단일 출처는 문서다** (ROOT AGENTS.md §6). 여기에 파일 이름을 적지 않는다 —
 *       적으면 그 순간 값이 두 곳이 되고, 둘이 되면 한쪽만 고쳐지는 날이 온다.
 *       표에 줄을 더하고 다시 돌리면 된다.
 *
 * `[!]` **빠진 그림을 조용히 넘기지 않는다.** 하나라도 없거나 비어 있으면 `exit 1` 이다.
 *       빠진 채 지나가면 표는 그림을 가리키는데 산출물에는 없는 상태로 제출된다.
 *
 * `[X]` **`scripts/test.sh` 에 넣지 않는다.** 그림을 다시 뜨려면 실물 DB 와 화면이 필요하고,
 *       그 회귀는 「어디서나 같은 판정」이 규칙이다. 대신 이 생성기가 스스로 판정한다.
 *
 *   node tools/build-scenario-evidence.js
 */
'use strict';

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..');
const DOC = path.resolve(ROOT, '..', 'docs', 'phase5', '2026-09-14-Test-Scenarios.md');
const SRC = path.join(ROOT, 'artifacts', 'logs', 'scenario');
const OUT = path.resolve(ROOT, '..', 'docs', 'phase5', 'output', 'screens');

/* 빈 그림을 거르는 바닥. 캡처 시험이 거는 것과 같은 값이다 — 폼이 안 그려지면 파일만 생긴다. */
const MIN_BYTES = 10 * 1024;

let failed = 0;
function fail(line) {
  console.log('FAIL ' + line);
  failed = 1;
}

/* 문서가 백틱으로 감싼 `*.png` 가 곧 증빙 목록이다. 표의 칸 위치에 기대지 않는다. */
function wanted() {
  const doc = fs.readFileSync(DOC, 'utf8');
  const names = [];
  const re = /`([^`\s]+\.png)`/g;
  let m;
  while ((m = re.exec(doc)) !== null) {
    if (names.indexOf(m[1]) === -1) names.push(m[1]);
  }
  return names;
}

function main() {
  if (!fs.existsSync(DOC)) {
    fail('시나리오 문서가 없다: ' + DOC);
    return;
  }

  const names = wanted();
  if (!names.length) {
    fail('문서에서 증빙 그림 이름을 하나도 못 읽었다 — 표 형식이 바뀌었다: ' + DOC);
    return;
  }

  fs.mkdirSync(OUT, { recursive: true });

  for (const name of names) {
    const from = path.join(SRC, name);
    if (!fs.existsSync(from)) {
      fail('문서가 가리키는 그림이 없다: ' + name + ' — 캡처 시험을 먼저 돌린다');
      continue;
    }

    const size = fs.statSync(from).size;
    if (size < MIN_BYTES) {
      fail('그림이 비었다: ' + name + ' (' + size + ' bytes) — 폼이 그려지지 않았다');
      continue;
    }

    fs.copyFileSync(from, path.join(OUT, name));
    console.log('PASS ' + name + ' (' + Math.round(size / 1024) + 'KB)');
  }

  /* 표에서 빠진 그림이 산출물에 남아 있으면 제출본에 설명 없는 그림이 섞인다. */
  for (const left of fs.readdirSync(OUT)) {
    if (left.endsWith('.png') && names.indexOf(left) === -1) {
      fs.unlinkSync(path.join(OUT, left));
      console.log('지움 ' + left + ' — 문서가 가리키지 않는다');
    }
  }
}

main();

console.log('');
if (failed) {
  console.log('=== 화면 증빙 FAIL ===');
} else {
  console.log('=== 화면 증빙 PASS — ' + OUT.replace(/\\/g, '/') + ' ===');
}
process.exit(failed);
