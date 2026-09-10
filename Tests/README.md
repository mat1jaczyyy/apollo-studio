# Migration regression scenarios

These command-line scenarios close the testing loop without desktop automation. Native runs build a separate executable with a test entry point, then invoke Apollo's real startup and desktop lifetime. The same source runs against the original Windows master and the migrated branch. Headless runs use Avalonia's input driver and real Skia rendering.

All runs use a process-local test profile beneath their output directory. Updates, Discord presence, backups, and autosave are disabled. MIDI scanning stops once startup completes. Run suites sequentially, with normal Apollo instances closed, because Apollo's single-instance socket is intentionally preserved. Use a fresh output directory each time.

## Requirements

- .NET SDK pinned by `global.json` (10.0.401), and either Python 3 for the portable runner or PowerShell for the original Windows runner. macOS ARM builds also require Xcode Command Line Tools.
- For master comparisons: a separate checkout/archive of commit `fca034e0bad8430bc3802fa01530b404627f23e0`, .NET SDK 5.0.102 and runtime 5.0.2, and its historical NuGet packages. The original Avalonia CI build may require an existing package cache; the migrated app restores entirely from nuget.org.
- These are integration executables, not `dotnet test` projects. A nonzero process exit or missing `results.json` means failure.

## Portable runner and Mac desktop validation

From the repository root, with the pinned SDK on PATH:

```sh
python3 Tests/run-scenarios.py --output artifacts/headless
python3 Tests/run-scenarios.py --mode native --output artifacts/native
python3 Tests/run-scenarios.py --mode native --theme Light --software --output artifacts/native-light
python3 Tests/run-scenarios.py --suite quit --output artifacts/quit-headless
python3 Tests/run-scenarios.py --mode native --suite quit --output artifacts/quit-native
```

Pass `--dotnet /absolute/path/to/dotnet` for a portable SDK. Every output directory must be fresh. The runner builds once and runs each child sequentially, with a 60-second per-process timeout. Native mode needs a desktop; both modes need Apollo's local single-instance socket, and Mac MIDI checks need CoreMIDI access. A sandbox denying these services is not an application failure.

The migrated harness sets the `Apollo.UserPath` AppContext value before Apollo initializes and checks that the resulting profile is isolated. It does not change HOME. The Windows legacy harness retains its original USERPROFILE redirection. Mac shortcut fixtures use Command and account for the native desktop's point coordinates. The input regressions also check that printable key events remain unhandled so the native Mac backend can deliver text, while window shortcuts do not act on a focused text field.

## Native Windows scenarios

From the repository root, restore the selected project first, then run:

```powershell
dotnet restore Apollo/Apollo.csproj
./Tests/Run-Native.ps1 -Output artifacts/native-dark
./Tests/Run-Native.ps1 -Theme Light -Software -Output artifacts/native-light-software
```

To use a portable SDK, pass `-Dotnet C:/path/to/dotnet.exe`. To compare master, pass `-Legacy -Project C:/path/to/master/Apollo/Apollo.csproj -Dotnet C:/path/to/dotnet5/dotnet.exe`. Restore master from its own directory so its original `global.json` applies.

The runner writes `build.log`, `run/results.json`, stdout/stderr, PNGs, presets, a saved project, and the isolated profile beneath `-Output`. It limits a run to 45 seconds and terminates only the child process it started if that deadline expires. Native tests raise control events directly; they test real Windows windows and exit behavior but do not simulate OS mouse/keyboard input.

The 70 shared checks cover startup, singleton preferences/track/pattern windows, all 24 device editors and their preset roundtrips, image import, undo/redo, explicit text focus, project/track replacement, unsaved-close cancellation and restoration, save/reopen, and final shutdown with preferences and a virtual Launchpad open. Each result records the visible windows. Run `./Tests/Compare-Runs.ps1 -Baseline artifacts/master/run -Migrated artifacts/native-dark/run` to compare scenario outcomes, window sets/visibility, and preset hashes. Window enumeration order is intentionally ignored. Inspect the paired PNGs for visual differences.

## Headless input scenarios

