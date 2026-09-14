/*
 * paths.js — docgen 이 쓰는 경로 한자리.
 *
 * [X] **예전에는 절대경로가 열일곱 곳에 박혀 있었다** (2026-09-14 실측). 다른 머신이나
 *     다른 클론에서는 생성기도 게이트도 통째로 돌지 않는다 — 산출물을 판정하는
 *     `verify_output.js` 조차 첫 줄의 require 에서 죽는다. 그러면 「공개본이 무엇을
 *     말하는가」를 지키는 것이 그 환경에는 아예 없다.
 *
 * 저장소 경로는 이 파일 위치에서 나온다. `node_modules` 만 저장소 밖에 있으므로
 * 환경변수로 가리킬 수 있게 둔다 — 이름은 `build_02.js` 가 이미 쓰던 `HCDOC_MODULES` 를
 * 그대로 쓴다. 새로 지으면 이름이 둘이 되고, 둘이 되면 한쪽만 맞춰지는 날이 온다
 * (ROOT AGENTS.md §6).
 */
'use strict';
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..');
const OUT = path.join(ROOT, 'docs', 'baseline', 'output');
const MODULES = process.env.HCDOC_MODULES || 'D:/tmp/hcwork/gen/node_modules';

/** 저장소 밖 node_modules 를 먼저 보고, 없으면 보통의 해석에 맡긴다. */
function mod(name) {
  try { return require(path.join(MODULES, name)); }
  catch (e) { return require(name); }
}

module.exports = { ROOT, OUT, MODULES, mod };
