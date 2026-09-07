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

## 실행 환경

Node 18+ / `pptxgenjs@4` / `exceljs@4`.
모듈 경로는 `NODE_PATH` 또는 각 스크립트의 경로 상수로 지정한다.

```
node tools/docgen/wireframe/build.js "docs/baseline/output" \
  a0_list,a1_flow,wf_00,wf_pat_01,dlg_pat_02,dlg_pat_01,dlg_pat_03,wf_rsv_01,wf_wrk_01,\
dlg_rsv_01,cnf_rsv_01,dlg_rcp_01,dlg_rcp_02,cnf_rcp_01,\
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

## 주의

- 원본을 파싱할 때 `02_Function_Definition.xlsx` 는 **exceljs로 읽히지 않는다**(모든 요소에 `x:` 접두사 + `xl/tables/*` ListObject). JSZip으로 SpreadsheetML을 직접 읽는다.
- 원본에서 문구를 옮길 때 `기준일`·`최종` 같은 키워드를 일괄 치환하면 업무 규칙이 깨진다
  (`대상판정 기준일은 예약일이다`, `검사결과 입력·판독·최종 판정`). 지정된 셀·줄만 바꾼다.
