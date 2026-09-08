# Phase 5 인계 — `07` UI–DB Matrix 를 CANDIDATE 로 쓴다

작업 디렉터리는 `D:/AIDEV/HealthCheckupReservationReception` 이다.
**세션을 저장소 루트에서 연다.** 루트 `AGENTS.md` 가 봉인·입주·산출물 규칙을 담고 있고,
`database/` 에서 일할 때는 `database/AGENTS.md` 가 추가로 붙는다. 이 문서는 그 위에 얹는 것이지
대체하지 않는다.

---

## 1. 지금 참인 것

```text
브랜치        phase4-database
작업트리      clean
봉인          docs/baseline/ 에 00~06 일곱.  07 은 없다 — 이번에 만든다
```

Phase 4 DB 는 **끝났고 봉인됐다.** 어느 문서가 어느 회차인지는 `06` §4.1 이 단일 출처다 —
여기에 회차 이름을 적지 않는다. 실제로 R5 라 적었다가 R6 하나에 낡았다.

**회차별 실측과 Gate 판정은 `06` §42 가 단일 출처다 — 여기에 베끼지 않는다.**
예전에 베껴 두었는데 R5 재봉인에 전부 낡았다: 창 안 회차는 `357`→`363`, `verify-docs` 는
`22/0`→`24/0` 이 되었고 그 사이 판정 코드가 두 번 바뀌어 `357` 은 **고치기 전 코드의 증거**였다
(ROOT `AGENTS.md` §6).

지금 상태는 읽지 말고 **돌려서 본다.**

```bash
cd database
./scripts/test.sh              # exit 0 · FAIL 0. 창 안이면 SKIP 은 OFF-309 둘뿐이다
node tools/verify-docs.js      # FAIL 0
./scripts/verify-baseline.sh   # 봉인 전건 일치
```

돌아가는 게이트: `verify-baseline` · `verify-docs`(V01~V23) · `verify-rs-contract` ·
`verify-schema-doc` · `verify-winforms-unchanged` · `verify-tsql-allowlist` ·
`allowlist-review`(G13-c) · `verify-no-secret` · `verify-red` · `csharp-probe` · `r4-rename check`.
`verify_output` 은 산출물 쪽이라 `test.sh` 밖이며 `tools/docgen/build_all.js` 가 부른다.

DB 가 확정한 것 — **이것이 `07` 의 오른쪽 절반이다.**

```text
Table 6 · Inline TVF 4 · Stored Procedure 16 · Sequence 1
Parameter 99 · Result Set 컬럼 고유 73종 · ResultCode 38종
계약 시나리오 110종 · Test ID 카탈로그 247
```

`docs/baseline/06` 이 Phase 4 의 구현 계약이자 실행검증 기록이고,
`database/artifacts/reports/phase4-report.md` §7 이 **Phase 5 인계 13항**을 이미 담고 있다.
그 13항은 이 문서가 옮겨 적지 않는다 — 읽어라.

## 2. 이번에 할 일 — `07` CANDIDATE 하나

`docs/phase5/07_UI_DB_Matrix_Final_Validation.md` 를 **CANDIDATE 로** 쓴다.
구현은 하지 않는다. 개발 킷이 아직 완성되지 않았다(§4).

`06` 이 정확히 이 경로를 밟았다.

```text
06  v0.4 CANDIDATE (구현 전에 쓴 스펙)
      -> 구현 -> 회귀 -> §42 에 실측 채움
    v1.0 FINAL / GO
    v1.1 R4 반영 -> 2026-09-08 docs/baseline/ 입주
    v1.2 R5 — 입주 뒤 §42 의 G00·G13·G16 이 실측과 어긋나 있던 것을 고쳤다
    v1.3 R6 — R5 가 §42 에 남긴 "이후 안 바뀌었다" 는 미래 단정을 걷었다
```

`[!]` **v1.1 -> v1.2 가 이 경로의 진짜 교훈이다.** `FINAL / GO` 로 봉인한 뒤에도 §42 는 세 칸이
거짓이었다 — 봉인 안이라 아무도 다시 안 봤기 때문이다. `07` 도 같은 위험을 진다.
숫자를 적을 때마다 **그 값이 게이트와 일치하는지 보는 검사를 함께 만든다.** 못 만들면 적지 않는다.

