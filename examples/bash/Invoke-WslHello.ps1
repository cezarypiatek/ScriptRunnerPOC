param(
    [Parameter(Mandatory)]
    [string]$Text,
    [Parameter(Mandatory)]
    [string]$FilePath
)

function Convert-ToWslPath {
    param([string]$Path)
    $full = [System.IO.Path]::GetFullPath($Path)
    $drive = $full.Substring(0,1).ToLowerInvariant()
    $rest = $full.Substring(2).Replace('\\','/')
    "/mnt/$drive$rest"
}

$scriptDir = Split-Path -Parent $PSCommandPath
$linuxDir = Convert-ToWslPath $scriptDir
$linuxFile = Convert-ToWslPath $FilePath

$arguments = @('--cd', $linuxDir, 'bash', './hello_prompt.sh', $Text, $linuxFile)
Write-Host "[wsl] Executing: wsl $($arguments -join ' ')"
$process = Start-Process -FilePath 'wsl' -ArgumentList $arguments -NoNewWindow -PassThru
$process.WaitForExit()
exit $process.ExitCode
