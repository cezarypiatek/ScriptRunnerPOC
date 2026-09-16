$ErrorActionPreference = 'Stop'

$Utf8 = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $Utf8
$OutputEncoding = $Utf8

$Esc = [char]0x1B
$Bell = [char]0x07
$Nul = [char]0x00
$Csi = "$Esc["
$Osc = "$Esc]"
$St = "$Esc\"

function Write-RawLine {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Text)

    [Console]::Out.Write($Text)
    [Console]::Out.Write([Environment]::NewLine)
    [Console]::Out.Flush()
}

function Write-Section {
    param([Parameter(Mandatory)][string]$Title)

    Write-RawLine ''
    Write-RawLine "${Csi}1;36m=== $Title ===${Csi}0m"
}

function ConvertFrom-UnicodeEscapes {
    param([Parameter(Mandatory)][string]$Text)

    return [Text.RegularExpressions.Regex]::Unescape($Text)
}

function Get-GradientRgb {
    param(
        [Parameter(Mandatory)][object[]]$Stops,
        [Parameter(Mandatory)][double]$Position
    )

    $ScaledPosition = [Math]::Max(0.0, [Math]::Min(1.0, $Position)) * ($Stops.Count - 1)
    $Segment = [Math]::Min([int][Math]::Floor($ScaledPosition), $Stops.Count - 2)
    $Amount = $ScaledPosition - $Segment
    $Start = [int[]]($Stops[$Segment] -split ',')
    $End = [int[]]($Stops[$Segment + 1] -split ',')

    return @(
        [int][Math]::Round($Start[0] + (($End[0] - $Start[0]) * $Amount)),
        [int][Math]::Round($Start[1] + (($End[1] - $Start[1]) * $Amount)),
        [int][Math]::Round($Start[2] + (($End[2] - $Start[2]) * $Amount))
    )
}

function New-GradientBar {
    param(
        [Parameter(Mandatory)][object[]]$Stops,
        [int]$Width = 48,
        [switch]$Background
    )

    $Builder = [Text.StringBuilder]::new()
    $ColorMode = if ($Background) { 48 } else { 38 }
    $Cell = if ($Background) { '  ' } else { "$([char]0x2588)$([char]0x2588)" }

    for ($Index = 0; $Index -lt $Width; $Index++) {
        $Position = if ($Width -eq 1) { 0 } else { $Index / ($Width - 1) }
        $Rgb = Get-GradientRgb -Stops $Stops -Position $Position
        [void]$Builder.Append("${Csi}${ColorMode};2;$($Rgb[0]);$($Rgb[1]);$($Rgb[2])m$Cell")
    }

    [void]$Builder.Append("${Csi}0m")
    return $Builder.ToString()
}

function New-GradientText {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][object[]]$Stops
    )

    $Builder = [Text.StringBuilder]::new()
    for ($Index = 0; $Index -lt $Text.Length; $Index++) {
        $Position = if ($Text.Length -eq 1) { 0 } else { $Index / ($Text.Length - 1) }
        $Rgb = Get-GradientRgb -Stops $Stops -Position $Position
        [void]$Builder.Append("${Csi}38;2;$($Rgb[0]);$($Rgb[1]);$($Rgb[2])m$($Text[$Index])")
    }

    [void]$Builder.Append("${Csi}0m")
    return $Builder.ToString()
}

Write-RawLine "${Csi}1;35mScriptRunner VT feature showcase${Csi}0m"
Write-RawLine 'Every line below is emitted as raw VT output by PowerShell.'

Write-Section 'Composable SGR attributes'
Write-RawLine "Normal | ${Csi}1mBold${Csi}22m | ${Csi}2mFaint${Csi}22m | ${Csi}3mItalic${Csi}23m"
Write-RawLine "${Csi}4mUnderline${Csi}24m | ${Csi}21mDouble underline${Csi}24m | ${Csi}9mStrikethrough${Csi}29m | ${Csi}53mOverline${Csi}55m"
Write-RawLine "${Csi}5mSlow blink (rendered steadily)${Csi}25m | ${Csi}6mRapid blink (rendered steadily)${Csi}25m"
Write-RawLine "${Csi}7mInverse video${Csi}27m | Conceal [${Csi}8mthis text is hidden${Csi}28m] restored"
Write-RawLine "${Csi}1;3;4;31;44mCombined bold, italic, underline, red on blue${Csi}0m"
Write-RawLine "${Csi}1;31mBold red ${Csi}22mnot bold ${Csi}39mdefault foreground${Csi}m reset with empty SGR"

