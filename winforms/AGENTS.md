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

**Suspended for the overhaul (ROOT `AGENTS.md` §1.1).** Nearly every commit now changes
`winforms/`, so regenerating the manifest each time buys nothing. The manifest stays where it
is and the rule stays written here; it resumes when the overhaul lands. Until then
`verify-winforms-unchanged.sh` reads stale and is not evidence of anything — do not cite it
either way.

### The screen is not matched to `03` or to the design source

**2026-09-10, by the user (ROOT `AGENTS.md` §1.1): building screens to match
`03_Wireframe_Definition.md` and `../tools/docgen/wireframe/screens/*.js` is stopped.** The UI
and the code approach are being substantially revised. Build the screen from the UX judgement
the task states; `03` supplies business rules and field meaning, not layout and not navigation.

`../tools/docgen/wireframe/` keeps drawing the published `pptx` and is still the place to edit
that deliverable — it is no longer a source for implementation.

`// 화면 ID: <ID>` at the top of a screen's C# files is now **convention, not a gate**. The
gate that read it does not run. Keep writing it: it is how a human finds every file of one
screen. `grep -rl "화면 ID:" --include=*.cs` is the count; do not write the number here —
nothing checks it any more, so it rots (ROOT `AGENTS.md` §6, and it already did).

### The one gate that does not run

**Exactly one gate is stopped under ROOT `AGENTS.md` §1.1: `verify-screen-design.js`.** It is
the only one that reads the design source as the truth about layout, which is what §1.1
released. Its failures are the release itself — `SCR-003` (ribbon buttons and group order) and
`SCR-004` (`03` labels absent from C#).

`SCR-SELFTEST` fails with them, deterministically, and it is not a separate defect: its first
case copies the current tree and asserts the check passes on it, so an intended red makes the
copy red too. Measured three times on 2026-09-10 — all three the same, and the other six
selftest cases pass. Do not go hunting.

`[!]` **`scripts/test.sh` red must be that one gate and nothing else.** Anything else red is a
real defect, not overhaul noise. Read the output; do not wave the whole run off.

The stop was once written as seven gates. Six of those were measured green on 2026-09-10 and
turned back on the same day — the long stopped-list had become a blanket excuse, and under it
nobody noticed the six were passing. Keep this list at one.

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
