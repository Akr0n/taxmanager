#requires -Version 7
<#
  PostToolUse hook (Claude Code).

  WHY: TaxManager's central correctness invariant is that the tax rulesets stored as
  DATA (data/rulesets/*.json) stay in sync with the code fallback in the Domain
  (SampleRulesets.cs) — a test in TaxManager.Domain.Tests verifies JSON and code
  produce identical results. This hook runs that suite the instant a ruleset or any
  Domain calculator changes, so divergence is caught before build/commit.

  PROTOCOL: receives the tool event as JSON on stdin; reads tool_input.file_path.
  Exit 0 = nothing to do / tests passed. Exit 2 = tests failed (stderr surfaced to Claude).
  NOTE: the failure message uses [Console]::Error.WriteLine (not Write-Error) so that
  $ErrorActionPreference='Stop' does not throw and mask the intended exit code.
#>

$ErrorActionPreference = 'Stop'

try {
    $event = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $path  = $event.tool_input.file_path
} catch {
    exit 0   # not a parseable file event -> nothing to do
}

if (-not $path) { exit 0 }

$norm = $path -replace '\\', '/'

# Trigger only for tax-relevant changes:
#   - data/rulesets/*.json  or  data/addizionali/*.json   (rulesets-as-data)
#   - any .cs under src/TaxManager.Domain                  (calculators / SampleRulesets)
$pattern = '(?i)(data/(rulesets|addizionali)/.*\.json$)|(taxmanager\.domain/.*\.cs$)'
if ($norm -notmatch $pattern) { exit 0 }

$file = [System.IO.Path]::GetFileName($path)
Write-Host "[hook] Tax ruleset/calculator change ($file) -> running Domain parity tests..."

$proj = 'tests/TaxManager.Domain.Tests/TaxManager.Domain.Tests.csproj'
dotnet test $proj --nologo -v quiet
$code = $LASTEXITCODE

if ($code -ne 0) {
    [Console]::Error.WriteLine("[hook] Domain tests FAILED (exit $code). The JSON<->code ruleset parity (or another Domain invariant) is broken; fix it before continuing.")
    exit 2
}

Write-Host "[hook] Domain tests passed."
exit 0