```powershell
dotnet build Tests/Apollo.Scenarios.csproj -c Release
dotnet Tests/bin/Release/net10.0/Apollo.Scenarios.dll artifacts/headless-dark
$env:APOLLO_TEST_THEME = 'Light'
dotnet Tests/bin/Release/net10.0/Apollo.Scenarios.dll artifacts/headless-light
Remove-Item Env:APOLLO_TEST_THEME
```

The headless suite adds actual routed input for buttons, author typing, dial dragging and double-click text entry, Enter to commit edits, track renaming, keyboard undo, clipboard preset roundtrips and invalid data, device drag/drop with undo/redo, and a 150% DPI change. Its clipboard belongs to the headless platform and does not change the desktop clipboard. `UseHeadlessDrawing=false`, Skia, and HarfBuzz are required so screenshots and layout use real rendering.

## Scope and follow-up checks

The tests do not install updates, contact Discord, drive native file-picker dialogs, or use physical MIDI hardware. Keep a short hardware/manual smoke test for native open/save cancellation and overwrite prompts, desktop clipboard and cross-application drag/drop, real Launchpads/Ableton, and the updater's installation handoff. macOS/Linux publishing from Windows checks build/package compatibility; native window behavior on those OSes still needs a run there. PNGs capture Avalonia content, not OS decorations, compositor effects, or physical screen DPI.

Do not ship the native scenario build under `artifacts`: build/publish the normal Apollo project without `CustomAfterMicrosoftCommonTargets` and without overriding `StartupObject`.
## Verified migration run (2026-09-10)

Target: Avalonia 12.1.2; .NET SDK 10.0.401 / runtime 10.0.12. Other package updates: DiscordRichPresence 1.143.0, Humanizer.Core 3.0.10, Octokit 14.0.0, Newtonsoft.Json 13.0.4, and Debug-only AvaloniaUI.DiagnosticsSupport 2.2.3. Avalonia supplies its matching SkiaSharp version; the obsolete Registry package was removed from the updater. The direct Newtonsoft.Json reference also overrides Discord's older transitive dependency.

- Master and migrated native runs: 70/70 checks each, in dark and light themes; software rendering also passed on the migrated app.
- Final comparison: matching window sets/visibility and scenario results; all 24 device preset files have identical hashes.
- Headless runs: 83/83 checks in both themes. The final light run includes the icon fixes; the final native dark run verifies those fixes on Windows.
- Visual inspection: every device editor, preferences, project, pattern, Learn, and virtual Launchpad windows. Some font rasterization and default-theme spacing differ from the old renderer.
- Debug build and self-contained app publishes for win-x64, osx-x64, and linux-x64 passed. Updater publishes for win-x64 and osx-x64 passed. Published assemblies retain their normal production entry points and include runtime 10.0.12.
- NuGet's vulnerability audit reported no vulnerable packages, including transitive dependencies. The existing updater download code still produces a WebClient obsolescence warning; its download/install flow was retained.

Local evidence is under ignored `artifacts/`: `baseline/final/run`, `migrated/final/run`, `headless/light`, `headless/run7` (dark), and `publish`. The portable SDK is in `artifacts/dotnet`; it is not installed globally or committed. These artifacts are local to the migration workspace; use the commands above to reproduce them elsewhere.

## Graceful application Quit (#214)

The `codex/214-graceful-quit` branch is based on migration commit `ec3d3029` on `avalonia-12.1.2`. It handles the desktop lifetime's `ShutdownRequested` event, used by Avalonia's macOS native Quit menu/Command+Q and native application termination callback. The initial synchronous request is cancelled while Apollo asks about unsaved work. After approval and a successful save when requested, it closes all windows, suppresses normal replacement windows, disposes the project and runs normal exit cleanup.

Ordinary window close retains its previous behavior. Repeated Quit requests share the pending decision; if an unrelated message or an ordinary close prompt is already open, Quit activates that dialog so it can be completed first. Cancel, closing the confirmation, cancelling the save picker and an unsuccessful save all leave the workspace open.

Run each mode with a fresh output directory and the normal Apollo app closed:

