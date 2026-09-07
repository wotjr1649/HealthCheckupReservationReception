# docgen — 사내 공개용 산출물 생성기

`docs/baseline/` 의 확정 문서를 읽어 `docs/baseline/output/` 에 공개용 PPT / Excel을 만든다.
**`docs/baseline/` 의 원본 6종은 읽기 전용이다. 어떤 스크립트도 원본을 쓰지 않는다.**

## 산출물

| 산출물 | 원본 | 생성 |
|---|---|---|
| `00_검진_예약접수_업무정책.xlsx` | `00_Project_Policy.md` | `node tools/docgen/xlsx/build_00.js` |
| `01_검진_예약접수_업무프로세스.pptx` | `01_Process_Definition.md` | `node tools/docgen/proc/build.js` |
| `02_검진_예약접수_기능정의.xlsx` | `02_Function_Definition.xlsx` | `node tools/docgen/xlsx/build_02.js` |
| `03_검진_예약접수_화면설계서.pptx` | `03_Wireframe_Definition.md` | 아래 참조 |
| `04_검진_예약접수_DB설계서.xlsx` | `04_DB_Design.md` | `node tools/docgen/xlsx/build_04.js` |
| `05_검진_예약접수_SP계약서.xlsx` | `05_DB_Rule_SP_Contract.md` | `node tools/docgen/xlsx/build_05.js` |

`04` 는 시트 7개다 — **논리 ERD · 물리 ERD** 가 앞의 둘이고 표 다섯이 뒤따른다.
`exceljs 4.4` 는 도형을 못 그리고 이 환경에는 래스터라이저(sharp·canvas·resvg)가 없어
한글이 든 PNG 를 만들 수 없다. 그래서 **셀로 그린다** — 테두리·채움·병합만 쓰므로 이미지와
달리 선택·검색·인쇄가 되고 파일이 커지지 않는다. 그리기 킷은 `xlsx/md.js` 아래쪽에 있다.

```text
xlsx/erd_04.js       논리 ERD · 물리 ERD 시트. 내용은 기준선에서, 좌표만 손으로
xlsx/inspect_erd.js  만들어진 ERD 시트를 텍스트로 되그린다 — 눈으로 열지 않고 배치를 판정한다
```

`[!]` **논리 ERD 는 기준선 `04` §4.5 의 관계선 2개를 그대로 그린다.** 점선으로 그린 검사구성
문자열 참조는 §4.5 **산문**이 "FK 관계선이 없다" 고 적은 것을 눈에 보이게 옮긴 것이며 관계가 아니다.
**물리 ERD 는 기준선에 없다** — §7 테이블 요약과 §8 컬럼·키에서 기계적으로 유도한 것이다.

`[X]` **열 너비에 정확히 `9` 를 쓰지 않는다.** `exceljs` 가 자기 기본값과 같다고 보고 `<col>` 을
아예 쓰지 않는데 Excel 의 실제 기본은 8.43 이라 그림이 조용히 어긋난다(실측). `emit()` 이
지정한 너비가 파일에 살아남았는지 왕복으로 대조하고 어긋나면 `exit 1` 이다.

`04`·`05` 생성기만 **원본을 파싱한다.** `build_00.js` 와 `proc/slides/*.js`·`wireframe/screens/*.js` 는
내용이 하드코딩돼 있어 기준선을 고쳐도 산출물이 따라오지 않는다(`database/CLAUDE.md` §3).
`xlsx/md.js` 가 마크다운 표·코드펜스를 뽑고 두 생성기가 그것만 쓴다 — 기준선을 다시 봉인하면
`node tools/docgen/xlsx/build_04.js && node tools/docgen/xlsx/build_05.js` 만 돌리면 된다.
두 생성기는 마지막에 **행 수를 기준선 선언값과 대조**하고 어긋나면 `exit 1` 이다.

## 실행 환경

Node 18+ / `pptxgenjs@4` / `exceljs@4`.
모듈 경로는 `NODE_PATH` 또는 각 스크립트의 경로 상수로 지정한다.

```
node tools/docgen/wireframe/build.js "docs/baseline/output" \
  a0_list,a1_flow,wf_00,wf_pat_01,dlg_pat_02,dlg_pat_01,dlg_pat_03,wf_rsv_01,wf_wrk_01,\
dlg_rsv_01,cnf_rsv_01,dlg_rcp_01,dlg_rcp_02,cnf_rcp_01,dlg_log_01,\
z_a_rules,z_b_search,z_c_validation,z_d_ribbon,z_e_fields,z_f_impl \
  "03_검진_예약접수_화면설계서.pptx"
```

## 03 화면설계서 구조