Write-Section 'Standard and bright foreground colors'
$ForegroundNames = @('black', 'red', 'green', 'yellow', 'blue', 'magenta', 'cyan', 'white')
for ($Index = 0; $Index -lt $ForegroundNames.Count; $Index++) {
    $NormalCode = 30 + $Index
    $BrightCode = 90 + $Index
    $SampleBackground = if ($Index -eq 0) { 107 } else { 49 }
    Write-RawLine "${Csi}${SampleBackground};${NormalCode}m$($ForegroundNames[$Index])${Csi}0m | ${Csi}${SampleBackground};${BrightCode}mbright $($ForegroundNames[$Index])${Csi}0m"
}

Write-Section 'Standard and bright background colors'
$NormalForegrounds = @(97, 97, 30, 97, 97, 97, 30, 30)
$BrightForegrounds = @(97, 30, 30, 30, 97, 30, 30, 30)
for ($Index = 0; $Index -lt $ForegroundNames.Count; $Index++) {
    $NormalCode = 40 + $Index
    $BrightCode = 100 + $Index
    Write-RawLine "${Csi}$($NormalForegrounds[$Index]);${NormalCode}m background $($ForegroundNames[$Index]) ${Csi}0m | ${Csi}$($BrightForegrounds[$Index]);${BrightCode}m bright background $($ForegroundNames[$Index]) ${Csi}0m"
}

Write-Section '256-color and true-color forms'
Write-RawLine "${Csi}38;5;196mindexed foreground 196${Csi}39m | ${Csi}38;5;46m46${Csi}39m | ${Csi}38;5;21m21${Csi}39m | ${Csi}38;5;244mgray 244${Csi}39m"
Write-RawLine "${Csi}48;5;25m indexed background 25 ${Csi}49m | ${Csi}48;5;208m indexed background 208 ${Csi}49m"
Write-RawLine "${Csi}38;2;255;128;32mRGB foreground (255,128,32)${Csi}39m | ${Csi}48;2;45;55;72m RGB background (45,55,72) ${Csi}49m"
Write-RawLine "${Csi}38:2::80:200:255mColon-form RGB foreground${Csi}39m | ${Csi}48:2::80:40:120m Colon-form RGB background ${Csi}49m"
Write-RawLine "${Csi}4;58;2;255;80;160mRGB-colored underline${Csi}59;24m | ${Csi}4;58;5;46mindexed-color underline${Csi}59;24m"

Write-Section 'True-color gradients'
$RainbowStops = @(
    '255,70,70',
    '255,220,70',
    '70,255,120',
    '70,220,255',
    '90,90,255',
    '220,70,255',
    '255,70,70'
)
$SunsetStops = @(
    '36,0,70',
    '123,44,191',
    '255,77,109',
    '255,158,0',
    '255,230,109'
)
$OceanStops = @(
    '0,18,51',
    '0,80,157',
    '0,180,216',
    '144,224,239'
)

Write-RawLine 'Rainbow foreground blocks:'
Write-RawLine (New-GradientBar -Stops $RainbowStops)
Write-RawLine 'Sunset foreground blocks:'
Write-RawLine (New-GradientBar -Stops $SunsetStops)
Write-RawLine 'Ocean background cells:'
Write-RawLine (New-GradientBar -Stops $OceanStops -Background)
Write-RawLine (New-GradientText -Text 'ScriptRunner renders smooth RGB gradients!' -Stops $RainbowStops)

Write-Section 'Unicode glyphs over UTF-8'
$BoxTop = ConvertFrom-UnicodeEscapes '\u250C\u2500\u2500\u2500\u252C\u2500\u2500\u2500\u2510'
$BoxMiddle = ConvertFrom-UnicodeEscapes '\u2502 A \u2502 B \u2502'
$BoxBottom = ConvertFrom-UnicodeEscapes '\u2514\u2500\u2500\u2500\u2534\u2500\u2500\u2500\u2518'
$Blocks = ConvertFrom-UnicodeEscapes '\u2591\u2592\u2593\u2588 \u2580\u2584\u258C\u2590'
$ArrowsAndMath = ConvertFrom-UnicodeEscapes '\u2190 \u2191 \u2192 \u2193  \u2194 \u21D2  \u2713 \u2717  \u00B1 \u00D7 \u00F7 \u2260 \u2264 \u2265 \u221E \u221A \u03C0'
$Polish = ConvertFrom-UnicodeEscapes 'Za\u017C\u00F3\u0142\u0107 g\u0119\u015Bl\u0105 ja\u017A\u0144'
$Greek = ConvertFrom-UnicodeEscapes '\u039A\u03B1\u03BB\u03B7\u03BC\u03AD\u03C1\u03B1 \u03BA\u03CC\u03C3\u03BC\u03B5'
$Cyrillic = ConvertFrom-UnicodeEscapes '\u041F\u0440\u0438\u0432\u0435\u0442, \u043C\u0438\u0440'
$Japanese = ConvertFrom-UnicodeEscapes '\u3053\u3093\u306B\u3061\u306F\u4E16\u754C'
$Korean = ConvertFrom-UnicodeEscapes '\uC548\uB155\uD558\uC138\uC694 \uC138\uACC4'
$Emoji = ConvertFrom-UnicodeEscapes '\uD83D\uDE80 \uD83C\uDF08 \u2699\uFE0F \u2728 \uD83D\uDFE2'

