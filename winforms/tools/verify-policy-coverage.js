#!/usr/bin/env node
/*
 * 정책 ↔ DB 검증 대조표 게이트 — `docs/phase5/2026-09-15-Policy-DB-Coverage.md`
 *
 * `00_Project_Policy.md` 가 세운 규칙 ID **전건**이 대조표에 한 줄씩 있는지 본다.
 *
 *   POL-001  00 의 규칙 ID 가 전부 대조표에 있다      규칙이 늘었는데 표가 그대로면 red
 *   POL-002  대조표의 규칙 ID 가 전부 00 에 있다      이름이 바뀌거나 오타가 나면 red
 *
 * `[X]` **이 게이트는 「그 시험이 그 규칙을 잰다」를 판정하지 않는다.** 그것을 기계가 보려면
 *       시험마다 규칙 ID 를 적어야 하고, 그러면 값이 두 곳이 된다(ROOT AGENTS.md §6).
 *       여기서 막는 것은 **표가 조용히 낡는 것** 하나다 — 규칙이 늘거나 이름이 바뀌면
 *       사람이 다시 읽어야 한다는 신호를 낸다.
 *
 * `[!]` **DB 없이 돈다.** 두 파일 다 텍스트라 `scripts/test.sh` 의 「어디서나 같은 판정」을
 *       깨지 않는다.
 *
 *   node tools/verify-policy-coverage.js
 *   node tools/verify-policy-coverage.js selftest
 */
'use strict';

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..');
const POLICY = process.env.POLICY_DOC
  || path.resolve(ROOT, '..', 'docs', 'baseline', '00_Project_Policy.md');
const MAP = process.env.COVERAGE_DOC
  || path.resolve(ROOT, '..', 'docs', 'phase5', '2026-09-15-Policy-DB-Coverage.md');

/* `00` 이 쓰는 규칙 ID 는 여덟 접두 + 두 자리다. */
const RULE = /\b(CP|EP|RP|RCP|TGT|NEX|AEX|HOL)-\d{2}\b/g;

function idsOf(file) {
  const text = fs.readFileSync(file, 'utf8');
  const out = [];
  let m;
  RULE.lastIndex = 0;
  while ((m = RULE.exec(text)) !== null) {
    if (out.indexOf(m[0]) === -1) out.push(m[0]);
  }
  return out.sort();
}

function check() {
  for (const f of [POLICY, MAP]) {
    if (!fs.existsSync(f)) {
      console.log('FAIL POL-000 파일이 없다: ' + f);
      return 1;
    }
  }

  const policy = idsOf(POLICY);
  const mapped = idsOf(MAP);
  if (!policy.length) {
    console.log('FAIL POL-000 00 에서 규칙 ID 를 하나도 못 읽었다 — 표기가 바뀌었다');
    return 1;
  }

  let failed = 0;
  const missing = policy.filter(function (r) { return mapped.indexOf(r) === -1; });
  if (missing.length) {
    console.log('FAIL POL-001 대조표에 없는 규칙 ' + missing.length + '건 — ' + missing.join(' '));
    failed = 1;
  } else {
    console.log('PASS POL-001 00 의 규칙 ' + policy.length + '건이 전부 대조표에 있다');
  }

  const extra = mapped.filter(function (r) { return policy.indexOf(r) === -1; });
  if (extra.length) {
    console.log('FAIL POL-002 00 에 없는 규칙이 대조표에 있다 ' + extra.length + '건 — ' + extra.join(' '));
    failed = 1;
  } else {
    console.log('PASS POL-002 대조표가 00 밖의 이름을 쓰지 않는다');
  }

  return failed;
}

if (process.argv[2] === 'selftest') {
  const os = require('os');
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'pol-'));
  let rc = 0;

  const fire = function (label, want, policyText, mapText) {
    fs.writeFileSync(path.join(dir, 'p.md'), policyText, 'utf8');
    fs.writeFileSync(path.join(dir, 'm.md'), mapText, 'utf8');
    const child = require('child_process').spawnSync(
      process.execPath, [__filename],
      { env: Object.assign({}, process.env, {
          POLICY_DOC: path.join(dir, 'p.md'),
          COVERAGE_DOC: path.join(dir, 'm.md'),
        }) });
    const got = child.status;
    if (got === want) {
      console.log('PASS POL-SELFTEST ' + label);
    } else {
      console.log('FAIL POL-SELFTEST ' + label + ' (기대 exit ' + want + ', 실제 ' + got + ')');
      process.stdout.write(String(child.stdout));
      rc = 1;
    }
  };

  fire('같으면 통과한다', 0, 'CP-01 EP-02', 'CP-01 EP-02');
  fire('규칙이 늘었는데 표가 그대로면 잡는다', 1, 'CP-01 EP-02 HOL-06', 'CP-01 EP-02');
  fire('00 에 없는 이름이 표에 있으면 잡는다', 1, 'CP-01', 'CP-01 RP-99');
  fire('00 을 못 읽으면 통과가 아니라 FAIL 이다', 1, '규칙이 없다', 'CP-01');

  fs.rmSync(dir, { recursive: true, force: true });
  process.exit(rc);
}

const failed = check();
console.log('');
console.log(failed ? '=== 정책 대조표 FAIL ===' : '=== 정책 대조표 PASS ===');
process.exit(failed);