```powershell
./Tests/Run-Quit.ps1 -Mode Headless -Output artifacts/quit-headless
./Tests/Run-Quit.ps1 -Mode Native -Output artifacts/quit-native
```

Pass `-Dotnet C:/path/to/dotnet.exe` for a portable SDK. Restore the relevant projects first. Each mode builds once and launches a separate process per case, with a 20-second timeout, isolated profile, and per-case logs/results. `-Case saved`, for example, runs only that case.

The eight cases cover splash-only exit with auxiliary windows, a saved project with track/pattern/undo/preferences/virtual Launchpad windows, unsaved discard after cancellation and repeated requests, saving a text edit still in focus, a track-only workspace, save-picker cancellation, a locked-file save error followed by successful retry, and an existing ordinary project-close prompt. Exit checks verify zero remaining/replacement windows, project disposal, crash-backup removal, cleared crash state, and saved file contents.

The picker test substitutes a test-only storage-provider proxy; it exercises Apollo's actual asynchronous Save flow, but does not operate a native file-picker dialog. These tests invoke `TryShutdown`, the same public entry point as Avalonia's native Quit menu. Native mode here means real Windows windows, not automated macOS interaction.

Verified on 2026-09-10: all eight cases passed headlessly and with real Windows windows (93 checks per mode). Evidence is in `artifacts/quit/native-1`, `artifacts/quit/headless-2` (first seven cases), and `artifacts/quit/headless-pending-3` (the corrected existing-close fixture).

Before closing the GitHub issue, smoke-test Command+Q, menu Quit and Dock Quit on macOS, including saved/unsaved projects, track-only workspaces, minimized windows, Save/Discard/Cancel, and a cancelled native save picker. Activity Monitor's normal Quit can also be checked; Force Quit is outside graceful shutdown.
The original regression suites also passed after this fix: 70 native checks with zero scenario/window differences against master and identical hashes for all 24 presets, plus 83 headless checks. Evidence: `artifacts/quit/regression-native/run` and `artifacts/quit/regression-headless`.

A normal self-contained macOS x64 publish is available locally at `artifacts/quit/publish/osx-x64`. Its assembly entry point was verified as `Apollo.Core.Program.Main`, with no `Apollo.Tests` types included. This is a cross-published build for a macOS smoke test, not a record of running on macOS.
## Separate macOS architecture targets

Branch `codex/macos-arm64` is stacked on `codex/214-graceful-quit`. Both Apollo and ApolloUpdate declare `osx-x64` and `osx-arm64`. The Intel native MIDI binary is unchanged; ARM builds compile the pinned fork in `Native/rtmidi` on macOS, or accept a previously built thin ARM dylib through `RtMidiArm64Library` when cross-publishing. Build guards reject missing native libraries, Intel libraries and incompatible Mach-O file types.

Run the release-selection checks without starting the UI or contacting GitHub:

```powershell
$env:APOLLO_TEST_RELEASE_ASSETS = '1'
dotnet run --project Tests/Apollo.Scenarios.csproj -c Release -- artifacts/release-selection
Remove-Item Env:APOLLO_TEST_RELEASE_ASSETS
python Tests/test_mac_package.py
```

The 11 release-selection cases cover both architectures, Windows, historical versioned names, missing architecture assets, unsupported platforms/CPUs and misleading filename suffixes. Installer checks validate separate payload/output paths, host-architecture declarations, preservation of installation settings and resources, and an unchanged source template.

The `macOS builds` GitHub Actions workflow uses separate Intel and ARM runners. It publishes the app/updater, validates native Mach-O architectures, runs `Tests/native_rtmidi_smoke.py` against a virtual CoreMIDI connection, and runs the release/installer checks. It preserves executable permissions in its uploaded zip. Its native tests do not substitute for physical Launchpad tests or actual installation/update smoke tests.

Local Windows verification (2026-09-10): release selection 11/11 and installer project tests 2/2 passed. Missing ARM RtMidi and an intentionally supplied Intel dylib were both rejected. The Intel app and both updater architectures cross-published successfully; executable headers match their targets. These outputs and logs are under `artifacts/mac-targets`, with release-selection results under `artifacts/mac-arm-release-tests`.

