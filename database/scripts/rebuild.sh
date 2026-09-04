#!/usr/bin/env bash
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'
mkdir -p artifacts/logs
echo "!! 이 작업은 HealthCheckupReservationReceptionDb 를 삭제하고 다시 만듭니다."
sqlcmd -S "$SRV" -E -d master -b -u -i Rebuild.sql -o artifacts/logs/rebuild.log
iconv -f UTF-16 -t UTF-8 artifacts/logs/rebuild.log
./scripts/deploy.sh
