# ui-tests.ps1 — WinUICalc end-to-end UI test suite.
# Exercises every requirement called out in the project brief.
#
# Usage:
#   .\ui-tests.ps1 -AppPid 12345
#
# All tests target the running app via UIA AutomationIds (see MainWindow.xaml).

param([Parameter(Mandatory)][int]$AppPid)

$ErrorActionPreference = 'Continue'
$pass = 0
$fail = 0
$results = @()

function Test-UI {
    param([string]$Name, [scriptblock]$Script)
    try {
        $output = & $Script 2>&1
        if ($LASTEXITCODE -eq 0) {
            $script:pass++
            $script:results += @{ name = $Name; status = "PASS" }
            Write-Host "  PASS: $Name" -ForegroundColor Green
        } else {
            $script:fail++
            $script:results += @{ name = $Name; status = "FAIL"; detail = "$output" }
            Write-Host "  FAIL: $Name -- $output" -ForegroundColor Red
        }
    } catch {
        $script:fail++
        $script:results += @{ name = $Name; status = "FAIL"; detail = "$_" }
        Write-Host "  FAIL: $Name -- $_" -ForegroundColor Red
    }
}

# Helper: press a button by its AutomationId, then sleep briefly so the next
# UIA query sees the updated TextBlock.
function Press {
    param([string]$AutomationId)
    winapp ui invoke $AutomationId -a $AppPid 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to invoke $AutomationId" }
    Start-Sleep -Milliseconds 80
}

# Helper: clear via the Clear button between scenarios.
function Reset {
    Press "BtnClear"
}

# Confirm process is alive at the start of the run — fail fast otherwise.
$proc = Get-Process -Id $AppPid -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Host "App PID $AppPid is not running." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== App launch / window visibility ===" -ForegroundColor Cyan

Test-UI "App window is alive" {
    $p = Get-Process -Id $AppPid -ErrorAction SilentlyContinue
    if (-not $p) { throw "process not found" }
    if (-not $p.Responding) { throw "process not responding" }
    $global:LASTEXITCODE = 0
}

Test-UI "Title bar is present" {
    winapp ui wait-for "AppTitleBar" -a $AppPid -t 3000
}

Test-UI "Display element is present" {
    winapp ui wait-for "DisplayText" -a $AppPid -t 3000
}

Test-UI "Initial display is 0" {
    Reset
    winapp ui wait-for "DisplayText" -a $AppPid --value "0" -t 2000
}

Write-Host ""
Write-Host "=== Digit entry ===" -ForegroundColor Cyan

Test-UI "Digit buttons update display: 1 2 3 -> 123" {
    Reset
    Press "BtnDigit1"
    Press "BtnDigit2"
    Press "BtnDigit3"
    winapp ui wait-for "DisplayText" -a $AppPid --value "123" -t 2000
}

Test-UI "Pressing 0 first stays as 0" {
    Reset
    Press "BtnDigit0"
    winapp ui wait-for "DisplayText" -a $AppPid --value "0" -t 2000
}

Test-UI "Decimal point inserts: 1 . 5 -> 1.5" {
    Reset
    Press "BtnDigit1"
    Press "BtnDecimal"
    Press "BtnDigit5"
    winapp ui wait-for "DisplayText" -a $AppPid --value "1.5" -t 2000
}

Write-Host ""
Write-Host "=== Arithmetic ===" -ForegroundColor Cyan

Test-UI "Addition: 2 + 3 = 5" {
    Reset
    Press "BtnDigit2"
    Press "BtnAdd"
    Press "BtnDigit3"
    Press "BtnEquals"
    winapp ui wait-for "DisplayText" -a $AppPid --value "5" -t 2000
}

Test-UI "Subtraction (negative result): 2 - 5 = -3" {
    Reset
    Press "BtnDigit2"
    Press "BtnSubtract"
    Press "BtnDigit5"
    Press "BtnEquals"
    winapp ui wait-for "DisplayText" -a $AppPid --value "-3" -t 2000
}

Test-UI "Multiplication: 6 * 7 = 42" {
    Reset
    Press "BtnDigit6"
    Press "BtnMultiply"
    Press "BtnDigit7"
    Press "BtnEquals"
    winapp ui wait-for "DisplayText" -a $AppPid --value "42" -t 2000
}

Test-UI "Division: 20 / 4 = 5" {
    Reset
    Press "BtnDigit2"
    Press "BtnDigit0"
    Press "BtnDivide"
    Press "BtnDigit4"
    Press "BtnEquals"
    winapp ui wait-for "DisplayText" -a $AppPid --value "5" -t 2000
}

Test-UI "Decimal math: 1.5 + 2.25 = 3.75" {
    Reset
    Press "BtnDigit1"
    Press "BtnDecimal"
    Press "BtnDigit5"
    Press "BtnAdd"
    Press "BtnDigit2"
    Press "BtnDecimal"
    Press "BtnDigit2"
    Press "BtnDigit5"
    Press "BtnEquals"
    winapp ui wait-for "DisplayText" -a $AppPid --value "3.75" -t 2000
}

Write-Host ""
Write-Host "=== Error handling ===" -ForegroundColor Cyan

