param(
    [Parameter(Mandatory)][string]$AppPublish,
    [Parameter(Mandatory)][string]$UpdaterPublish,
    [Parameter(Mandatory)][string]$Hook,
    [Parameter(Mandatory)][string]$HandleProbe,
    [Parameter(Mandatory)][string]$Output,
    [string[]]$Case = @('success','lock-release','lock-persistent','read-only','missing-executable','missing-m4l','no-staging','unsafe-archive','missing-apollo-folder')
)
$ErrorActionPreference = 'Stop'
if (Get-Process -Name Apollo,ApolloUpdate -ErrorAction SilentlyContinue) { throw 'Close other Apollo processes before running updater tests.' }
$AppPublish = (Resolve-Path -LiteralPath $AppPublish).Path
$UpdaterPublish = (Resolve-Path -LiteralPath $UpdaterPublish).Path
$Hook = (Resolve-Path -LiteralPath $Hook).Path
$HandleProbe = (Resolve-Path -LiteralPath $HandleProbe).Path
$Output = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $Output) { throw 'Use a new output directory.' }
New-Item -ItemType Directory -Path $Output | Out-Null
$results = [Collections.Generic.List[object]]::new()
$started = [Collections.Generic.List[Diagnostics.Process]]::new()
$oldEnvironment = @{}
foreach ($key in @('APOLLO_WINDOWS_TEST_ROOT','APOLLO_WINDOWS_TEST_MODE','APOLLO_WINDOWS_TEST_ZIP','DOTNET_STARTUP_HOOKS')) {
    $oldEnvironment[$key] = [Environment]::GetEnvironmentVariable($key)
}
function Copy-Tree($source, $destination) {
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath $source -Force | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $destination -Recurse -Force }
}
function Start-Child($root, $exe, $mode, $logName) {
    $full = [IO.Path]::GetFullPath($exe)
    if (!$full.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Child outside disposable installation' }
    $env:APOLLO_WINDOWS_TEST_ROOT = $root
    $env:APOLLO_WINDOWS_TEST_MODE = $mode
    $env:DOTNET_STARTUP_HOOKS = $Hook
    $p = Start-Process -FilePath $full -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput "$root/$logName.stdout.log" -RedirectStandardError "$root/$logName.stderr.log"
    $started.Add($p)
    return $p
}
function Wait-Child($p, [int]$milliseconds = 30000) {
    if (!$p.WaitForExit($milliseconds)) { Stop-Process -Id $p.Id; $p.WaitForExit(); throw 'Child timeout' }
    return $p.ExitCode
}
function Add-Check($root, $name, $passed, $detail) {
    $entry = [pscustomobject]@{case=(Split-Path $root -Leaf); check=$name; passed=[bool]$passed; detail=$detail; output=$root}
    $results.Add($entry)
    $results | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath "$Output/results.json"
    $entry | ConvertTo-Json -Compress -Depth 7
}
function Wait-App($root) {
    for ($i=0; $i -lt 160; $i++) {
        if (Test-Path -LiteralPath "$root/app-probe.json") {
            $proof = Get-Content -LiteralPath "$root/app-probe.json" -Raw | ConvertFrom-Json
            if ($proof.pid) {
                $child = Get-Process -Id $proof.pid -ErrorAction SilentlyContinue
                if ($child) { $started.Add($child); if (!$child.WaitForExit(10000)) { Stop-Process -Id $child.Id; throw 'App probe failed to exit' } }
            }
            return $proof
        }
        Start-Sleep -Milliseconds 100
    }
    return $null
}
function Make-Package($root, $app, $update, $kind) {
    $path = Join-Path $root 'payload.zip'
    $stream = [IO.File]::Create($path)
    try {
        $zip = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            foreach ($folder in @('Update','Apollo','M4L')) {
                if ($kind -eq 'missing-apollo-folder' -and $folder -eq 'Apollo') { continue }
                [void]$zip.CreateEntry("Apollo Studio/$folder/")
                $source = if ($folder -eq 'Update') { $update } elseif ($folder -eq 'Apollo') { $app } else { "$root/package-m4l" }
                Get-ChildItem -LiteralPath $source -Recurse -File | ForEach-Object {
                    $rel = [IO.Path]::GetRelativePath($source, $_.FullName).Replace('\','/')
                    # Explicit parent directory entries are required by the legacy extractor.
                    $parts = $rel.Split('/')
                    $parent = "Apollo Studio/$folder/"
                    for ($i=0; $i -lt $parts.Length-1; $i++) { $parent += $parts[$i]+'/'; [void]$zip.CreateEntry($parent) }
                    [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, "Apollo Studio/$folder/$rel", [IO.Compression.CompressionLevel]::Fastest)
                }
            }
            if ($kind -eq 'unsafe-archive') { [void]$zip.CreateEntry('../outside.txt') }
        } finally { $zip.Dispose() }
    } finally { $stream.Dispose() }
    return $path
}
try {
    foreach ($caseName in $Case) {
        $root = Join-Path $Output ($caseName + " space ü")
        New-Item -ItemType Directory -Path $root | Out-Null
        [IO.File]::WriteAllText("$root/.windows-updater-test", 'Disposable Apollo Windows updater installation')
        # RegLoadAppKey creates a new empty private hive; no real profile hive is copied.
        Copy-Tree $UpdaterPublish "$root/Update"
        Copy-Item -LiteralPath $HandleProbe -Destination "$root/Update/handle64.exe" -Force
        New-Item -ItemType Directory -Path "$root/Apollo","$root/M4L","$root/package-m4l" -Force | Out-Null
        [IO.File]::WriteAllText("$root/Apollo/old.txt", 'old installation')
        [IO.File]::WriteAllText("$root/Apollo/another-old.txt", 'must be removed on success')
        [IO.File]::WriteAllText("$root/M4L/custom.amxd", 'user custom')
        [IO.File]::WriteAllText("$root/M4L/existing.amxd", 'user edited')
        [IO.File]::WriteAllText("$root/package-m4l/existing.amxd", 'new vendor version')
        [IO.File]::WriteAllText("$root/package-m4l/new.amxd", 'new connector')

        if ($caseName -in @('success','unsafe-archive','missing-apollo-folder')) {
            Copy-Tree $AppPublish "$root/Apollo"
            $packageUpdate = "$root/package-update"
            Copy-Tree $UpdaterPublish $packageUpdate
            Copy-Item -LiteralPath $HandleProbe -Destination "$packageUpdate/handle64.exe" -Force
            [IO.File]::WriteAllText("$root/Update/old-updater-marker", 'prior updater')
            $zipPath = Make-Package $root $AppPublish $packageUpdate $caseName
            $env:APOLLO_WINDOWS_TEST_ZIP = $zipPath
            $stage = Start-Child $root "$root/Apollo/Apollo.exe" 'stage' 'stage'
            $stageExit = Wait-Child $stage
            $proof = if (Test-Path -LiteralPath "$root/staging.json") { Get-Content -LiteralPath "$root/staging.json" -Raw | ConvertFrom-Json } else { $null }
            if (!$proof) { throw "No staging proof: $root (exit $stageExit)" }
            Add-Check $root 'production-entry-staging' ($proof.entry -eq 'Apollo.Core.Program' -or !$proof.passed) $proof
            if ($caseName -ne 'success') {
                Add-Check $root 'archive-rejected-with-old-app-present' (!$proof.passed -and (Test-Path -LiteralPath "$root/Apollo/Apollo.exe")) $proof
                $updaterIntact = Test-Path -LiteralPath "$root/Update/old-updater-marker"
                Add-Check $root 'record-staging-boundary' $true @{oldUpdaterIntact=$updaterIntact; tempExists=(Test-Path -LiteralPath "$root/Temp")}
                continue
            }
            Add-Check $root 'staged-app-updater-connectors' ($proof.passed -and (Test-Path -LiteralPath "$root/Temp/Apollo.exe") -and (Test-Path -LiteralPath "$root/TempM4L/new.amxd")) $stageExit
        } elseif ($caseName -ne 'no-staging') {
            New-Item -ItemType Directory -Path "$root/Temp" | Out-Null
            if ($caseName -eq 'missing-executable') { [IO.File]::WriteAllText("$root/Temp/payload.txt", 'malformed staged payload') }
            else { Copy-Tree $AppPublish "$root/Temp" }
            Copy-Tree "$root/package-m4l" "$root/TempM4L"
            if ($caseName -eq 'missing-m4l') {
                # These two known files and their now-empty directory are disposable fixtures.
                Remove-Item -LiteralPath "$root/M4L/custom.amxd","$root/M4L/existing.amxd"
                Remove-Item -LiteralPath "$root/M4L"
            }
        }

        $lock = $null
        try {
            if ($caseName -in @('lock-release','lock-persistent')) { $lock = [IO.File]::Open("$root/Apollo/old.txt", [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read) }
            if ($caseName -eq 'read-only') { [IO.File]::SetAttributes("$root/Apollo/old.txt", [IO.FileAttributes]::ReadOnly) }
            $updater = Start-Child $root "$root/Update/ApolloUpdate.exe" 'apply' 'updater'
            if ($caseName -in @('lock-release','lock-persistent','read-only')) {
                Start-Sleep -Seconds 7
                Add-Check $root 'blocked-delete-keeps-updater-running' (!$updater.HasExited -and (Test-Path -LiteralPath "$root/Apollo/old.txt")) @{anotherOldFileExists=(Test-Path -LiteralPath "$root/Apollo/another-old.txt"); tempExists=(Test-Path -LiteralPath "$root/Temp")}
                if ($caseName -eq 'lock-release') { $lock.Dispose(); $lock=$null }
                else {
                    if (!$updater.HasExited) { Stop-Process -Id $updater.Id; $updater.WaitForExit() }
                    Add-Check $root 'bounded-test-stopped-unbounded-retry' $true @{elapsedObservationSeconds=7; relaunch=(Test-Path -LiteralPath "$root/app-probe.json")}
                    continue
                }
            }
            $exitCode = Wait-Child $updater
            if ($caseName -in @('success','lock-release')) {
                $app = Wait-App $root
                Add-Check $root 'replacement-and-real-native-app-relaunch' ($exitCode -eq 0 -and $app.passed -and $app.entry -eq 'Apollo.Core.Program' -and !(Test-Path -LiteralPath "$root/Apollo/old.txt") -and !(Test-Path -LiteralPath "$root/Temp")) @{exit=$exitCode; app=$app}
                Add-Check $root 'custom-and-edited-connectors-preserved' (([IO.File]::ReadAllText("$root/M4L/custom.amxd") -eq 'user custom') -and ([IO.File]::ReadAllText("$root/M4L/existing.amxd") -eq 'user edited') -and ([IO.File]::ReadAllText("$root/M4L/new.amxd") -eq 'new connector')) $null
            } elseif ($caseName -eq 'no-staging') {
                Add-Check $root 'missing-staging-is-noop' ($exitCode -eq 0 -and (Test-Path -LiteralPath "$root/Apollo/old.txt") -and !(Test-Path -LiteralPath "$root/handle-probe.json")) $exitCode
            } else {
                $crashes = @(Get-ChildItem -LiteralPath "$root/profile/.apollostudio/Crashes" -Filter '*.zip' -ErrorAction SilentlyContinue)
                Add-Check $root 'record-post-replacement-failure' ($exitCode -ne 0 -and $crashes.Count -gt 0) @{exit=$exitCode; oldInstallationSurvives=(Test-Path -LiteralPath "$root/Apollo/old.txt"); newExecutableExists=(Test-Path -LiteralPath "$root/Apollo/Apollo.exe"); crashCount=$crashes.Count; relaunch=(Test-Path -LiteralPath "$root/app-probe.json")}
            }
        } finally {
            if ($lock) { $lock.Dispose() }
            if ($caseName -eq 'read-only' -and (Test-Path -LiteralPath "$root/Apollo/old.txt")) { [IO.File]::SetAttributes("$root/Apollo/old.txt", [IO.FileAttributes]::Normal) }
        }
    }
} finally {
    foreach ($p in $started) {
        try { if (!$p.HasExited) { Stop-Process -Id $p.Id; $p.WaitForExit() } } catch {}
    }
    foreach ($key in $oldEnvironment.Keys) { [Environment]::SetEnvironmentVariable($key, $oldEnvironment[$key]) }
}
if ($results | Where-Object { !$_.passed }) { throw 'A Windows updater fixture assertion failed; inspect results.json' }
