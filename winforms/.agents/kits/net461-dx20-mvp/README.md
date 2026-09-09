# NET461_DX20_MVP v1.8 — 프로젝트 로컬 추가형 킷

기존 프로젝트 지침을 보존하면서 C# 7.3 / .NET Framework 4.6.1 / DevExpress 20.2 / VS2019 / WinForms MVP 개발 규칙을 추가합니다. 실제 프로젝트의 규칙과 기술 조건이 우선합니다.

## 복사와 연결

1. ZIP을 임시 폴더에 풉니다. 다음 세 폴더만 프로젝트의 같은 경로로 복사합니다. 기존 `.agents` 또는 `.claude` 폴더 전체를 삭제하거나 교체하지 않습니다.

```text
.agents/kits/net461-dx20-mvp/
.agents/skills/winforms-devexpress-ui/
.claude/skills/winforms-devexpress-ui/
```

2. 기존 `AGENTS.md`에 이 폴더의 `AGENTS.append.md` 내용을 추가합니다. 기존 본문은 보존합니다. `AGENTS.md`가 없으면 그 블록으로 새 파일을 만듭니다. 같은 위치에 `AGENTS.override.md`가 있으면 실제 로딩 진입점이므로, 해당 파일에 블록을 추가할지 프로젝트 관리자가 결정해야 합니다. override 파일을 새로 만들지는 않습니다.
3. 기존 `CLAUDE.md`에 `CLAUDE.append.md` 내용을 추가합니다. 없으면 그 블록으로 새 파일을 만듭니다. 기존 `CLAUDE.md`가 이미 `@AGENTS.md`로 연결돼 있어도 Claude용 직접 import는 문서 로딩 경로를 명확히 합니다. 같은 킷의 직접 import가 이미 있으면 중복 추가하지 않습니다.
4. 필요하면 `editorconfig.example`의 해당 항목을 기존 `.editorconfig`에 병합합니다. 프로젝트의 다른 설정을 덮어쓰지 않습니다.
5. 새 세션에서 로딩을 확인합니다. Codex에는 킷이 적용되는 코드 검토를 요청하고 실제 `PROJECT_INSTRUCTIONS.md` 읽기를 확인합니다. Claude는 `/memory`에서 연결 문서가 로드됐는지 확인합니다. 파일이 없거나 읽히지 않으면 적용 완료로 보지 않습니다.

루트 `AGENTS.md`, `CLAUDE.md`, `README.md`, `.editorconfig`와 설치·동기화 실행 스크립트는 이 배포 ZIP에 없습니다. 폴더 병합 복사만으로 기존 루트 파일을 덮어쓰지 않습니다. 세 전용 경로가 이미 있으면 아래 업데이트 절차를 따릅니다.

## 업데이트와 프로젝트별 예외

패키지 전용 세 폴더는 배포본 그대로 유지하고 프로젝트별 예외는 기존 프로젝트 지침에 작성합니다. 같은 경로에 다른 출처의 스킬이나 직접 수정한 내용이 있으면 먼저 비교·병합하고 소유권을 확인합니다.

업데이트 전 세 전용 폴더를 프로젝트 밖 임시 위치에 백업합니다. 세 폴더만 새 버전으로 교체해 이전 버전에서 삭제된 파일이 남지 않게 합니다. 기존 루트 지침의 연결 블록과 다른 킷·스킬은 유지합니다. 버전과 두 스킬의 내용 일치를 확인한 뒤 새 세션에서 다시 로딩을 검증합니다. 이 구조에서 프로젝트 루트 문서를 통째로 복사하는 설치 명령은 사용하지 않습니다.

## 로딩과 충돌

Codex의 연결 블록은 문서를 읽으라는 지시이며 자동 import 문법이 아닙니다. Claude의 `@경로`는 import입니다. 스킬이 목록에 보이는 것과 스킬 본문을 읽은 것은 구분합니다. 상세 UI 참고 문서는 해당 작업에서 필요할 때 읽습니다.

기존 프로젝트 지침이 킷보다 우선합니다. 프로젝트 Framework·DevExpress 버전이나 DBA SP 계약이 킷과 맞지 않으면 호환 불가 부분을 보고하고, 킷에 맞추려고 기존 기술 계약을 바꾸지 않습니다. 지침 문서는 안전·권한 통제를 대체하지 않습니다.

회사 명명 문서는 프로젝트가 제공하는 원본이 기준입니다. `contract/naming.md`와 다르면 원본을 따르고 차이를 보고합니다.

## 유지보수

배포본을 만드는 저장소의 `tools/sync-skill-mirror.ps1`은 정본 `.agents/skills/winforms-devexpress-ui`를 같은 이름의 Claude 스킬 폴더에만 반영합니다. 이 스크립트는 배포 ZIP에 포함하지 않습니다. 계약 본문과 스킬 버전, 문서 경로, 다른 파일 보존, 실제 로딩, 문서 코드 컴파일을 검사한 뒤 배포합니다.

공식 로딩 규칙: [Codex AGENTS.md](https://learn.chatgpt.com/docs/agent-configuration/agents-md), [Claude memory/imports](https://code.claude.com/docs/en/memory).
