$ErrorActionPreference = 'Stop'

$Utf8 = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $Utf8
$OutputEncoding = $Utf8

if (Get-Variable -Name PSStyle -ErrorAction SilentlyContinue) {
    $PSStyle.OutputRendering = 'Ansi'
}

$Esc = [char]0x1B
$Csi = "$Esc["
$Reset = "${Csi}0m"

$ColorNames = @(
    'Black', 'DarkRed', 'DarkGreen', 'DarkYellow',
    'DarkBlue', 'DarkMagenta', 'DarkCyan', 'Gray',
    'DarkGray', 'Red', 'Green', 'Yellow',
    'Blue', 'Magenta', 'Cyan', 'White'
)

Write-Host "${Csi}1;35mPowerShell color output methods${Reset}"
Write-Host 'This compares host-managed colors with explicit VT/SGR output.'
Write-Host ''

Write-Host "${Csi}1;36m=== Write-Host color parameters ===${Reset}"
Write-Host 'These are limited to the 16 System.ConsoleColor values.'
Write-Host 'Their preservation depends on the PowerShell host when stdout is redirected.'
Write-Host "SupportsVirtualTerminal: $($Host.UI.SupportsVirtualTerminal)"

foreach ($ColorName in $ColorNames) {
    $Arguments = @{
        Object = "Write-Host -ForegroundColor $ColorName"
        ForegroundColor = [ConsoleColor]::$ColorName
    }

    if ($ColorName -in @('Black', 'DarkBlue')) {
        $Arguments.BackgroundColor = [ConsoleColor]::Gray
    }

    Write-Host @Arguments
}

Write-Host ''
Write-Host 'Foreground and background parameters:'
Write-Host '  warning: black on yellow' -ForegroundColor Black -BackgroundColor Yellow
Write-Host '  success: white on dark green' -ForegroundColor White -BackgroundColor DarkGreen
Write-Host '  info: white on dark blue' -ForegroundColor White -BackgroundColor DarkBlue

Write-Host ''
Write-Host "${Csi}1;36m=== Write-Host with explicit SGR ===${Reset}"
Write-Host 'Literal SGR sequences are preserved by ScriptRunner output capture.'
$StandardCodes = @(30, 31, 32, 33, 34, 35, 36, 37)
$BrightCodes = @(90, 91, 92, 93, 94, 95, 96, 97)
$AnsiNames = @('black', 'red', 'green', 'yellow', 'blue', 'magenta', 'cyan', 'white')

for ($Index = 0; $Index -lt $AnsiNames.Count; $Index++) {
    $Background = if ($Index -eq 0) { 107 } else { 49 }
    Write-Host "${Csi}${Background};$($StandardCodes[$Index])m $($AnsiNames[$Index]) ${Reset} " -NoNewline
    Write-Host "${Csi}${Background};$($BrightCodes[$Index])m bright $($AnsiNames[$Index]) ${Reset}"
}

Write-Host "${Csi}38;5;208mWrite-Host indexed color 208${Reset}"
Write-Host "${Csi}38;2;255;90;170mWrite-Host true color RGB(255,90,170)${Reset}"
Write-Host "${Csi}1;3;4;38;2;80;210;255mWrite-Host combined bold, italic, underline, and RGB${Reset}"

[Console]::WriteLine('')
[Console]::WriteLine("${Csi}1;36m=== [Console]::WriteLine with explicit SGR ===${Reset}")
[Console]::WriteLine('Console.WriteLine writes the same VT sequences directly to stdout.')

for ($Index = 0; $Index -lt $AnsiNames.Count; $Index++) {
    $Background = if ($Index -eq 0) { 107 } else { 49 }
    [Console]::WriteLine(
        "${Csi}${Background};$($StandardCodes[$Index])m $($AnsiNames[$Index]) ${Reset} " +
        "${Csi}${Background};$($BrightCodes[$Index])m bright $($AnsiNames[$Index]) ${Reset}")
}

[Console]::WriteLine("${Csi}38;5;39mConsole.WriteLine indexed color 39${Reset}")
[Console]::WriteLine("${Csi}38;2;255;140;40mConsole.WriteLine RGB foreground${Reset}")
[Console]::WriteLine("${Csi}30;48;2;90;220;170m Console.WriteLine RGB background ${Reset}")

$Gradient = [Text.StringBuilder]::new()
for ($Index = 0; $Index -lt 64; $Index++) {
    $Angle = 2.0 * [Math]::PI * $Index / 64.0
    $Red = [int][Math]::Round(127.5 + (127.5 * [Math]::Sin($Angle)))
    $Green = [int][Math]::Round(127.5 + (127.5 * [Math]::Sin($Angle + (2.0 * [Math]::PI / 3.0))))
    $Blue = [int][Math]::Round(127.5 + (127.5 * [Math]::Sin($Angle + (4.0 * [Math]::PI / 3.0))))
    [void]$Gradient.Append("${Csi}38;2;$Red;$Green;${Blue}m$([char]0x2588)")
}
[void]$Gradient.Append($Reset)

[Console]::WriteLine('Console.WriteLine RGB gradient:')
[Console]::WriteLine($Gradient.ToString())
[Console]::WriteLine("${Csi}1;32mColor output method showcase complete.${Reset}")
