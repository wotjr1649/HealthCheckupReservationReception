# Build, Test, and File Normalization

Read before editing (to record the state of files you will edit) and again before running the checks that `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 9 requires. Commands are in cmd or bash syntax; in PowerShell prefix the Locate, Build, and Tests commands with `& `. From cmd, run the PowerShell lines in a PowerShell prompt or the bash lines in Git Bash.

## Locate the tools (Visual Studio 2019, any edition)

```text
"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -version "[16.0,17.0)" -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"
```

The same command with `-find "Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"` locates the test runner. vswhere's default product list covers Community, Professional, and Enterprise and excludes Build Tools. The paths below are the two results.

## Work already in progress

Before the first edit, list the files already changed but not committed (`git status --short`, or the SVN equivalent) and leave them alone. Never use `reset`, `checkout`, `clean`, `stash`, or a whole-file rewrite to get past a mixed working tree, and never commit a file the task did not change. When a file the task needs is already modified, change only the lines the task needs and say so in the report.

## Files you edit

Before editing an existing file, record its encoding and line endings; after editing, run the same commands and restore the recorded state when the tool changed it (BOM + CRLF with the lines in the next section, CRLF without BOM with their no-BOM variants).

Two separate commands per shell, BOM first and line endings second:

```text
bash BOM   : head -c 3 "<file>" | od -An -tx1                           ef bb bf = BOM
bash EOL   : grep -cUP '\r' "<file>"                                    0 = LF
PS   BOM   : [IO.File]::ReadAllBytes("<abs path>")[0..2]                239 187 191 = BOM
PS   EOL   : ([regex]::Matches([IO.File]::ReadAllText("<abs path>"), "`r")).Count
```

`-UP '\r'` is not optional. Without `-U`, Git Bash grep strips CR before matching and returns 0 for a CRLF file, so the check passes on everything. With `$'\r'` instead of `-P '\r'` the pattern collapses to empty inside `$(...)` and every line matches, so capturing the count into a variable — which recording the state means — reports a CRLF file for LF input. `-P` makes grep read the escape, so the same command is correct both ways. The PowerShell lines need absolute paths: `[IO.File]` resolves relative paths against the process directory, not the current location.

## Files you created

Create files as UTF-8 (the Write or apply_patch tool, a bash heredoc, or PowerShell `-Encoding UTF8`). Every C# file an agent created is then normalized to UTF-8 BOM + CRLF with a final newline before the build. Other text files you created keep CRLF without a BOM: replace `printf '\xEF\xBB\xBF' >` with `: >` in the bash line, or use `New-Object Text.UTF8Encoding $false` in the PowerShell line.

```text
bash:       printf '\xEF\xBB\xBF' > "<file>.tmp" && sed -e '1s/^\xEF\xBB\xBF//' -e 's/\r$//' -e 's/$/\r/' -e '$a\' "<file>" >> "<file>.tmp" && mv "<file>.tmp" "<file>"
PowerShell: $f = (Resolve-Path "<file>").Path; $t = [IO.File]::ReadAllText($f) -replace "`r?`n", "`r`n"; if (-not $t.EndsWith("`r`n")) { $t += "`r`n" }; [IO.File]::WriteAllText($f, $t, (New-Object Text.UTF8Encoding $true))
```

Both lines leave the file's own trailing blank lines in place and add one final newline only when it had none.

## Build

Restore first, whichever package format the solution uses, then build:

```text
"<MSBuild.exe>" <App>.sln -t:restore -p:RestorePackagesConfig=true
"<MSBuild.exe>" <App>.sln -p:Configuration=Debug -v:m
```

- Performance measurements use `-p:Configuration=Release`.
- `dotnet build` cannot build this csproj (LC task, MSB4803).
- MSB3644 in the log means the 4.6.1 targeting pack is missing: report `Build Verified: Failed`, do not retarget.

## Tests

`<App>.Tests` references `MSTest.TestFramework` and `MSTest.TestAdapter` 2.2.10, registered in its own `<App>.Tests.csproj` (the 2.2.x line ships net45 assets; MSTest 3.x offers net461 only its netstandard2.0 build, which the Section 1 package rule excludes).

```text
"<vstest.console.exe>" "<App>.Tests/bin/Debug/<App>.Tests.dll"
```
