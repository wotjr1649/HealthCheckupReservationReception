#!/usr/bin/env bash
# R4 C# 호출 사전 검수. tools/csharp-probe/Probe.cs 를 실제로 컴파일해 실행한다.
# WinForms 에 DB 호출 코드가 0건이라 "C# 이 한글 계약을 부를 수 있는가" 를 실행으로
# 확인한 적이 없었다 — 이 스크립트가 그 첫 증거다.
#
# set -e 를 쓰지 않는다 (CLAUDE.md §6). 실패한 줄에서 셸이 끝나면 RC 도 로그도 못 남긴다.
set -uo pipefail
cd "$(dirname "$0")/.."

CSC='/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
SRC='tools/csharp-probe/Probe.cs'
OUT='artifacts/logs/csharp-probe.exe'

if [ ! -x "$CSC" ]; then
  # 판정하지 못한 검사는 PASS 가 아니다 (CLAUDE.md §10).
  echo "NOT RUN csharp-probe — csc.exe 가 없다: $CSC"
  exit 3
fi

# BOM 이 없으면 csc 가 한글 리터럴을 시스템 코드페이지로 읽어 조용히 깨뜨린다 (05 §16.2).
if [ "$(head -c 3 "$SRC" | od -An -tx1 | tr -d ' ')" != "efbbbf" ]; then
  echo "FAIL csharp-probe — $SRC 에 UTF-8 BOM 이 없다"
  exit 1
fi

mkdir -p artifacts/logs
"$CSC" -nologo -warnaserror+ -optimize+ -platform:x64 \
       -out:"$(cygpath -w "$OUT")" "$(cygpath -w "$SRC")" || { echo "FAIL csharp-probe 컴파일 실패"; exit 1; }

"$OUT"; rc=$?
rm -f "$OUT"
exit $rc