`07` 도 같다. **쓰는 동안 `docs/phase5/` 에 있고, FINAL 이 되면 봉인 입주한다**(루트 `AGENTS.md` §2.1).

### 담는 것 셋

```text
1  아키텍처      계층 · 의존 방향 · 에러 전파
                 킷이 정한 MVP Passive View 를 이 프로젝트에 앉히는 방법
2  전건 대응표    화면 13 <-> SP 16 <-> ResultCode 38  (§3 이 이것의 승인을 요구한다)
3  대표화면 1개   WF-PAT-01 수검자 관리 — 나머지 12개의 본보기
```

`docs/phase5/plans/` 에 단계 분할과 게이트를 적는다. `docs/phase4/plans/**` 가 그 본보기다.

### 담지 않는 것

```text
나머지 12개 화면의 컨트롤 배치 상세     구현 전에 굳으면 되돌리는 비용이 크다
Repository/Service 의 실제 메서드 시그니처   킷이 아직 움직인다 (§4)
DevExpress 컨트롤 구체 설정
```

## 3. 착수 전 첫 산출물 — 대응표. **승인 없이 본문을 쓰지 마라**

화면 13 ↔ SP 16 ↔ ResultCode 대응표를 **먼저 표로 내고 사용자 승인을 받는다.**

R4 에서 이 절차가 실제로 값을 했다. 매핑표 130여 건을 먼저 냈고, 그 자리에서 네 갈래가 갈렸으며
그중 하나는 작성자의 권장과 **반대로** 정해졌다. 승인 없이 본문을 썼으면 다시 써야 했다.

표의 출처는 이 넷이고, 전부 기계로 뽑을 수 있다.

```text
화면 13     docs/baseline/03_Wireframe_Definition.md
             WF-PAT-01 · WF-RSV-01 · WF-WRK-01
             DLG-PAT-01 · DLG-PAT-02 · DLG-PAT-03 · DLG-RSV-01
             DLG-RCP-01 · DLG-RCP-02 · DLG-LOG-01
             CNF-RSV-01 · CNF-RCP-01  (+ MainForm)
SP 16       docs/baseline/05_DB_Rule_SP_Contract.md §1.3
ResultCode  05 §4.2 (38종) · SP별 허용집합은 05 §13
허용집합    database/tools/allowed-codes.json  — 05 §13 을 기계가 읽는 형태로 옮긴 것
```

## 4. 개발 킷 — 그 위에 서라. 다만 시그니처를 박지 마라

```text
D:/AIDEV/winformdev_env/NET461_DX20_MVP/
  AGENTS.md                        계약 본문 (13,674 bytes)
  .agents/contract/build.md        파일 편집·빌드·테스트 전
  .agents/contract/naming.md       식별자를 만들거나 바꾸기 전
  .agents/contract/repository.md   리포지토리를 쓰기 전
  .agents/contract/service.md      서비스·DTO·요청·결과 타입을 쓰기 전
  .agents/contract/report.md       완료 보고서를 쓰기 전
  .agents/skills/winforms-devexpress-ui/
D:/AIDEV/winformdev_env/docs/개발 네이밍 및 코딩 규칙.md
```

킷이 정한 스택은 **C# 7.3 / .NET Framework 4.6.1 / DevExpress 20.2 / VS2019 / WinForms MVP
Passive View** 이고, 킷 README 가 *"저장 프로시저는 DBA가 제공합니다"* 라고 적은 그 SP 가
바로 Phase 4 의 16개다.

`[!]` **킷은 아직 완성되지 않았다.** 그래서 스펙은 **계약에만 기댄다.**

```text
지금 박아도 되는 것   계층 구조 · 의존 방향 · 명명규칙 · 에러 전파
                      화면 <-> SP <-> ResultCode 대응
                      RS0 5컬럼 읽기 · 동시성 4가지 분기
지금 박지 않는 것     Repository/Service 의 실제 메서드 시그니처
                      DevExpress 컨트롤 구체 설정
```

`[!]` **킷을 아직 설치하지 마라.** 킷의 `CLAUDE.md` 는 `@AGENTS.md` 한 줄이라 **이 저장소의
루트 `CLAUDE.md` 를 덮어쓴다.** 설치는 킷이 완성된 뒤 두 `AGENTS.md` 를 합치는 작업이며,
그때 예산을 넘는다(§7).

