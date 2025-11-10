param(
    [Parameter(Mandatory)]
    [string]$Text,
    [Parameter(Mandatory)]
    [string]$FilePath
)

Write-Host "[hello] Hello from PowerShell!" -ForegroundColor Cyan
Write-Host "[info] Text argument : $Text"
Write-Host "[info] File argument : $FilePath"

if (-not (Test-Path -LiteralPath $FilePath)) {
    Write-Error "File '$FilePath' does not exist." -ErrorAction Stop
}

Write-Host "QUESTION: Display the file content? (Yes/No/Maybe)"
$answer = (Read-Host "Answer").Trim().ToUpperInvariant()

switch ($answer) {
    'YES' {
        Write-Host "[content]" -ForegroundColor Green
        Get-Content -LiteralPath $FilePath | ForEach-Object { Write-Host "  $_" }
        exit 0
    }
    'NO' {
        Write-Host "[skip] Content display skipped." -ForegroundColor Yellow
        exit 0
    }
    'MAYBE' {
        Write-Host "MAYBE_SELECTED - user could not decide." -ForegroundColor Magenta
        exit 2
    }
    default {
        Write-Host "[warn] Unexpected answer '$answer'. Treating as NO." -ForegroundColor Yellow
        exit 0
    }
}
