# Phase 4 R3 — 4단계: `00`·`01`·`02` 기준선 개편 후 중단

작업 디렉터리: `D:\AIDEV\HealthCheckupReservationReception\database`
작업 branch: **`phase4-r3`** (`56b77ea`). 이미 체크아웃돼 있다
선행 세션: `database-87` — 초안 `d0.3` 확정 + 미결 8건 실측 해소
복원점: branch `phase4-database`(`fdfb8e6`) · tag `phase4-r2-t07-complete`

---

## 1. 목표

**`docs/baseline/` 의 `00`·`01`·`02` 를 R3 로 교체하고 `scripts/verify-baseline.sh` 의 해시 3개를 같은 커밋에 묶어 바꾼 뒤 멈춘다.**

`03`·`04`·`05`·`06`·`plans/**`·실물 배포로 **넘어가지 않는다.** 그것들은 5·6·7단계다.

```text
이 세션의 산출물
  docs/baseline/00_Project_Policy.md        개정
  docs/baseline/01_Process_Definition.md    개정
  docs/baseline/02_Function_Definition.xlsx 개정
  database/scripts/verify-baseline.sh       SHA-256 3개 갱신
  → 한 커밋. 종료 시 게이트 3종 green
```

## 2. 설계는 이미 확정돼 있다 — 다시 설계하지 마라

`docs/phase4/04_DB_Design_R3_DRAFT.md` (d0.3, 2100여 줄)가 **개정문을 문장으로** 들고 있다. 그대로 옮겨 적는 일이다.

| 무엇 | 초안의 어디 |
|---|---|
| `00` CP-06 개정문 (현행 ↔ 개정 짝) | §3.1 |
| `00` 용어표·CP-05·RP-03·RP-10·RCP-06·TGT-02 | §3.2.1 · §3.2.2 |
| `00` §6 전이도 + Action Matrix (4행) | §3.2.3 |
| `01` 11곳 (현행 ↔ 개정 표) | §3.2.4 |
| `00`:70 · `00`:87 · `01`:189 · `01`:426 물리 식별자 | §3.3 |
| `02` **편집 36행 전수 목록** + 편집하지 않는 행 | §14.4 |
| 기준선 ID · 기준일 · 문서 버전 | §14.3 · §14.5 |

**먼저 읽을 것** (순서대로)

1. `database/CLAUDE.md` — 경계·실행 규칙. **§2 가 이번에 바뀌었다**(§4 참조)
2. `docs/phase4/04_DB_Design_R3_DRAFT.md` **§0 · §3 · §14** — 이 세션이 실행할 전부
3. `docs/baseline/00_Project_Policy.md` · `01_Process_Definition.md` — 개편 대상 원본

`docs/phase4/04_DB_Design_R3_DRAFT_REVIEW.md`(적대적 검토 51건)는 **읽지 않아도 된다.** 전건이 초안 §16 에 반영돼 있다.

## 3. 확정된 결정 — 재논의 대상이 아니다

| 결정 | 승인 |
|---|---|
| `00`·`01`·`02` 를 열고 R3 전체를 진행한다 | 2026-09-04 사용자 확정 |
| 상태 **4값** `RSV`·`RCP`·`CNR`·`CNC` — `FIN` 없음 | 2026-09-04 사용자 확정 |
| `완료이력` **존치** (테이블 7개). `00` TGT-04 는 열지 않는다 | 2026-09-04 사용자 확정 |
| `@OperatorName` 은 WinForms 설정 파일 → 화면 하단 상태영역 표시 | 2026-09-04 사용자 확정 |
| `02` 편집 수단 = **sheet XML in-place 셀 텍스트 치환** | 2026-09-04 사용자 확정 |
| 기준선 ID `HC-RSV-RCP-20260904-R3` · 기준일 `2026-09-04` | 2026-09-04 사용자 확정 |

## 4. `docs/baseline/` 쓰기 권한 — 이번에 열렸다

`database/CLAUDE.md` §2 가 이 커밋(`56b77ea` 다음)에서 갱신됐다.

```text
../winforms/          예외 없이 읽기 전용
../docs/baseline/     기본 읽기 전용
                      예외: R3 진행안 4·5·6단계에 한해, 그 단계가 지정한
                            기준선 파일과 verify-baseline.sh 해시를
                            같은 커밋에 묶어 바꾼다
4단계 지정 파일        00_Project_Policy.md · 01_Process_Definition.md
                      02_Function_Definition.xlsx  + 해시 3개
```

