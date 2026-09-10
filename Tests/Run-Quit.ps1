param(
    [ValidateSet('Native', 'Headless')][string]$Mode = 'Headless',
    [string]$Dotnet = 'dotnet',
    [ValidateSet('splash', 'saved', 'discard', 'save', 'track-only', 'picker-cancel', 'save-error', 'pending-close')]
    [string[]]$Case = @('splash', 'saved', 'discard', 'save', 'track-only', 'picker-cancel', 'save-error', 'pending-close'),
    [string]$Output = (Join-Path $PSScriptRoot ('..\artifacts\quit\' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss')))
)
$ErrorActionPreference = 'Stop'
$Dotnet = (Get-Command $Dotnet -CommandType Application).Source
$Output = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $Output) { throw 'Use a fresh output directory.' }
New-Item -ItemType Directory -Path $Output | Out-Null
$buildLog = Join-Path $Output 'build.log'
if ($Mode -eq 'Native') {
    $project = Join-Path $PSScriptRoot '..\Apollo\Apollo.csproj'
    $appOutput = Join-Path $Output 'app'
    $targets = Join-Path $PSScriptRoot 'Scenarios.targets'
    & $Dotnet build $project -c Release --no-restore --nologo -v:q "-p:CustomAfterMicrosoftCommonTargets=$targets" '-p:StartupObject=Apollo.Tests.Scenarios' -o $appOutput *> $buildLog
    $assembly = Join-Path $appOutput 'Apollo.dll'
} else {
    & $Dotnet build (Join-Path $PSScriptRoot 'Apollo.Scenarios.csproj') -c Release --no-restore --nologo -v:q *> $buildLog
    $assembly = Join-Path $PSScriptRoot 'bin\Release\net10.0\Apollo.Scenarios.dll'
}
if ($LASTEXITCODE -ne 0) { Get-Content $buildLog; throw 'Quit scenario build failed.' }
$oldCase = $env:APOLLO_TEST_QUIT_CASE
$oldTheme = $env:APOLLO_TEST_THEME
try {
    $env:APOLLO_TEST_THEME = 'Dark'
    foreach ($quitCase in $Case) {
        $env:APOLLO_TEST_QUIT_CASE = $quitCase
        $runOutput = Join-Path $Output $quitCase
        New-Item -ItemType Directory -Path $runOutput | Out-Null
        $process = Start-Process -FilePath $Dotnet -ArgumentList @(('"' + $assembly + '"'), ('"' + $runOutput + '"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runOutput 'stdout.log') -RedirectStandardError (Join-Path $runOutput 'stderr.log')
        if (!$process.WaitForExit(20000)) {
            Stop-Process -Id $process.Id
            Get-Content (Join-Path $runOutput 'stdout.log'),(Join-Path $runOutput 'stderr.log')
            throw "Quit scenario timed out: $quitCase"
        }
        $resultsPath = Join-Path $runOutput 'results.json'
        if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $resultsPath)) {
            Get-Content (Join-Path $runOutput 'stdout.log'),(Join-Path $runOutput 'stderr.log')
            throw "Quit scenario failed: $quitCase (exit $($process.ExitCode))"
        }
        $results = @(Get-Content -LiteralPath $resultsPath -Raw | ConvertFrom-Json)
        if ($results | Where-Object { !$_.passed }) { throw "Failed assertions in $resultsPath" }
        Write-Output "PASS $Mode $quitCase ($($results.Count) checks)"
    }
} finally {
    $env:APOLLO_TEST_QUIT_CASE = $oldCase
    $env:APOLLO_TEST_THEME = $oldTheme
}
Write-Output "Results: $Output"
