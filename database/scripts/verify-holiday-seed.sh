#!/usr/bin/env bash
# 06 §14.7 · 00 HOL-06 — 공휴일 Seed 만료와 사본 일치 판정.
# 판정 본체는 tools/verify-holiday-seed.js 다 (DB 없이 돈다).
cd "$(dirname "$0")/.."
node tools/verify-holiday-seed.js
