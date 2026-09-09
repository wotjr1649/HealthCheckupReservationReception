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

### Changing winforms means regenerating the manifest in the same commit

`../database/scripts/verify-winforms-unchanged.sh` judges whether the database series left
winforms alone, so a legitimate Phase 5 change turns it red as well. In the commit that
changes anything under `winforms/`, run
`cd ../database && ./scripts/verify-winforms-unchanged.sh init` and include the regenerated
manifest in that same commit — the same rule ROOT `AGENTS.md` §2.2 applies to reseals.

### Line endings are LF, against the kit

The kit fixes C# sources at UTF-8 BOM + **CRLF**. This repository is LF everywhere — the
sealed documents, the database series SQL and scripts, and the C# that was here before the
kit arrived. LF wins, by the kit's own precedence rule.

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
