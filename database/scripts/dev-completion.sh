#!/usr/bin/env bash
# 개발 전용 완료이력 SP 를 설치/제거한다. 배포물이 아니다 (scripts/dev-completion-sp.sql 머리 참조).
#   ./scripts/dev-completion.sh          설치
#   ./scripts/dev-completion.sh drop     제거
# scripts/test.sh 는 언제나 rebuild.sh(DROP DATABASE)로 시작하므로 회귀에 섞이지 않는다.
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'; DB='HealthCheckupReservationReceptionDb'
if [ "${1:-install}" = "drop" ]; then
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -Q "DROP PROCEDURE IF EXISTS [dbo].[DEV_완료이력_등록];" \
    && echo "DEV_완료이력_등록 제거"
  exit $?
fi
sqlcmd -S "$SRV" -E -d "$DB" -b -I -i scripts/dev-completion-sp.sql
RC=$?
# 계약 개수가 움직이지 않았는지 그 자리에서 확인한다 — 이 SP 의 존재 이유가 그것이다.
N=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q \
    "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.procedures WHERE name LIKE 'USP[_]HC[_]%';" | tr -d ' \r' | head -1)
if [ "${N:-0}" = "16" ]; then echo "PASS 계약 SP 는 여전히 16개 (DEV_ 접두사는 게이트 밖)"
else echo "FAIL 계약 SP 가 $N 개다 — DEV_ SP 가 계약을 건드렸다"; RC=1; fi
exit $RC
