#!/usr/bin/env bash
set -uo pipefail          # -e 는 쓰지 않는다. 실패해도 로그를 반드시 출력해야 한다
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'
DB='HealthCheckupReservationReceptionDb'
mkdir -p artifacts/logs

RC=0
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -i Deploy.sql -o artifacts/logs/deploy_full.log || RC=$?
iconv -f UTF-16 -t UTF-8 artifacts/logs/deploy_full.log | tail -40
echo "deploy exit=$RC"
exit $RC