**목록 밖 파일에는 쓰지 않는다.** `03`·`04`·`05` 는 5·6단계 소관이다.

`[!]` 파일과 해시를 **한 커밋 안에서** 끝낸다. 중간에 멈추면 `verify-baseline.sh` 가 red 로 남으므로 되돌린다.

## 5. `02_Function_Definition.xlsx` — 실측된 함정 5종

이 파일은 일반 파서로 열리지 않는다. **선행 세션이 실제로 당한 것들이다.**

| 함정 | 대응 |
|---|---|
| 모든 요소에 **`x:` 네임스페이스 접두사** | XML 전체에서 접두사를 먼저 제거하고 파싱한다 — `s.replace(/<\/?[A-Za-z0-9]+:/g, m => m.replace(/[A-Za-z0-9]+:$/,''))` |
| `xl/sharedStrings.xml` 이 **비어 있다**(`si` 0개) | 셀 값은 `t="str"` + `<v>텍스트</v>` 로 **인라인**이다. `inlineStr`/`<is><t>` 가 아니다 |
| `workbook.xml.rels` 의 속성 순서가 `Type` → `Target` → **`Id`** | `Id="..."[^>]*Target="..."` 같은 순서 가정 정규식은 **매칭에 실패한다.** 속성을 각각 따로 뽑는다 |
| `Target="/xl/worksheets/sheet1.xml"` — **선두 슬래시** | `.replace(/^\//,'')` 로 떼야 `zip.file()` 이 찾는다 |
| `jszip` 이 프로젝트 `node_modules` 에 없다 | 전역 설치돼 있다. `NODE_PATH="$(npm root -g)" node <스크립트>` |

**시트 ↔ 파일 대응** (`workbook.xml` 순서 = `sheet1..7.xml` 순서)

```text
1 문서정보  2 기능정의  3 업무Rule  4 개발범위  5 설계근거  6 DB추적  7 최종검수
```

**보존해야 하는 것** — `xl/tables/table1~6.xml` 의 `ref`

```text
table1 A3:I39  table2 A3:J39  table3 A3:H32
table4 A3:F25  table5 A3:G19  table6 A3:E32
```

편집이 **전부 셀 텍스트 치환**이라 행 삽입·삭제가 없고 이 `ref` 6개를 손댈 일이 없다. **치환 후 6개가 그대로인지 확인하는 것이 회귀 검사다.**

## 6. 검증 — 이 순서로 한다

```text
02 치환 직후
  1. 다시 파싱해 셀 단위 대조 — 초안 §14.4 의 36행만 바뀌었는가
  2. xl/tables/table1~6.xml 의 ref 6개가 치환 전과 동일한가
  3. Excel 이 파일을 열 수 있는가            ← 미검증. §8 참조

전체
  4. node tools/verify-docs.js               → PASS 15 / FAIL 0
  5. ./scripts/verify-baseline.sh            → 6/6   (해시 3개 갱신 후)
  6. ./scripts/verify-winforms-unchanged.sh  → exit 0
```

해시 얻는 법 — `verify-baseline.sh` 를 갱신 **전에** 한 번 돌리면 `DIFF` 줄에 실측 SHA-256 이 찍힌다. 그 값을 스크립트의 `exp` 객체에 붙여 넣는다.

## 7. 완료 판정 — 전부 참이어야 이 세션이 끝난다

```text
 1. 00 이 초안 §3.1·§3.2.1·§3.2.2·§3.2.3·§3.3 대로 개정됐다
 2. 01 이 초안 §3.2.4·§3.3 대로 개정됐다 (CNL 11곳 전건)
 3. 02 가 초안 §14.4 의 36행대로 개정됐고 표 ref 6개가 그대로다
 4. 세 파일의 기준선 ID·기준일·문서 버전이 §14.3·§14.5 대로다
 5. grep -c 'CNL' 이 00 에서 0, 01 에서 0 이다
 6. 02 에 CNL·INFO_·MST_·HIS_ 토큰이 0회다
 7. verify-baseline.sh 가 6/6 이다
 8. node tools/verify-docs.js 가 PASS 15 / FAIL 0 이다
 9. verify-winforms-unchanged.sh 가 exit 0 이다
10. 03·04·05·06·plans/** 이 한 줄도 바뀌지 않았다
11. git status 에 ROOT tools/ 와 docs/baseline/output/ 이 없다
12. branch 가 phase4-r3 이고 phase4-database 는 fdfb8e6 그대로다
```