Write-RawLine "${Csi}38;2;80;200;255m$BoxTop${Csi}0m"
Write-RawLine "${Csi}38;2;80;200;255m$BoxMiddle${Csi}0m"
Write-RawLine "${Csi}38;2;80;200;255m$BoxBottom${Csi}0m"
Write-RawLine "Block elements: ${Csi}38;2;255;100;180m$Blocks${Csi}0m"
Write-RawLine "Arrows and math: ${Csi}38;2;255;220;80m$ArrowsAndMath${Csi}0m"
Write-RawLine "Polish:   ${Csi}38;2;120;220;255m$Polish${Csi}0m"
Write-RawLine "Greek:    ${Csi}38;2;140;255;160m$Greek${Csi}0m"
Write-RawLine "Cyrillic: ${Csi}38;2;255;170;100m$Cyrillic${Csi}0m"
Write-RawLine "Japanese: ${Csi}38;2;220;150;255m$Japanese${Csi}0m"
Write-RawLine "Korean:   ${Csi}38;2;255;130;160m$Korean${Csi}0m"
Write-RawLine "Emoji:    ${Csi}38;2;255;210;80m$Emoji${Csi}0m"

Write-Section 'OSC 8 hyperlinks'
$QuotedUrl = "https://example.com/docs/it's(a-demo)?q=one%20two"
Write-RawLine "ST-terminated: ${Osc}8;;`"$QuotedUrl`"${St}${Csi}38;2;80;200;255mopen a quoted URL containing apostrophe and parentheses${Csi}39m${Osc}8;;${St}"
Write-RawLine "BEL-terminated: ${Osc}8;;https://example.com/bel${Bell}open the BEL hyperlink${Osc}8;;${Bell}"

Write-Section 'Automatic URL and local-path links'
Write-RawLine "URL punctuation: https://example.com/it's-valid(a-demo)"
$PathWithSpaces = Join-Path $PSScriptRoot '..\sample-data\file with spaces.txt'
$PathWithSpaces = [IO.Path]::GetFullPath($PathWithSpaces)
Write-RawLine "Existing path: $PathWithSpaces"

Write-Section 'Horizontal cursor and line editing'
Write-RawLine 'Cursor absolute (G), expected: 01234XYZ89'
Write-RawLine "0123456789${Csi}6GXYZ"
Write-RawLine 'Cursor backward (D), expected: ABCxy'
Write-RawLine "ABCDE${Csi}2Dxy"
Write-RawLine 'Cursor forward (C), expected: A, three spaces, B'
Write-RawLine "A${Csi}3CB"
Write-RawLine 'Erase from cursor to end (K mode 0), expected: keep'
Write-RawLine "keep-REMOVE${Csi}5G${Csi}K"
Write-RawLine 'Erase from start through cursor (K mode 1), expected: four spaces, ef'
Write-RawLine "abcdef${Csi}4G${Csi}1K"
Write-RawLine 'Erase entire line (K mode 2), then home, expected: new text'
Write-RawLine "old text${Csi}2K${Csi}1Gnew text"
Write-RawLine 'Erase two characters (X), expected: AB, two spaces, EF'
Write-RawLine "ABCDEF${Csi}3G${Csi}2X"
Write-RawLine 'Backspace overwrite, expected: ABCxy'
Write-RawLine "ABCDE`b`bxy"
Write-RawLine 'Tab stops, expected columns 1, 9, and 17'
Write-RawLine "A`tB`tC"
Write-RawLine 'Carriage-return overwrite, expected: new value'
Write-RawLine "old value`rnew value"

Write-Section 'Ignored control data'
Write-RawLine "NUL and BEL are removed: left${Nul}${Bell}right"
Write-RawLine "DCS payload is hidden: before ${Esc}Pnot visible${St}after"
Write-RawLine "APC payload is hidden: before ${Esc}_not visible${St}after"
Write-RawLine "PM payload is hidden: before ${Esc}^not visible${St}after"

Write-Section 'Reset and completion'
Write-RawLine "${Csi}1;3;4;9;53;38;5;196;48;5;25mEverything enabled${Csi}0m -> reset to normal"
Write-RawLine "${Csi}1;32mVT showcase complete.${Csi}0m"
