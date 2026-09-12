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

## Review regression pass (2026-09-10)

The pointer suite now drives Apollo's custom drag loop: the first-item/no-op boundary, Escape cancellation, cursor restoration, moves and copies with undo/redo, transfer between track windows, and removal of the source viewer during a drag. Horizontal resize handles are exercised at 100% and 150% scaling. Headless screen conversion ignores window positions, so the cross-window fixture uses disjoint hit regions; native monitor offsets and window stacking still need a desktop test.

Both native and headless suites additionally cover a missing compatible release asset, failed/cancelled downloads, invalid ZIP data, closing each failed update window, and rejection of unsafe archive paths. They substitute download data and never install an update. The native suite now has 84 checks; use `-AllowAdditionalScenarios` with `Compare-Runs.ps1` to compare all 70 historical scenarios and their window states while also requiring the added checks to pass.

Review evidence: `artifacts/review/base-native/run` and `artifacts/review/base-headless-2`. The original first-item drop crash and Escape cancellation failure are captured in `artifacts/review-pointer-before-3.log` and `artifacts/review-pointer-cancel-before.log`.
