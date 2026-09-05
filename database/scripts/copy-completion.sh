#!/usr/bin/env bash
# 접수완료(RCP) 업무를 완료이력으로 복사한다. 조회·시나리오 준비용이고 배포물이 아니다.
cd "$(dirname "$0")/.."
mkdir -p artifacts/logs
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -W \
       -i scripts/copy-completion.sql -o artifacts/logs/copy-completion.log
RC=$?
iconv -f UTF-16 -t UTF-8 artifacts/logs/copy-completion.log
exit $RC