`[!]` **5·6번이 이 단계의 진짜 게이트다.** `verify-baseline.sh` 는 해시만 보므로 개정이 **틀려도** 통과한다.

## 8. 검증되지 않은 것 — 마주치면 이렇게 하라

| 항목 | 상태 | 대응 |
|---|---|---|
| **치환한 xlsx 를 Excel 이 여는가** | **미검증.** 선행 세션은 읽기만 했고 쓴 적이 없다 | 치환 전 원본을 복사해 두고, 치환 후 열리지 않으면 되돌린 뒤 `[검증 1·2]` 로 원인을 좁힌다. zip 재패키징 시 **압축 방식과 엔트리 순서**를 원본과 맞춘다 |
| `build_02.js` 가 개정된 원본으로 도는가 | **미검증.** 이 세션 범위 밖 | ROOT `tools/` 는 **다른 세션(`a97cfb9f`, 일시 중지)의 산출물**이다. 실행·수정·커밋하지 않는다. 그 세션이 나중에 재생성한다 |
| `00` §6 전이도 코드펜스 | 코드펜스 안이라 `verify-docs` `V01` 이 짝을 센다 | 펜스를 깨지 않는다. 개정 후 `V01` 확인 |
| `01`:649 와 `01`:899 | **문자열이 완전히 같다** (`S8[동일 업무 행을 CNL로 변경]`) | 649 는 예약취소 흐름 → `CNR`, 899 는 접수취소 흐름 → `CNC`. **일괄 치환하면 둘 다 같은 값이 된다** |
| `02` 시트7 `r11` `RCP-01~RCP-06` | `CP-06` 정규식의 **오탐** | 손대지 않는다. 초안 §14.4 말미 참조 |

## 9. 절대 금지

```text
docs/baseline/03 · 04 · 05 수정            ← 5·6단계 소관
docs/phase4/06_*.md 와 plans/** 수정        ← 6단계 소관
docs/phase4/04_DB_Design_R3_DRAFT.md 재설계 ← 확정본이다. 오류를 찾으면 고치되 설계를 바꾸지 마라
database/deploy/** · database/tests/** 수정 ← 7단계 소관
../winforms/** 수정
ROOT tools/ 와 docs/baseline/output/ 실행·수정·커밋
phase4-database branch 로 되돌아가 커밋하는 것
git config --global 변경 / remote 추가 / push
git reset · clean · checkout -- · force push · 기존 tag 이동
SQL Server 접속·배포 (7단계 소관. 현재 R2 7테이블이 올라가 있다)
실제 주민등록번호 사용
```

## 10. `CLAUDE.md` 에 없는 실행 함정

`CLAUDE.md` 의 BOM · `-b -I -u` · `N` 접두사 · `set -e` 회피를 **먼저 읽는다.** 아래는 거기 없다.

| 함정 | 실측 |
|---|---|
| `sqlcmd` 출력을 셸 리다이렉트로 받으면 **CP949** 다 | `-u` 는 `-o` 에만 적용된다. `iconv -f CP949 -t UTF-8` 로 읽는다. `UTF-16` 으로 열면 깨진다 |
| `sqlcmd -S` 의 인스턴스명 | `-S ".\SQLEXPRESS"` 처럼 **따옴표로 감싼다.** 안 그러면 백슬래시가 먹혀 `[53]` 연결 실패 |
| `EXCEPT` 뒤의 `UNION ALL` | 좌→우 결합이라 `A EXCEPT B UNION ALL C` 가 `(A EXCEPT B) UNION ALL C` 다. **괄호를 친다** |
| `rm -rf` | 훅이 막는다. 임시 디렉터리라도 **리터럴 절대경로**만 통과한다. 변수·따옴표는 deny |
| `bash -c` · `powershell` 중첩 호출 | 훅이 막는다. 스크립트는 `bash <파일>` 로 |

## 11. 끝나면

§7 판정 12개를 표로 보고하고 **멈춘다.** 5단계(`03` 개편)로 넘어갈지는 사용자가 정한다.
전체 진행안은 `docs/phase4/04_DB_Design_R3_DRAFT_REVIEW.md` §9.2 에 있다.
