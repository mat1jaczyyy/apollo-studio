param(
    [Parameter(Mandatory)][string]$Baseline,
    [Parameter(Mandatory)][string]$Migrated,
    [switch]$AllowAdditionalScenarios
)
$ErrorActionPreference = 'Stop'
$Baseline = (Resolve-Path -LiteralPath $Baseline).Path
$Migrated = (Resolve-Path -LiteralPath $Migrated).Path
$before = @(Get-Content -LiteralPath (Join-Path $Baseline 'results.json') -Raw | ConvertFrom-Json)
$after = @(Get-Content -LiteralPath (Join-Path $Migrated 'results.json') -Raw | ConvertFrom-Json)
$additional = @()
if ($AllowAdditionalScenarios) {
    $additional = @($after | Where-Object { $_.scenario -notin $before.scenario })
    if ($additional | Where-Object { !$_.passed }) { throw 'An additional scenario failed.' }
    $after = @($after | Where-Object { $_.scenario -in $before.scenario })
}
$differences = @()
if ($before.Count -ne $after.Count) { $differences += 'Scenario count differs' }
for ($index = 0; $index -lt [Math]::Min($before.Count, $after.Count); $index++) {
    # Desktop lifetime window collection order differs between Avalonia versions.
    # Compare the window set and visibility, including duplicate window types.
    $before[$index].windows = @($before[$index].windows | Sort-Object type,visible)
    $after[$index].windows = @($after[$index].windows | Sort-Object type,visible)
    if (!$before[$index].passed -or !$after[$index].passed -or
        ($before[$index] | ConvertTo-Json -Depth 8 -Compress) -ne
        ($after[$index] | ConvertTo-Json -Depth 8 -Compress)) {
        $differences += $before[$index].scenario
    }
}
$presets = @(Get-ChildItem -LiteralPath $Baseline -Filter '*.apdevice')
foreach ($preset in $presets) {
    $candidate = Join-Path $Migrated $preset.Name
    if (!(Test-Path -LiteralPath $candidate) -or
        (Get-FileHash -LiteralPath $preset.FullName).Hash -ne (Get-FileHash -LiteralPath $candidate).Hash) {
        $differences += ('Preset differs: ' + $preset.Name)
    }
}
if ($presets.Count -ne @(Get-ChildItem -LiteralPath $Migrated -Filter '*.apdevice').Count) {
    $differences += 'Preset count differs'
}
[pscustomobject]@{
    Scenarios = $before.Count
    AdditionalScenarios = $additional.Count
    IdenticalPresets = $presets.Count
    Differences = $differences
} | ConvertTo-Json
if ($differences.Count) { throw 'Regression comparison failed.' }
