## WinForms development kit

Before modifying, reviewing, building, or testing code in this repository,
read `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` and apply its
technical requirements alongside this project's guidance.

Existing applicable project instructions take precedence over the kit.
Report incompatible framework, dependency, or stored-procedure contracts
before changing affected code; do not change them to fit the kit.
If the file cannot be read, report the missing guidance and do not claim
that the kit was applied. Never replace the existing project instructions.

## winforms/ — Phase 5 작업 경계

저장소 전체 규칙은 루트 `AGENTS.md` 다. 문서 사슬 `00`~`07`, 봉인, 산출물, 이름 두 계층의
단일 출처가 전부 거기 있다 — **여기에 베끼지 않는다**(루트 §6). 이 절은 이 디렉터리에서만
참인 것만 적는다.

### 쓰기·읽기 경계

```text
쓴다      winforms/**  ·  ../docs/phase5/
읽는다    ../docs/baseline/  ·  ../database/**
```

`../docs/baseline/` 을 여는 것은 루트 `AGENTS.md` §2 의 재봉인이며 database 계열의 절차다.
계약이 틀렸다고 판단되면 고치지 말고 **멈추고 사용자에게 보고한다.**

### 킷보다 `05` 가 이긴다

**SP·DB 계약의 단일 출처는 `../docs/baseline/05_DB_Rule_SP_Contract.md` 이며, 킷의 SP 관련
규칙보다 `05` 가 이긴다.** 킷 본문과 `contract/repository.md` 에 이 저장소와 어긋나는 대목이
실제로 있다 — 무엇이 왜 어긋나는지는 착수 인계문서가 `05` 절번과 함께 적는다.
킷 자신이 그렇게 하라고 적어 두었다(`PROJECT_INSTRUCTIONS.md` 머리말).

### winforms 를 바꾸면 manifest 도 같은 커밋에서 갱신한다

`../database/scripts/verify-winforms-unchanged.sh` 가 "database 계열이 winforms 를 건드리지
않았다" 를 판정한다. Phase 5 가 정당하게 바꾼 것까지 red 가 되므로, 바꾼 커밋에서
`cd ../database && ./scripts/verify-winforms-unchanged.sh init` 으로 manifest 를 갱신해
같은 커밋에 넣는다. 루트 §2.2 재봉인과 같은 방식이다.

### 연결문자열은 통합인증만

`user id=`·`uid=`·`integrated security=false` 는 이 저장소의 금지값이다.

### 착수 전에 읽는다

`../docs/phase5/2026-09-09-session-11-Phase5-Start.md`
