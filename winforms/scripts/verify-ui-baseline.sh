#!/usr/bin/env bash
# 킷 §1 이 소스에 실제로 지켜지는가 (07 §10 P10).
#
# UIB-001  SetPerMonitorDpiAware() 가 Main 의 **첫 문장**이다
# UIB-002  DefaultFont · DefaultMenuFont 가 굴림 9pt 다
# UIB-003  Designer 전건이 AutoScaleMode=Font · AutoScaleDimensions(7F,12F) · 자기 Font 를 직렬화한다
# UIB-004  app.manifest 에 DPI 설정이 없다
# UIB-005  .cs 전건이 UTF-8 BOM + CRLF 다 (.editorconfig)
#
# [X] UIB-005 는 사람이 손으로 재다 틀렸던 값이다. contract/build.md 가 경고한 대로
#     grep -c $'\r' 은 $(...) 안에서 패턴이 무너져 LF 파일을 CRLF 로 보고한다.
#     -UP '\r' 이 맞고, 그래서 사람이 아니라 이 게이트가 잰다 (07 §14 X-06).
#
# [D] 2026-09-09 사용자가 .editorconfig 를 crlf 로 되돌렸다. 이 게이트도 함께 뒤집는다 —
#     값이 두 곳에 있으면 한쪽만 고쳐도 아무도 red 가 되지 않는다 (ROOT AGENTS.md §6).
#
# [X] UIB-003 이 이 게이트의 핵심이다. references/designer.md 함정 9 — Program.cs 는 디자인타임에
#     돌지 않으므로, 폼이 자기 Font 를 직렬화하지 않으면 **디자이너가 한 번 저장하는 순간**
#     AutoScaleDimensions 를 자기가 쓰던 글꼴로 다시 계산해 폼 전체를 조용히 재스케일한다.
#     사람이 알아채기 전에 게이트가 잡아야 하는 부류다.
set -uo pipefail
cd "$(dirname "$0")/.."

: "${PROGRAM:=src/HealthCheckupReservationReception.WinForms/Program.cs}"
: "${SRC:=src/HealthCheckupReservationReception.WinForms}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# Main 의 여는 중괄호 다음 첫 실행문 한 줄. 빈 줄과 // 주석은 건너뛴다.
first_stmt() {
  awk '/static +void +Main/ {f=1; next}
       f && /^[[:space:]]*\{/ {g=1; next}
       g' "$1" \
  | grep -vE '^[[:space:]]*(//|$)' | head -1 | sed 's/^[[:space:]]*//'
}

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  mkdir -p "$D/src/Views"
  GOODDES='            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;'
  GOODPROG='        private static void Main()
        {
            // 주석은 첫 문장이 아니다
            WindowsFormsSettings.SetPerMonitorDpiAware();
            WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);
            WindowsFormsSettings.DefaultMenuFont = new Font("굴림", 9F);
        }'
  mkdir -p "$D/empty"
  # 픽스처는 실물과 같은 인코딩(UTF-8 BOM + CRLF)으로 쓴다. UIB-005 가 그것을 본다.
  wcs() { printf '\xEF\xBB\xBF' > "$1"; printf '%s\n' "$2" | sed 's/$/\r/' >> "$1"; }
  fire() { # fire <라벨> <기대 exit>
    PROGRAM="$D/src/Program.cs" SRC="$D/src" TESTS="$D/empty" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS UIB-SELFTEST $1"
    else echo "FAIL UIB-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  run() { # run <라벨> <기대 exit> <Program.cs> <Designer 본문>
    wcs "$D/src/Program.cs" "$3"
    wcs "$D/src/Views/Frm.Designer.cs" "$4"
    fire "$1" "$2"
  }
  run '전부 맞으면 통과한다'                0 "$GOODPROG" "$GOODDES"
  run 'DPI 호출이 첫 문장이 아니면 잡는다'   1 "${GOODPROG/            WindowsFormsSettings.SetPerMonitorDpiAware();/            Application.EnableVisualStyles();
            WindowsFormsSettings.SetPerMonitorDpiAware();}" "$GOODDES"
  run 'DefaultMenuFont 누락을 잡는다'        1 "${GOODPROG/            WindowsFormsSettings.DefaultMenuFont = new Font(\"굴림\", 9F);/}" "$GOODDES"
  run 'Designer 의 Font 누락을 잡는다'       1 "$GOODPROG" "${GOODDES/            this.Font = new System.Drawing.Font(\"굴림\", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));/}"
  run 'AutoScaleDimensions 변조를 잡는다'    1 "$GOODPROG" "${GOODDES/SizeF(7F, 12F)/SizeF(7F, 14F)}"
  run 'AutoScaleMode 변조를 잡는다'          1 "$GOODPROG" "${GOODDES/AutoScaleMode.Font/AutoScaleMode.Dpi}"

  # UIB-005 — 인코딩·줄바꿈. 픽스처를 정상으로 되돌린 뒤 한 항목씩 무너뜨린다.
  run '기준 상태로 되돌린다'                 0 "$GOODPROG" "$GOODDES"
  printf '%s\n' "$GOODPROG" | sed 's/$/\r/' > "$D/src/Program.cs"   # BOM 없이 다시 쓴다
  fire 'BOM 누락을 잡는다'                   1
  wcs "$D/src/Program.cs" "$GOODPROG"
  sed -i 's/\r$//' "$D/src/Program.cs"
  fire 'LF 를 잡는다'                        1

  rm -f "$D/src/Program.cs" "$D/src/Views/Frm.Designer.cs" "$D/out.txt"
  rmdir "$D/src/Views" "$D/src" "$D/empty" "$D"
  exit $RC
