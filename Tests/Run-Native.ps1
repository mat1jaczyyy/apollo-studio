param(
    [string]$Project = (Join-Path $PSScriptRoot '..\Apollo\Apollo.csproj'),
    [string]$Dotnet = 'dotnet',
    [switch]$Legacy,
    [ValidateSet('Dark', 'Light')][string]$Theme = 'Dark',
    [switch]$Software,
    [string]$Output = (Join-Path $PSScriptRoot ('..\artifacts\scenarios\' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss')))
)
$ErrorActionPreference = 'Stop'
$Dotnet = (Get-Command $Dotnet -CommandType Application).Source
$Project = (Resolve-Path -LiteralPath $Project).Path
$Output = [IO.Path]::GetFullPath($Output)
$appOutput = Join-Path $Output 'app'
$runOutput = Join-Path $Output 'run'
New-Item -ItemType Directory -Force -Path $appOutput,$runOutput | Out-Null
$targets = Join-Path $PSScriptRoot 'Scenarios.targets'
Push-Location (Split-Path -Parent $Project)
try {
    & $Dotnet build $Project -c Release --no-restore --nologo -v:q "-p:CustomAfterMicrosoftCommonTargets=$targets" '-p:StartupObject=Apollo.Tests.Scenarios' "-p:TestLegacy=$($Legacy.IsPresent.ToString().ToLowerInvariant())" -o $appOutput *> (Join-Path $Output 'build.log')
    if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $Output 'build.log'); throw 'Scenario build failed.' }
} finally { Pop-Location }
$oldTheme = $env:APOLLO_TEST_THEME
$oldSoftware = $env:APOLLO_TEST_SOFTWARE
try {
    $env:APOLLO_TEST_THEME = $Theme
    $env:APOLLO_TEST_SOFTWARE = [int]$Software.IsPresent
    $process = Start-Process -FilePath $Dotnet -ArgumentList @(('"' + (Join-Path $appOutput 'Apollo.dll') + '"'), ('"' + $runOutput + '"')) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runOutput 'stdout.log') -RedirectStandardError (Join-Path $runOutput 'stderr.log')
} finally {
    $env:APOLLO_TEST_THEME = $oldTheme
    $env:APOLLO_TEST_SOFTWARE = $oldSoftware
}
if (!$process.WaitForExit(45000)) {
    Stop-Process -Id $process.Id
    throw "Scenario timed out. Logs: $runOutput"
}
Get-Content (Join-Path $runOutput 'stdout.log'),(Join-Path $runOutput 'stderr.log')
if ($process.ExitCode -ne 0) { throw "Scenario failed ($($process.ExitCode)). Results: $runOutput" }
Write-Output "Results: $runOutput"