## 5. 이름이 두 계층이다 — 단일 출처는 `05` §16.5

R4 로 DB 계약이 한글이 되었고 화면·C# 계층은 영문을 유지한다(`05` §16.2).

```text
00 · 01 · 03   화면·C# 계층   PatientId · WorkId · RowVersion · LastEditDate …
04 · 05 · 06   DB 계약        수검자ID · 업무ID · 행버전 · 최종수정일시 …
```

대응 **26종의 단일 출처는 `05` §16.5** 다. `07` 에 같은 표를 다시 만들지 마라 —
`verify-docs` 의 `V20` 이 그 표를 양방향으로 지키고 있고, 사본은 그 보호를 받지 못한다.

C# 쪽 규칙은 `05` §16.2 에 있다. 요약하면 이렇다.

```text
식별자      영문 PascalCase 유지 (DbCode Enum · DTO 프로퍼티)
호출        cmd.Parameters.Add("@수검자ID", SqlDbType.BigInt)
읽기        reader["성공여부"]  — 이름으로 읽는다. 순서에 기대지 않는다
CommandText dbo.USP_HC_수검자_등록   한글 SP 이름을 그대로
소스 파일   UTF-8 with BOM. 없으면 컴파일러가 한글 리터럴을 깨뜨린다
```

## 6. 절대 어기면 안 되는 것

**`04`·`05`·`06` 을 열지 마라.** 봉인돼 있다. 여는 조건은 셋뿐이다.

```text
07 을 쓰다가 SP 계약의 결함을 찾았을 때
화면이 요구하는 것을 SP 가 못 준다는 것이 밝혀졌을 때
07 이 FINAL 이 되어 baseline 에 입주시킬 때
```

그 셋 중 하나면 **작업을 멈추고 사용자에게 보고한다.** 재봉인은 database 계열의 절차이고
파일과 해시를 같은 커밋에 묶어야 한다(루트 `AGENTS.md` §2.2).

그 밖에 지킬 것.

```text
실행하지 않은 검증을 PASS 로 적지 않는다        database/AGENTS.md §10
같은 값을 두 곳에 두지 않는다                  루트 AGENTS.md §6
.sql 을 만들면 UTF-8 with BOM                 database/AGENTS.md §5
sqlcmd 는 항상 -b -I -u                       database/AGENTS.md §6
커밋 전에 git status --short 로 삭제·이름변경을 먼저 본다
```

## 7. 이미 실측했다 — 다시 하지 마라

```text
DevExpress 20.2   GAC 에 설치돼 있다. 단 winforms 의 csproj 는 아직 참조하지 않는다
MSBuild 4.0       /c/Windows/Microsoft.NET/Framework64/v4.0.30319/MSBuild.exe. dotnet CLI 는 없다
csc.exe           같은 경로. C# 호출 탐침이 이것으로 컴파일된다
winforms/         98줄 골격뿐 — Program.cs · 빈 MainForm · 스모크 테스트 1
winforms/scripts/ 비어 있다. 그쪽엔 게이트가 하나도 없다
C# 호출           이미 검증했다. csharp-probe CS-001~016 전건 PASS
                  한글 SP 이름 · 한글 SqlParameter · 한글 컬럼 읽기 · 행버전 byte[8] ·
                  Parameters 추가 순서를 뒤섞어도 이름으로 바인딩됨
```

`[!]` **Codex 지침 예산.** 넘으면 경고 없이 잘린다. **한도를 여기 적지 않는다** —
`~/.codex/config.toml` 의 `project_doc_max_bytes` 가 소유하고, 기계 설정이라 언제든 바뀐다.
예전에 `32,768` 이라 적어 두었는데 실측은 그 두 배였고 "킷을 설치하면 초과한다" 는 결론이
거짓이었다 (ROOT `AGENTS.md` §6).

```bash
grep project_doc_max_bytes ~/.codex/config.toml
cat ~/.codex/AGENTS.md AGENTS.md database/AGENTS.md | wc -c   # 저장소 루트에서
```

킷 `AGENTS.md`(`NET461_DX20_MVP/`)를 더하면 합계가 크게 는다 — 설치 시점에 합치면서 줄인다.

## 8. 검증 방법 — 게이트를 회귀 안에 넣는다

