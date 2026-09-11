## WinForms development kit

Before modifying, reviewing, building, or testing code in this repository,
read `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` and apply its
technical requirements alongside this project's guidance.

Existing applicable project instructions take precedence over the kit.
Report incompatible framework, dependency, or stored-procedure contracts
before changing affected code; do not change them to fit the kit.
If the file cannot be read, report the missing guidance and do not claim
that the kit was applied. Never replace the existing project instructions.

## winforms/ — Phase 5 work boundary

The repository-wide rules live in ROOT `AGENTS.md`: the `00`–`07` document chain, the sealed
baseline, the generated deliverables, and the single source for the two naming layers.
**Do not copy any of that here** (ROOT `AGENTS.md` §6). This section states only what is true
in this directory alone.

### Write and read boundary

```text
write   winforms/**  ·  ../docs/phase5/
read    ../docs/baseline/  ·  ../database/**
```

Opening anything under `../docs/baseline/` is a reseal, defined by ROOT `AGENTS.md` §2, and
that procedure belongs to the database series. If the contract looks wrong, do not fix it —
stop and report to the user.

### `05` outranks the kit

**The single source for every SP and DB contract is
`../docs/baseline/05_DB_Rule_SP_Contract.md`, and `05` outranks any SP-related rule in the
kit.** The kit body and `contract/repository.md` do contradict this repository in places; the
start-of-work handoff document records which parts and why. The kit itself prescribes this
resolution (`PROJECT_INSTRUCTIONS.md`, opening note).

### The contract and the DB deployment are frozen

**2026-09-10, by the user: `docs/baseline/` and `database/deploy/` are not changed again.**
Phase 5 finishes inside `winforms/`. R11 through R15 opened the chain five times; that is over.

`scripts/verify-db-frozen.sh` judges it every round — a declaration alone rots (ROOT
`AGENTS.md` §6). `DBF-001` delegates the sealed documents to `verify-baseline.sh` so the
hashes stay in one place; `DBF-002` pins the twelve deployment files against
`artifacts/db-frozen-manifest.txt`.

**Red does not mean "do not change it" — it means "give the change a round name."** If the
contract genuinely has to open, follow the reseal procedure (ROOT `AGENTS.md` §2.2), run
`./scripts/verify-db-frozen.sh init`, and put the regenerated manifest in that same commit.
That is the same rule the winforms manifest follows.

### Changing winforms means regenerating the manifest in the same commit

`../database/scripts/verify-winforms-unchanged.sh` judges whether the database series left
winforms alone, so a legitimate Phase 5 change turns it red as well. In the commit that
changes anything under `winforms/`, run
`cd ../database && ./scripts/verify-winforms-unchanged.sh init` and include the regenerated
manifest in that same commit — the same rule ROOT `AGENTS.md` §2.2 applies to reseals.

### The screen is not matched to `03` or to the design source

**2026-09-10, by the user (ROOT `AGENTS.md` §1.1): building screens to match
`03_Wireframe_Definition.md` and `../tools/docgen/wireframe/screens/*.js` is stopped.** The UI
and the code approach are being substantially revised. Build the screen from the UX judgement
the task states; `03` supplies business rules and field meaning, not layout and not navigation.

`../tools/docgen/wireframe/` keeps drawing the published `pptx` and is still the place to edit
that deliverable — it is no longer a source for implementation.

**The running program is the truth; the published `03` is a record of the design moment.**
2026-09-11, by the user (`docs/phase5/2026-09-10-session-17-Phase5-UI-Overhaul.md` §4.19):
the published deliverable is not brought back in line with the screens that were built.
It stays behind on purpose. Cite it for business rules and field meaning (that part still
comes from `03` itself); never cite its layout or navigation as what the program does.

`// 화면 ID: <ID>` at the top of a screen's C# files is now **convention, not a gate**. The
gate that read it does not run. Keep writing it: it is how a human finds every file of one
screen. `grep -rl "화면 ID:" --include=*.cs` is the count; do not write the number here —
nothing checks it any more, so it rots (ROOT `AGENTS.md` §6, and it already did).

### The checks that do not run — three of them, inside one script

**`verify-screen-design.js` is not one thing.** It carries five checks and §1.1 released three
of them, not the script:

```text
runs      SCR-000  the design source is readable at all
runs      SCR-001  screens/*.js screen IDs ↔ 03 §2, both directions
stopped   SCR-002  kit.js NAV ↔ MainForm RibbonPage order
stopped   SCR-003  per-screen ribbon group names and buttons
stopped   SCR-004  design labels ⊆ that screen's C# strings
```

ROOT `AGENTS.md` §1.1 released *implementation ↔ `03`*, which is exactly `SCR-002`·`003`·`004`.
`SCR-000`·`001` compare the **design source** to `03` — that the deliverable generator still
draws every screen `03` declares. That invariant never stopped being true, so it keeps running.

`scripts/test.sh` therefore runs the script in `source` mode:

