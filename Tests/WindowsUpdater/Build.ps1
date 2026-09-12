param(
    [Parameter(Mandatory)][string]$Output,
    [string]$Dotnet = 'dotnet',
    [string]$Cxx = 'g++'
)
$ErrorActionPreference = 'Stop'
$Output = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $Output) { throw 'Use a new output directory.' }
$Dotnet = (Get-Command $Dotnet -CommandType Application).Source
$Cxx = (Get-Command $Cxx -CommandType Application).Source
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
New-Item -ItemType Directory -Path "$Output/hook-source" -Force | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/StartupHook.cs.source" -Destination "$Output/hook-source/StartupHook.cs"
[IO.File]::WriteAllText("$Output/hook-source/Hook.csproj", '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><AssemblyName>WindowsUpdaterHook</AssemblyName></PropertyGroup></Project>')
& $Dotnet build "$Output/hook-source/Hook.csproj" -c Release -o "$Output/hook" --nologo *> "$Output/hook-build.log"
if ($LASTEXITCODE -ne 0) { throw "Hook build failed: $Output/hook-build.log" }
& $Cxx -static -O2 "$PSScriptRoot/HandleProbe.cpp" -o "$Output/handle-probe.exe" *> "$Output/handle-build.log"
if ($LASTEXITCODE -ne 0) { throw "Handle probe build failed: $Output/handle-build.log" }
& $Dotnet publish "$repo/Apollo/Apollo.csproj" -c Release -r win-x64 --self-contained true -o "$Output/Apollo" --nologo *> "$Output/app-publish.log"
if ($LASTEXITCODE -ne 0) { throw "App publish failed: $Output/app-publish.log" }
# Trimming removes framework metadata needed by an external startup hook.
# Application source and normal entry point remain unchanged.
& $Dotnet publish "$repo/ApolloUpdate/ApolloUpdate.csproj" -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -o "$Output/Update" --nologo *> "$Output/updater-publish.log"
if ($LASTEXITCODE -ne 0) { throw "Updater publish failed: $Output/updater-publish.log" }
Write-Output "Built disposable test payloads: $Output"
