/*
 * build_all.js — 산출물 여섯 종을 만들고 **반드시** 공개 적합성 게이트를 지난다.
 *
 * 생성기를 하나씩 손으로 돌리면 마지막 검사를 빼먹는다. 이 저장소에서 실제로 그렇게
 * 한 건이 오래 떠 있었다(build_00.js 의 금지 문자열 1건). 진입점을 하나로 둔다.
 *
 *   node tools/docgen/build_all.js
 *
 * 03 화면설계서는 pptxgenjs 를 bare require 하므로 NODE_PATH 가 필요하다 — 여기서 직접 넣는다.
 */
'use strict';
const { execFileSync } = require('child_process');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..');
const MODULES = 'D:/tmp/hcwork/gen/node_modules';

const WF_SCREENS = [
  'a0_list', 'a1_flow', 'wf_00', 'wf_pat_01', 'dlg_pat_02', 'dlg_pat_01', 'dlg_pat_03',
  'wf_rsv_01', 'wf_wrk_01', 'dlg_rsv_01', 'cnf_rsv_01', 'dlg_rcp_01', 'dlg_rcp_02',
  'cnf_rcp_01', 'dlg_log_01', 'dlg_hol_01', 'z_a_rules', 'z_b_search', 'z_c_validation', 'z_d_ribbon',
  'z_e_fields', 'z_f_impl',
].join(',');

const STEPS = [
  ['00 업무정책',   ['tools/docgen/xlsx/build_00.js']],
  ['01 업무프로세스', ['tools/docgen/proc/build.js']],
  ['02 기능정의',   ['tools/docgen/xlsx/build_02.js']],
  ['03 화면설계서',  ['tools/docgen/wireframe/build.js', 'docs/baseline/output',
                    WF_SCREENS, '03_검진_예약접수_화면설계서.pptx']],
  ['04 DB설계서',   ['tools/docgen/xlsx/build_04.js']],
  ['05 SP계약서',   ['tools/docgen/xlsx/build_05.js']],
];

let failed = 0;
for (const [label, args] of STEPS) {
  process.stdout.write(label.padEnd(16));
  try {
    execFileSync(process.execPath, args, {
      cwd: ROOT, stdio: ['ignore', 'pipe', 'pipe'],
      env: Object.assign({}, process.env, { NODE_PATH: MODULES }),
    });
    console.log('생성');
  } catch (e) {
    failed++;
    console.log('실패');
    process.stdout.write(String(e.stdout || '') + String(e.stderr || ''));
  }
}

if (failed) {
  console.log('\n=== build_all: 생성 실패 ' + failed + '건 — 게이트를 돌리지 않는다 ===');
  process.exit(1);
}

console.log('');
try {
  const out = execFileSync(process.execPath, ['tools/docgen/verify_output.js'],
                           { cwd: ROOT, encoding: 'utf8' });
  process.stdout.write(out);
} catch (e) {
  process.stdout.write(String(e.stdout || '') + String(e.stderr || ''));
  process.exit(1);
}
