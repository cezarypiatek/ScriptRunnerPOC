param(
    [ValidateSet('All', 'Burst', 'Sequence', 'Severities', 'CapturedValues', 'RepeatedMatches', 'AnsiOutput', 'NoAlerts')]
    [string]$Scenario = 'All'
)

$ErrorActionPreference = 'Stop'

$Utf8 = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $Utf8
$OutputEncoding = $Utf8

if (Get-Variable -Name PSStyle -ErrorAction SilentlyContinue) {
    $PSStyle.OutputRendering = 'Ansi'
}

$Esc = [char]0x1B
$Csi = "$Esc["

function Write-DemoLine {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Text)

    [Console]::Out.WriteLine($Text)
    [Console]::Out.Flush()
}

function Write-Section {
    param([Parameter(Mandatory)][string]$Title)

    Write-DemoLine ''
    Write-DemoLine "--- $Title ---"
}

function Invoke-BurstScenario {
    Write-Section 'Burst: four alerts emitted without delays'
    Write-DemoLine 'DEMO_INFO [BURST-01] Cache warm-up started.'
    Write-DemoLine 'DEMO_WARNING [BURST-02] Disk space is below the preferred threshold.'
    Write-DemoLine 'DEMO_ERROR [BURST-03] A simulated deployment step failed.'
    Write-DemoLine 'DEMO_SUCCESS [BURST-04] The fallback completed successfully.'
}

function Invoke-SequenceScenario {
    Write-Section 'Sequence: alerts arrive at visibly different times'
    Write-DemoLine 'DEMO_INFO [SEQ-01] Validation started.'
    Start-Sleep -Milliseconds 1100
    Write-DemoLine 'DEMO_WARNING [SEQ-02] Validation found a recoverable issue.'
    Start-Sleep -Milliseconds 1100
    Write-DemoLine 'DEMO_SUCCESS [SEQ-03] Validation completed after recovery.'
}

function Invoke-SeverityScenario {
    Write-Section 'Severity palette'
    Write-DemoLine 'DEMO_ERROR [SEVERITY-ERROR] Error severity example.'
    Write-DemoLine 'DEMO_WARNING [SEVERITY-WARNING] Warning severity example.'
    Write-DemoLine 'DEMO_INFO [SEVERITY-INFO] Informational severity example.'
    Write-DemoLine 'DEMO_SUCCESS [SEVERITY-SUCCESS] Success severity example.'
}

function Invoke-CapturedValuesScenario {
    Write-Section 'Named regex capture substitutions'
    Write-DemoLine 'MISSING_SECRET name=PAYMENTS_API_TOKEN environment=staging'
    Write-DemoLine 'RATE_LIMIT service=OrdersApi retryAfter=45'
}

function Invoke-RepeatedMatchesScenario {
    Write-Section 'Repeated matches of the same rule'
    Write-DemoLine 'RETRY_ATTEMPT operation=UploadReport attempt=1 max=3'
    Write-DemoLine 'RETRY_ATTEMPT operation=UploadReport attempt=2 max=3'
    Write-DemoLine 'RETRY_ATTEMPT operation=UploadReport attempt=3 max=3'
}

function Invoke-AnsiOutputScenario {
    Write-Section 'Pattern surrounded by ANSI formatting'
    Write-DemoLine "${Csi}33mANSI_ALERT${Csi}0m component=BackgroundWorker message=Formatted output was sanitized before matching"
}

function Invoke-NoAlertsScenario {
    Write-Section 'Ordinary output that should not create notifications'
    Write-DemoLine 'Loading configuration...'
    Write-DemoLine 'Processed 12 records.'
    Write-DemoLine 'DEMO_IGNORED This token has no troubleshooting rule.'
    Write-DemoLine 'Ordinary output completed.'
}

Write-DemoLine "ScriptRunner troubleshooting notification demo: $Scenario"

switch ($Scenario) {
    'Burst' { Invoke-BurstScenario }
    'Sequence' { Invoke-SequenceScenario }
    'Severities' { Invoke-SeverityScenario }
    'CapturedValues' { Invoke-CapturedValuesScenario }
    'RepeatedMatches' { Invoke-RepeatedMatchesScenario }
    'AnsiOutput' { Invoke-AnsiOutputScenario }
    'NoAlerts' { Invoke-NoAlertsScenario }
    'All' {
        Invoke-BurstScenario
        Invoke-SequenceScenario
        Invoke-SeverityScenario
        Invoke-CapturedValuesScenario
        Invoke-RepeatedMatchesScenario
        Invoke-AnsiOutputScenario
        Invoke-NoAlertsScenario
    }
}

Write-DemoLine ''
Write-DemoLine 'Demo complete. Dismiss notifications individually or resize the notification rail.'

