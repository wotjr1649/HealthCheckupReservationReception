#!/usr/bin/env bash
# inspect.sql 을 돌리고 UTF-8 로 보여준다. 읽기 전용이라 rebuild 하지 않는다.
cd "$(dirname "$0")/.."
mkdir -p artifacts/logs
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -W -w 200 \
       -i scripts/inspect.sql -o artifacts/logs/inspect.log
RC=$?
iconv -f UTF-16 -t UTF-8 artifacts/logs/inspect.log
exit $RC