fi

[ -f "$PROGRAM" ] || { say FAIL "UIB-000 파일이 없다: $PROGRAM"; echo "== FAIL =="; exit 1; }

# ── UIB-001
STMT=$(first_stmt "$PROGRAM")
if [ -z "$STMT" ]; then
  say FAIL "UIB-001 Main 의 첫 문장을 못 읽었다 — Program.cs 형식이 바뀌었다"
elif [ "$STMT" = "WindowsFormsSettings.SetPerMonitorDpiAware();" ]; then
  say PASS "UIB-001 SetPerMonitorDpiAware() 가 Main 의 첫 문장이다 (킷 §1)"
else
  say FAIL "UIB-001 Main 의 첫 문장이 SetPerMonitorDpiAware() 가 아니다 — '$STMT'"
fi

# ── UIB-002
MISS=
for prop in DefaultFont DefaultMenuFont; do
  grep -qE "WindowsFormsSettings\.$prop[[:space:]]*=[[:space:]]*new Font\(\"굴림\", *9F\)" "$PROGRAM" \
    || MISS="$MISS $prop"
done
if [ -z "$MISS" ]; then
  say PASS "UIB-002 DefaultFont·DefaultMenuFont 가 굴림 9pt 다 (킷 §1)"
else
  say FAIL "UIB-002 굴림 9pt 로 설정되지 않았다 —$MISS"
fi

# ── UIB-003
DESIGNERS=$(find "$SRC" -name '*.Designer.cs' 2>/dev/null | sort)
if [ -z "$DESIGNERS" ]; then
  # [X] 대상이 0건이면 통과가 아니다. 검사할 것이 없는 게 아니라 못 찾은 것일 수 있다.
  say FAIL "UIB-003 Designer 파일을 하나도 못 찾았다 ($SRC)"
else
  N=0; BAD=
  while IFS= read -r d; do
    N=$((N + 1))
    grep -qF 'this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;' "$d" \
      || BAD="$BAD\n    $d — AutoScaleMode=Font 없음"
    grep -qF 'this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);' "$d" \
      || BAD="$BAD\n    $d — AutoScaleDimensions(7F, 12F) 없음"
    grep -qE 'this\.Font = new System\.Drawing\.Font\("굴림", *9F' "$d" \
      || BAD="$BAD\n    $d — 자기 Font(굴림 9F) 직렬화 없음"
  done <<< "$DESIGNERS"
  if [ -z "$BAD" ]; then
    say PASS "UIB-003 Designer $N 건 전부 킷 §1 스케일 기준을 직렬화한다"
  else
    say FAIL "UIB-003 Designer 스케일 기준 위반"
    printf '%b\n' "$BAD"
  fi
fi

# ── UIB-004  킷 §1: app.manifest 는 DPI 설정을 담지 않는다
MAN=$(grep -rlEi 'dpiAware' "$SRC" --include=*.manifest 2>/dev/null || true)
if [ -z "$MAN" ]; then
  say PASS "UIB-004 app.manifest 에 DPI 설정이 없다 (킷 §1)"
else
  say FAIL "UIB-004 manifest 가 DPI 를 설정한다 — Program.cs 와 두 곳이 서로 다른 말을 한다"
  echo "$MAN" | sed 's/^/    /'
fi

# ── UIB-005  .cs 전건이 UTF-8 BOM + CRLF 인가 (.editorconfig)
CSFILES=$(find "$SRC" "${TESTS:-tests}" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' 2>/dev/null | sort)
if [ -z "$CSFILES" ]; then
  say FAIL "UIB-005 .cs 파일을 하나도 못 찾았다"
else
  N=0; BAD=
  while IFS= read -r f; do
    N=$((N + 1))
    [ "$(head -c3 "$f" | od -An -tx1 | tr -d ' \n')" = "efbbbf" ] || BAD="$BAD\n    $f — BOM 없음"
    # CR 로 끝나지 **않는** 줄이 하나라도 있으면 LF 가 섞인 것이다.
    # "CR 이 있는 줄이 하나라도 있는가" 로 재면 한 줄만 CRLF 인 파일이 통과한다.
    [ "$(grep -cUvP '\r$' "$f" || true)" -eq 0 ] || BAD="$BAD\n    $f — LF"
  done <<< "$CSFILES"
  if [ -z "$BAD" ]; then
    say PASS "UIB-005 .cs $N 건 전부 UTF-8 BOM + CRLF 다"
  else
    say FAIL "UIB-005 인코딩·줄바꿈 위반"
    printf '%b\n' "$BAD"
  fi
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS 킷 §1 =="; else echo "== FAIL =="; fi
exit $FAIL