The final Windows native regression passed all 70 checks with zero scenario/window differences against master and identical hashes for all 24 presets (`artifacts/mac-targets/windows-regression-2/run`). The final release-selection fixture also passed all 11 cases (`artifacts/mac-targets/release-selection-final`).

An ARM app build/run, actual Packages installer builds, and the new macOS CI jobs have not been executed from this Windows workspace. On a Mac, use the publishing commands in the main README and check MIDI input/output, graceful Quit, installer launch and an update staying on the same architecture.

## macOS app bundle publishing

Branch `codex/macos-app-bundle` is stacked on `codex/macos-arm64`. The default Mac artifacts now include a normal `.app`, DMG and separate `*-app.zip` update archive. Legacy archive names/layouts remain available for existing executable installations. See [publishing research, cost constraints and native test plan](../Publish/MACOS.md).

Run portable packaging checks with:

```sh
python -B Tests/test_mac_bundle.py
python -B Tests/test_mac_package.py
dotnet run --project Tests/Packaging/Packaging.csproj -c Release
```

`APOLLO_TEST_RELEASE_ASSETS=1` now runs 16 cases, including bundle/legacy format selection for both CPUs. The separate packaging runner covers 28 cases for bundle discovery/identity, CPU checks, user and legacy connector preservation, ZIP paths/symlinks, replacement, rollback, staging boundaries and shell-free updater invocation. Its file operations only use an isolated temporary directory; no real app is replaced and no Mac helper command is run by those portable tests.

Verified locally on 2026-09-10: 28 packaging checks, 16 release-selection checks, two Python bundle tests (both architecture subcases), and two legacy installer-project tests passed. Windows native regression: 70 checks, zero scenario/window differences from master, and all 24 preset files byte-identical. Headless regression: 83 checks passed. Evidence: `artifacts/bundle-packaging-tests.log`, `artifacts/bundle/release-selection`, `artifacts/bundle/native/run`, and `artifacts/bundle/headless`.

Normal Intel app and Intel/ARM updater publishes passed. An unsigned Intel bundle preview is at `artifacts/bundle/preview/Apollo Studio.app`; PE metadata verifies its entry point is `Apollo.Core.Program.Main`, with no `Apollo.Tests` types. It is a structural preview assembled on Windows, not a signed/distribution-ready artifact or a record of Mac execution.

The future MacBook Air M3 on macOS 15.2 is not connected yet. Actual ARM app execution, signing/verification, DMG creation, browser-quarantined first launch, Finder/Dock behavior, native MIDI, and bundle-update handoff remain pending. The macOS CI workflow exists on the pushed branch stack, but no macOS run was recorded when checked during this review; the new review commits remain local.
## Review regression pass (2026-09-10)

The pointer suite now drives Apollo's custom drag loop: the first-item/no-op boundary, Escape cancellation, cursor restoration, moves and copies with undo/redo, transfer between track windows, and removal of the source viewer during a drag. Horizontal resize handles are exercised at 100% and 150% scaling. Headless screen conversion ignores window positions, so the cross-window fixture uses disjoint hit regions; native monitor offsets and window stacking still need a desktop test.

Both native and headless suites additionally cover a missing compatible release asset, failed/cancelled downloads, invalid ZIP data, closing each failed update window, and rejection of unsafe archive paths. They substitute download data and never install an update. The native suite now has 86 checks (including two legacy ZIP payload checks added later in the review); use `-AllowAdditionalScenarios` with `Compare-Runs.ps1` to compare all 70 historical scenarios and their window states while also requiring the added checks to pass.

Review evidence: `artifacts/review/base-native/run` and `artifacts/review/base-headless-2`. The original first-item drop crash and Escape cancellation failure are captured in `artifacts/review-pointer-before-3.log` and `artifacts/review-pointer-cancel-before.log`.


See [the consolidated review, current validation and complete deferred-work inventory](REVIEW-AND-DEFERRED.md). The final stack passes 123 headless checks and 86 native checks; 16 release-selection checks and 28 packaging checks are separate suites.