Test-UI "Divide by zero: 10 / 0 = Error" {
    Reset
    Press "BtnDigit1"
    Press "BtnDigit0"
    Press "BtnDivide"
    Press "BtnDigit0"
    Press "BtnEquals"
    winapp ui wait-for "DisplayText" -a $AppPid --value "Error" -t 2000
}

Test-UI "App still alive after divide-by-zero" {
    $p = Get-Process -Id $AppPid -ErrorAction SilentlyContinue
    if (-not $p) { throw "process exited" }
    if (-not $p.Responding) { throw "process not responding" }
    $global:LASTEXITCODE = 0
}

Test-UI "C resets display from Error to 0" {
    Press "BtnClear"
    winapp ui wait-for "DisplayText" -a $AppPid --value "0" -t 2000
}

Test-UI "Calculator usable after Clear-from-error" {
    Reset
    Press "BtnDigit7"
    Press "BtnAdd"
    Press "BtnDigit3"
    Press "BtnEquals"
    winapp ui wait-for "DisplayText" -a $AppPid --value "10" -t 2000
}

Write-Host ""
Write-Host "=== Chaining and clear ===" -ForegroundColor Cyan

Test-UI "Chaining after =: 2 + 3 = then * 4 = 20" {
    Reset
    Press "BtnDigit2"
    Press "BtnAdd"
    Press "BtnDigit3"
    Press "BtnEquals"
    Press "BtnMultiply"
    Press "BtnDigit4"
    Press "BtnEquals"
    winapp ui wait-for "DisplayText" -a $AppPid --value "20" -t 2000
}

Test-UI "Backspace removes last char: 123 -> 12" {
    Reset
    Press "BtnDigit1"
    Press "BtnDigit2"
    Press "BtnDigit3"
    Press "BtnBackspace"
    winapp ui wait-for "DisplayText" -a $AppPid --value "12" -t 2000
}

Test-UI "C resets display to 0" {
    Press "BtnDigit5"
    Press "BtnDigit5"
    Press "BtnClear"
    winapp ui wait-for "DisplayText" -a $AppPid --value "0" -t 2000
}

Write-Host ""
Write-Host "=== Accessibility audit ===" -ForegroundColor Cyan

# Reset state so the inspect snapshot doesn't depend on the last test.
Reset

$expectedIds = @(
    'BtnDigit0','BtnDigit1','BtnDigit2','BtnDigit3','BtnDigit4',
    'BtnDigit5','BtnDigit6','BtnDigit7','BtnDigit8','BtnDigit9',
    'BtnDecimal','BtnAdd','BtnSubtract','BtnMultiply','BtnDivide',
    'BtnEquals','BtnClear','BtnBackspace','DisplayText'
)
foreach ($id in $expectedIds) {
    Test-UI "AutomationId present: $id" {
        winapp ui wait-for $id -a $AppPid -t 1500
    }
}

# Inspect tree and assert every interactive Button in our app window has both
# an AutomationId AND a non-empty Name (UIA accessible name).
$inspectJson = winapp ui inspect -a $AppPid --interactive --json 2>$null
$elements = ($inspectJson | ConvertFrom-Json).elements
$appButtons = @($elements | Where-Object {
    $_.type -eq 'Button' -and
    $_.name -notmatch 'Minimize|Maximize|Close' -and
    $_.className -notmatch 'PickerHost|#32770'
})

$missingId = @($appButtons | Where-Object { -not $_.automationId })
if ($missingId.Count -eq 0) {
    $pass++
    $results += @{ name = "Every app Button has AutomationId"; status = "PASS" }
    Write-Host "  PASS: Every app Button has AutomationId" -ForegroundColor Green
} else {
    $fail++
    $names = ($missingId | ForEach-Object { "'$($_.name)'" }) -join ", "
    $results += @{ name = "Every app Button has AutomationId"; status = "FAIL"; detail = "Missing on: $names" }
    Write-Host "  FAIL: AutomationId missing on $names" -ForegroundColor Red
}

$missingName = @($appButtons | Where-Object { -not $_.name })
if ($missingName.Count -eq 0) {
    $pass++
    $results += @{ name = "Every app Button has Name"; status = "PASS" }
    Write-Host "  PASS: Every app Button has Name" -ForegroundColor Green
} else {
    $fail++
    $ids = ($missingName | ForEach-Object { "'$($_.automationId)'" }) -join ", "
    $results += @{ name = "Every app Button has Name"; status = "FAIL"; detail = "Missing on: $ids" }
    Write-Host "  FAIL: Name missing on $ids" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Final screenshot ===" -ForegroundColor Cyan
$shotPath = Join-Path $PSScriptRoot 'test-screenshot.png'
winapp ui screenshot -a $AppPid -o $shotPath 2>&1 | Out-Null

# ─── Results ───
Write-Host ""
Write-Host "Passed: $pass | Failed: $fail" -ForegroundColor Cyan
$results | ConvertTo-Json -Depth 4 | Out-File (Join-Path $PSScriptRoot 'test-results.json')
if ($fail -gt 0) { exit 1 } else { exit 0 }