```text
node tools/verify-screen-design.js selftest source   three cases, all green
node tools/verify-screen-design.js source            SCR-000·001
```

`[!]` **`scripts/test.sh` must be green. Every red is a real defect.** There is no longer an
"expected red" to wave off — that exemption is gone as of 2026-09-11 (user decision), because a
permanently red run teaches the next person to stop reading the output.

Running the script **without** `source` is still an intended red, and so is its full selftest
(the first case copies the current tree and asserts the check passes on it, so an intended red
makes the copy red too). Do not go hunting there; that full mode is what a future round would
run if §1.1 were ever revived.

The stop was once written as seven gates, then one, and is now three checks inside one script.
Each narrowing came from measuring rather than assuming — six of the original seven were green
on 2026-09-10, and two of this script's five were green on 2026-09-11. Narrow it again the same
way: measure first.

**The three are not one decision either — 2026-09-11, measured.** Do not write "permanent" here,
and do not write "structurally impossible": an earlier draft of this section said the latter and
was wrong. Both closures (`§1.1` on implementation→`03`, `§4.19` on generator→screens) are
decisions, and a decision can be changed. What differs is the cost, and it splits the three.

```text
SCR-002·003   the ribbon: kit.js NAV, per-screen groups and buttons.  9 mismatches,
              every one an intended change (신규예약 → 예약, DLG-PAT-02 removed,
              [조회]·[컬럼설정] moved out of the ribbon, the 검색 group dropped).
              **The target is now stable** — 14 ribbon buttons, all wired, no more coming.
              Revivable by refreshing the design source. The price: the published 03 then
              draws the current ribbon, which reverses §4.19 for the ribbon alone.

SCR-004       the screen interior: 62 labels over 10 screens. **Not revivable as written**,
              for two reasons that refreshing the design source does not touch:
                - the check reads only files carrying that screen's 화면 ID marker, but the
                  commands moved to the shared ribbon. 휴무일 captions live in
                  MainForm.Designer.cs as 휴무일추가/휴무일수정/휴무일삭제; the design
                  source says 추가/수정/삭제. Same button, different file, different string
                - many "labels" are wireframe annotations, not UI strings - for example
                  "휴무일 목록 - 휴무일자 오름차순 고정". Satisfying the check would mean
                  planting explanatory prose into C# as string literals
              Reviving this one is a redesign of the check, not a refresh of its expectations.
```

**Timing governs both.** While the UI/UX pass runs - screens change on every instruction -
refreshing the design source only makes it stale again on the next one, and `screens/*.js`
draws the published `03`, so refreshing costs the design-time record §4.19 chose to keep.
Revisit after that pass ends, and measure again before deciding.

```text
runs, and catches what a unit test cannot
  verify-no-secret.sh       credentials in winforms
  verify-db-frozen.sh       a silent edit to the frozen contract — needed more, not less,
                            while the UI is being torn up
  verify-ui-baseline.sh     kit §1 technique (DPI, 굴림 9pt, BOM+CRLF)
  verify-layering.sh        kit §2·§3 — SqlClient only in Repositories/, DevExpress only in
                            Views/, no AddWithValue, no inline DML. Architecture, not design
  verify-rs-columns.sh      `reader.GetOrdinal("오늘날짜")` typos: they compile, they pass the
                            fake-repository tests, and they blow up only at runtime
  verify-dbcode.sh          DbCode.cs ↔ 05 §16.1 — the enum is copied, so it is checked
  verify-social-century.sh  03 §6.2 주민번호 century table ↔ the presenter's branch
  verify-contract-names.sh  05 §1.1 names ↔ App.config · csproj
  verify-ui-db-matrix.sh    07 §3·§4 ID lists ↔ 03 §2 · 05 §1.3 (ID lists only, not status)
  MSBuild + vstest          a build and unit tests are not a gate
```

### What the toolchain writes follows the kit; the rest follows the repository

C# sources and the csproj take the kit's line endings. Everything else here is LF, like the
rest of the repository. The split is not a compromise — it is drawn where the toolchain
writes: Visual Studio and the DevExpress designer rewrite those files on every save, and
nothing but a human rewrites the shell scripts and Markdown. EditorConfig is an editor rule
and has no authority over the csproj at all; the project system writes that one.

The values live in `.editorconfig`, and `scripts/verify-ui-baseline.sh` `UIB-005` judges
them; do not restate either here.

`.editorconfig` reaches new lines and new files only, so an existing file keeps whatever it
has. Converting one is a commit of its own — folded into a functional change it turns the
diff into a whole-file replacement and buries what actually changed.

### Connection strings use integrated authentication only

A connection string that supplies `user id` or `uid`, or that turns `integrated security`
off, is forbidden in this repository.

Written without the `=` that would follow each of those keys in a real connection string:
the secret scanner matches key-plus-value, so spelling them out here would make this very
rule the first thing it reports.
