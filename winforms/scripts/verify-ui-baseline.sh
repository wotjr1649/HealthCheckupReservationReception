#!/usr/bin/env bash
# 킷 §1 UI 베이스라인이 소스에 실제로 있는가 (07 §10 P10).
#
# UIB-001  SetPerMonitorDpiAware() 가 Main 의 **첫 문장**이다
# UIB-002  DefaultFont · DefaultMenuFont 가 굴림 9pt 다
# UIB-003  Designer 전건이 AutoScaleMode=Font · AutoScaleDimensions(7F,12F) · 자기 Font 를 직렬화한다
# UIB-004  app.manifest 에 DPI 설정이 없다
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
  run() { # run <라벨> <기대 exit> <Program.cs> <Designer 본문>
    printf '%s\n' "$3" > "$D/src/Program.cs"
    printf '%s\n' "$4" > "$D/src/Views/Frm.Designer.cs"
    PROGRAM="$D/src/Program.cs" SRC="$D/src" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS UIB-SELFTEST $1"
    else echo "FAIL UIB-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  run '전부 맞으면 통과한다'                0 "$GOODPROG" "$GOODDES"
  run 'DPI 호출이 첫 문장이 아니면 잡는다'   1 "${GOODPROG/            WindowsFormsSettings.SetPerMonitorDpiAware();/            Application.EnableVisualStyles();
            WindowsFormsSettings.SetPerMonitorDpiAware();}" "$GOODDES"
  run 'DefaultMenuFont 누락을 잡는다'        1 "${GOODPROG/            WindowsFormsSettings.DefaultMenuFont = new Font(\"굴림\", 9F);/}" "$GOODDES"
  run 'Designer 의 Font 누락을 잡는다'       1 "$GOODPROG" "${GOODDES/            this.Font = new System.Drawing.Font(\"굴림\", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));/}"
  run 'AutoScaleDimensions 변조를 잡는다'    1 "$GOODPROG" "${GOODDES/SizeF(7F, 12F)/SizeF(7F, 14F)}"
  run 'AutoScaleMode 변조를 잡는다'          1 "$GOODPROG" "${GOODDES/AutoScaleMode.Font/AutoScaleMode.Dpi}"
  rm -f "$D/src/Program.cs" "$D/src/Views/Frm.Designer.cs" "$D/out.txt"
  rmdir "$D/src/Views" "$D/src" "$D"
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

if [ "$FAIL" -eq 0 ]; then echo "== PASS UI 베이스라인 =="; else echo "== FAIL =="; fi
exit $FAIL