`docs/phase5/` 는 **지금 어떤 게이트도 보지 않는다.** `verify-docs.js` 의 `PLANDIR` 은
`docs/phase4/plans/` 이고 `SPEC` 은 `docs/baseline/06` 이다.

`07` 과 Phase 5 plans 를 판정하는 게이트를 **함께 만들고 `database/scripts/test.sh` 안에 넣는다.**

```text
대응표의 SP 16개가 05 §1.3 과 일치하는가
ResultCode 가 05 §13 의 SP별 허용집합 안인가
화면 ID 가 03 에 실재하는가
07 의 절 참조가 04·05·06 에 실재하는가
```

`[X]` **회귀 밖의 게이트는 썩는다.** 이 저장소에서 `G13-b`·`G00`·`G01`·`G05` 가 전부 그렇게
한 번 썩었고, 이번 세션에도 새로 만든 게이트 둘을 손으로만 돌리다 회귀에 넣어 고쳤다.

만들었으면 **돌려서 확인한다.**

```bash
./scripts/test.sh          # database/ 에서. SQL Server 를 쓴다
```

시간 의존 경로가 있어 회귀는 시각에 따라 판정 대상이 달라진다.

```text
CON 창   월~토 11:00~15:50   CON-001~008 · Write SP 성공 경로
                             PM 시간대 접수마감 16:00 - 10분 여유. AM 마감(11:00) 뒤라야 PM 이 산다
계약 창  월~토 11:10~15:50   위에 CWR-009 가 더 붙어 계약 전건이 판정되는 구간
                             CWR-009 는 AM 마감(11:00)이 **지나야** 성립해 10분 여유를 더한다
AM 창    09:00~11:00         접수마감(AM 11:00) 전 경로
창 밖                        OFF-308 · OFF-309
```

`[!]` **두 창은 다른 것이다.** `11:10` 은 `11:00` 보다 좁고, 예전에 이 표가 둘을 뭉쳐
`CON-001~008` 에 `11:10` 을 붙여 두었다. 값의 소유자는 문서가 아니라 스크립트다 —
`concurrency-test.sh`(CON) 와 `verify-contract-all.sh`·`tests/07`(CUTAM·CUTPM) 이 갖고 있고
근거는 `06` §38.7 이다. 운영시간 `09:00~18:00` 과 마감 4종은 `deploy/03_Functions.sql` 이 확정한다.

시각을 옮기는 것은 **사람이 한다.** 되돌리지 못한 채 죽는 자동화를 만들지 마라.

## 9. 되돌림

```bash
git tag phase5-spec-start        # 착수 전에 박는다
git reset --hard phase5-spec-start
```

기준선이 걱정되면 **가장 최근 `baseline-*` 태그**가 지금 지점이다 — 이름을 여기 적지 않는다.

```bash
git tag -l "baseline-*" | tail -1
```

## 10. 완료 조건

```text
[ ] 대응표 사용자 승인 (§3)
[ ] 07 CANDIDATE — 아키텍처 · 전건 대응표 · 대표화면 WF-PAT-01
[ ] docs/phase5/plans/ 단계 분할과 게이트
[ ] Phase 5 게이트 신설 + database/scripts/test.sh 에 편입
[ ] ./scripts/test.sh exit 0 · FAIL 0  (실행해서 확인. 안 돌린 것을 PASS 로 적지 않는다)
[ ] verify-baseline 7/7 — 04·05·06 을 열지 않았음을 이것이 증명한다
```

`07` 은 이 시점에 **CANDIDATE 다.** FINAL 도 봉인 입주도 Phase 5 구현이 끝난 뒤다.

## 11. 확인하지 않은 것

```text
개발 킷의 완성 시점         "완료되면" 이라고만 들었다
킷 설치 절차의 실측          README 의 cp 명령을 실행해 본 적이 없다
Phase 5 의 시험 전략         킷의 .agents/contract/build.md 가 정할 것으로 보이나 읽지 않았다
winforms 빌드               MSBuild 로 현재 골격을 빌드해 본 적이 없다
verify-winforms-unchanged   Phase 5 구현이 시작되면 이 게이트가 red 가 된다.
                            init 로 다시 봉인할 수 있으나, 매 커밋마다 다시 봉인하는 게이트는
                            아무것도 지키지 않는다. 구현 착수 시점에 처분을 정해야 한다
```