- `spec.js` — 페이지 규격·색·선 굵기·폰트 크기. **여기만 고치면 전 장에 적용된다.**
- `canvas.js` — 도형 목록 하나에서 **PPTX와 SVG를 동시에** 뽑는다. `font.js`가 `malgun.ttf` 의 `hmtx`를 직접 읽어 실제 글자 폭을 재므로 줄바꿈·넘침을 생성 시점에 계산할 수 있다.
- `kit.js` — 창 셸 / 패널 / 필드 / 그리드 / 모달 / 확인창 / 조회조건 밴드.
- `page.js` — 제목 + 우측 설명표. **설명표가 5.9in을 넘으면 빌드를 실패시킨다.**
- `tablepage.js` — 부록용 전폭 표. 행 높이를 실측해 자동 페이지 분할하고, 캡션만 남는 고아 페이지를 만들지 않는다.
- `screens/*.js` — 화면 1개 = 파일 1개. `{ title, draw(canvas), desc[] }` 또는 `{ pages: [...] }`.
- `kit.js`의 `searchBand`는 **버튼을 먼저 배치하고 필드가 그 경계를 넘으면 경고한다.** 필드를 먼저
  그리면 마지막 필드가 우측정렬된 [조회] 밑으로 들어가 그림에서 사라지는데, 경고가 없으면 아무도 모른다.
- `tablepage.js`의 페이지 분할은 `keepWithNext`(캡션·헤더)를 다음 장으로 함께 넘긴다. 캡션만 앞 장에
  고아로 남기지 않는다.

### 왜 `addTable`을 쓰지 않는가

`pptxgenjs`의 `addTable`은 행 높이를 계산하지 않고 `<a:tr h="0">`으로 쓴다. PowerPoint가 열 때 행을 늘리므로 **넘침이 생성 시점에 드러나지 않고 조용히 슬라이드 밖으로 나간다.** 모든 표를 사각형 + 텍스트로 직접 그리고 높이를 스크립트가 계산한다.

## 눈으로 확인하는 방법

생성물이 실제로 어떻게 보이는지 확인하지 않는 것이 가장 위험하다. 빌드는 슬라이드마다 SVG/HTML을
`D:/tmp/hcwork/preview`(`WF_PREVIEW` 환경변수로 변경)에 쓴다. Chrome 헤드리스로 PNG를 떠서 본다.

```
"/c/Program Files/Google/Chrome/Application/chrome.exe" --headless --disable-gpu --no-sandbox \
  --hide-scrollbars --window-size=1600,900 \
  --screenshot="D:/tmp/hcwork/preview/wf_rsv_01.png" \
  "file:///D:/tmp/hcwork/preview/wf_rsv_01.html"
```

- **Chrome은 비동기로 파일을 쓴다.** 실행과 확인을 별도 명령으로 분리할 것.
- 경로는 정슬래시로 쓸 것.

## 검증

```
node tools/docgen/wireframe/check.js <pptx>   # OPC 무결성 + 슬라이드 밖 도형 0개
node tools/docgen/proc/check.js <pptx>        # 위와 동일 (01용)
node tools/docgen/verify_xlsx.js <xlsx...>    # 시트·틀고정·필터·금지 문자열·규칙 ID 커버리지
```

두 `check.js`는 압축해제 디렉터리를 **비우고** 푼다. 같은 디렉터리에 이어서 풀면 이전 파일에서 남은
슬라이드가 그대로 검사되어 실제로 없는 슬라이드가 통과한 것처럼 보인다.

## 「동반 문서」·「읽는 순서」는 docgen 자체 서술이다

`proc/slides/s00.js` 의 동반 문서 표와 `xlsx/build_00.js` 의 「읽는 순서」 행은 **기준선 `00`~`03` 에 대응 문장이 없다**(전체 검색 0건). docgen 이 공개 세트를 4종으로 정의한 것이며 원본이 정한 것이 아니다.

문서 세트가 늘면(예: `04` DB 설계서 PPT) **두 곳을 함께 고친다.** 어느 게이트도 이 둘의 불일치를 잡지 못한다 — `CNL` 이 R3 재봉인을 통과해 살아남은 것과 같은 구조다.

원본이 참조하는 방향은 뒤에서 앞이다(`04` → `01` 2건 · `04` → `00` 5건, `01` → `04` 0건). 새 문서를 더할 때 그 방향을 뒤집지 않는다.

## 주의

- 원본을 파싱할 때 `02_Function_Definition.xlsx` 는 **exceljs로 읽히지 않는다**(모든 요소에 `x:` 접두사 + `xl/tables/*` ListObject). JSZip으로 SpreadsheetML을 직접 읽는다.
- 원본에서 문구를 옮길 때 `기준일`·`최종` 같은 키워드를 일괄 치환하면 업무 규칙이 깨진다
  (`대상판정 기준일은 예약일이다`, `검사결과 입력·판독·최종 판정`). 지정된 셀·줄만 바꾼다.
