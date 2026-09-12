# Mac validation — 2026-09-12

The migration and macOS follow-up stack now builds and runs locally on the MacBook Air M3 with macOS 15.2. Native testing reproduced and fixed build/signing failures and a text-entry regression. Both architecture packages verify, the automated suites pass, and native project editing, dialogs and several graceful-Quit paths work. **Finder launch from the tested installation directory containing `ž` still fails before Apollo starts.** This pass does not establish release readiness or complete the remaining hardware/distribution matrix.

## Context and checkout

Recovered all 12 turns across both history pages of the connected Windows task **Port to latest Avalonia and .NET 10** (`01a08852-5ddd-7be0-baa1-ee3c1be5b7c1`), including its original migration, subsequent reviews, deferred checks and final history cleanup. Also read [REVIEW-AND-DEFERRED.md](REVIEW-AND-DEFERRED.md). Decisions retained:

- Avalonia 12.1.2, .NET SDK 10.0.401 / runtime 10.0.12; retain Apollo's custom project/track/window lifecycle. Whole-application Quit suppresses replacement windows, while ordinary window close retains them.
- Separate Intel and Apple Silicon app/updater targets, using Apollo's pinned custom RtMidi fork; stock RtMidi is not an ABI-compatible replacement.
- Free ad-hoc signed app bundles, DMGs and app ZIPs alongside the existing legacy archives. Whole-bundle replacement retains the previous app for recovery. Developer ID, notarization, Sparkle and Finder document activation remain deferred.
- Preserve the friend's resize/drag changes and the rebased linear stack: `avalonia-12.1.2` → `codex/214-graceful-quit` → `codex/macos-arm64` → `codex/macos-app-bundle`.

Validation began in `/Users/mat1jaczyyy/Code/apollo-studio` on the existing `codex/macos-app-bundle` branch at `d082c369`. At the user's subsequent request, the changes were committed to their owning branches and the higher branches were rebased upward. Before this bookkeeping update, all 355 final file hashes matched the validation snapshot after rebuilding the stack. No Git state was transferred, no branches were reset or merged, and nothing was pushed.

The pinned SDK was installed under ignored `artifacts/dotnet`; its CLI home and NuGet cache are under `artifacts/tooling`. No global SDK installation was needed. Tests and desktop copies use isolated profiles under their output directories, with updates, Discord, autosave and backups disabled. The profile override is process-local `AppContext` data; HOME is unchanged. The user's existing profile was not used for this validation.

## Reproduced failures and fixes

1. **Native build/publish architecture checks failed.** Apple's `lipo` consumed the filename as another architecture with the original argument ordering. The native build script, publisher, signing helper and CI now put the file before `-verify_arch`.
2. **Real bundle signing failed.** Signing an apphost seals its containing bundle, so it must follow its dependencies. The initial attempts reported unsigned managed DLLs, then unsigned JSON in `Contents/MacOS`. The signing helper now signs these files and native dependencies first, then the helper bundle, then the outer app. Non-Mach-O signatures use extended attributes; the existing `ditto` archive flow preserves them. `--deep` is used for verification only. Actual ZIP extraction and DMG mount checks verify the resulting signatures.
3. **Typing into Apollo text fields did nothing on the native Mac backend.** This was reproduced in the Intel app under Rosetta: key events arrived but printable text did not, while paste worked. Apollo consumed every `KeyDown`, preventing native text delivery. Text handlers now consume only their implemented Enter/Tab actions; window shortcuts return when the event originates from a text field. Project BPM/author `KeyUp` handlers were also wired incorrectly and now use `Text_KeyUp`. Native ARM typing, undo/redo and saving the focused edit pass. Twelve added assertions check the native text-delivery prerequisite; headless `KeyTextInput` alone had bypassed it.

The test harness now supports isolated Mac profiles and a portable Python runner. Shortcut fixtures use Command on Mac, and the resize fixture accounts for Mac desktop positions being points. These fixture corrections did not change the app's resize implementation. CI includes the headless editor/Quit suites and a production entry-point check; the modified workflow has not been run remotely.

## Automated and package results

Paths in this table are relative to ignored `artifacts/mac-validation/` in this checkout. They are local evidence, not checked-in fixtures or a new Windows/master comparison.

| Check | Result | Evidence |
| --- | --- | --- |
| Headless regression, dark | 135/135 | `text-fix-headless/run/results.json` |
| Headless regression, light | 135/135 | `final-headless-light/run/results.json` |
| Native regression, dark/default renderer | 86/86; before the text-handler fix | `native-dark/run/results.json` |
| Native regression, light/software renderer | 86/86; final input code | `final-native-light-software/run/results.json` |
| Graceful Quit, native Mac | 8 cases, 93/93 | `final-quit-native/*/results.json` |
| Graceful Quit, headless | 8 cases, 93/93 | `final-quit-headless/*/results.json` |
| Release-asset selection | 16/16 | `release-selection/results.json` |
| C# bundle/update checks | 28/28 | `packaging-tests.log` |
| Python bundle / legacy installer-project tests | 2/2 each | `Tests/test_mac_bundle.py`, `Tests/test_mac_package.py` |
| ARM native RtMidi compile | Passed with Xcode Command Line Tools | `rtmidi-build.log` |
| CoreMIDI C ABI, discovery, callback and note loopback | Passed for ARM and Intel under Rosetta | `Tests/native_rtmidi_smoke.py`; native tool output |
| Self-contained app + updater publishes | Passed for `osx-arm64` and `osx-x64` | `final-publish.log`, `Build/<rid>-app/` |
| Production entry points | Both use `Apollo.Core.Program.Main`, with no `Apollo.Tests` types | Packaging executable `--verify-publish`; native tool output |
| Signed app ZIP extraction + read-only DMG mount | Both architectures passed; `/Applications` links correct | `final-package-roundtrips.log` |

The final `Dist/` contains six assets: Intel/ARM DMGs, app ZIPs and legacy ZIPs. Production Build/Dist runtime configurations have no validation profile override. The native suites create real Mac windows and invoke controls programmatically; the headless suites exercise routed pointer/keyboard input with rendered content. Neither replaces the separate native desktop observations below. Native single-instance sockets, CoreMIDI and DMG services required execution outside the tool sandbox; the initial sandbox CoreMIDI denial was not an application defect.

## Native desktop observations

Desktop copies retain the production entry point and assemblies, with only an isolated profile runtime configuration and renewed ad-hoc signature. Finder controls additionally used a distinct validation bundle identifier to distinguish copies. These modified copies must not be distributed.

| Action | Observed result and limits |
| --- | --- |
| Intel app under Rosetta; ARM app | Both ran and displayed usable project/editor UI. Intel is not a physical Intel Mac test. |
| Native typing and shortcuts | ARM BPM `173` accepted; Command+Z restored `150`, Command+Shift+Z restored `173`. Author text with ASCII, spaces and `+` worked. The automation's attempt to type `ž` omitted that character, so Unicode/dead-key/IME and alternate keyboard-layout typing remain unverified. |
| Native Save and Open | Saved and reopened `Manual files ž spaces/Native Mac.approj` with BPM/author preserved. Folder navigation used the native picker's accessibility value setter. This verifies Unicode file paths, not typing that character. |
| Unsaved Command+Q | Cancel preserved the workspace; Yes opened the native Save picker and cancelling it preserved the workspace; No discarded the test project and exited. |
| Save-on-Quit with focused text | Typed BPM `179`, then Command+Q → Yes → native Save. Created `Manual files ž spaces/Native Quit Save.approj` and exited. Reopening showed BPM `179`. |
| Saved project/track/Pattern Editor Quit | Native application-menu Quit exited all windows. Command+Q also exited the saved project in the Finder launch control. |
| Ordinary project close | Command+Shift+W returned a saved project to the splash screen. |
| Maximized Pattern Editor | Maximized, closed with Command+W and reopened twice without a crash. Native fullscreen and a same-Mac master reproduction were not tested; this does not close #370. |
| Preferences and M4L locator | Dark theme, disabled Discord and disabled always-on-top settings remained visible after the local update exercise. Locator opened the isolated user M4L folder in Finder with the bundled connector and an existing custom connector present. |
| Signature after use | Deep/strict verification still passed; project/custom-connector hashes remained unchanged by the update. |
| Finder at ASCII installation paths | The same signed ARM bundle launched from `Desktop-ASCII` and `Desktop ASCII spaces`, without Terminal. The exact process path was checked before attaching desktop automation. The latter was responsive and reopened the saved Quit project. |

Minimize/raise was exercised but the available observations did not establish the complete minimize/restore matrix. Native Add Device interactions were inconclusive; no native pointer/menu success is inferred from those attempts. Dock Quit, Activity Monitor Quit, external drag/drop, clipboard interoperability and monitor-offset behavior remain open.

## Confirmed Finder failure at the tested Unicode installation path

Finder double-click and Open on `artifacts/mac-validation/Desktop final ž/Apollo Studio.app` produced **“The application ‘Apollo Studio’ can't be opened.”** No Apollo process started. The app has a valid deep/strict signature, valid plist, executable ARM apphost and no quarantine attribute. This was not an observed Gatekeeper approval prompt or a managed startup exception.

macOS 15.2's RunningBoard reports `NSPOSIXErrorDomain` code 22, `Unable to load a valid LSApplicationProxy`; Launch Services returns `-10810` (`kLSUnknownErr`). Apollo-specific native diagnostics are saved in `finder-launch-diagnostics.log`. An identical `ditto` copy launches from ASCII-only parent paths both with and without spaces. Its profile still points at the Unicode directory, so the control narrows the failure to bundle launch/installation-path handling rather than profile access or the runtime configuration.

The cause is unresolved. This evidence does not prove that all Unicode paths fail, identify an upstream fix, or establish behavior on another Mac/OS. Do not reset the user's Launch Services database or weaken Gatekeeper to work around it. Before a release claims support for such installations, reproduce with the real product identifier in a clean installation, investigate path normalization/registration, and verify the exact launched process path. Default `/Applications` installation, browser quarantine and Dock launch still need their own checks.

## Local updater transaction and its qualification

The production `MacBundle.PrepareUpdate` extracted and validated a signed local app ZIP and staged the real helper outside the installed app. Running that production helper replaced the isolated installation under `Update test ž/`, retained the signed previous bundle as `.Apollo Studio.app.previous`, preserved a saved project and custom connector byte-for-byte, and cleaned the staged payload/archive. Both installed and previous bundle signatures verified. Evidence: `update-prepare.log`, `update-run.log`, and the ignored `setup-update.py` / `run-update.py` fixtures.

**That Unicode-path run proves the local file transaction and a successful relaunch-command return, not reliable Finder/Launch Services relaunch at that path.** Earlier automation saw an Apollo UI after the helper ran, but copies shared the product identifier and the exact launched process path was not established. The later Finder failure invalidates a stronger claim.

A repeat using the production bundle identity under **`Updater ASCII spaces/` passed replacement and verified relaunch**. Before attaching desktop automation, `ps` identified the replacement's exact `Contents/MacOS/Apollo` path (PID 16896). Its splash and Preferences UI responded; dark theme, always-on-top off, Discord off, updates off, autosave off and backups off matched the seeded settings. Command+Q then exited normally. The replacement marker, signed previous bundle, unchanged project/custom-connector hashes and staging cleanup all passed. Evidence: `update-ascii-prepare.log`, `update-ascii-run.log`, `update-ascii-source/preserved-sha256.json` and the desktop observations. Configuration bytes are not compared after startup because Apollo writes session state; the settings were checked in the UI. These tests used locally prepared signed payloads, without downloading a GitHub release or changing an installed user app.

The 28 portable packaging tests exercise replacement/rollback using test fixtures. Actual read-only installation, mounted-DMG/App Translocation rejection, interruption, permission failures, relaunch failure and recovery on a Mac remain a separate distribution matrix.

## Remaining work

- Resolve/qualify the Unicode installation launch failure and repeat the updater at that path with exact process evidence. Test failure/recovery cases with signed bundles, including non-writable installs, mounted DMGs and App Translocation.
- Browser-download the ARM DMG, install through Finder and exercise per-app Gatekeeper approval, Finder/Dock launch and a released update. No release assets were uploaded, no Apple account/signing identity was created, and no security settings were changed in this pass.
- Complete graceful Quit through Dock and Activity Monitor, with minimized and track-only workspaces; complete native overwrite/cancel/long-path dialog coverage, focus/activation, clipboard and external file drag/drop.
- Test QWERTZ/QWERTY and text shortcuts (#466), M1 sustained rendering (#437), reliable minimize/restore (#385), mixed-DPI/offset displays and menus (#444), fullscreen and a broader Pattern Editor reopen run (#370), and native dragging/resizing under occlusion and across monitors.
- Physical Launchpad input/output, hot-plugging, LEDs, load/latency, multiple devices, #407's original reproducer, Ableton/M4L import/use and Discord reconnect require their actual integrations/hardware. Virtual MIDI and locating connector files do not establish these.
- Run the modified GitHub macOS CI matrix after the changes are published. Test physical Intel hardware and OS boundary versions; macOS 12/13 compatibility remains unverified. Linux, legacy `.pkg` installation and the Windows updater transaction remain deferred as recorded in the Windows handoff.

See [README.md](README.md#portable-runner-and-mac-desktop-validation) for repeatable commands and [Publish/MACOS.md](../Publish/MACOS.md) for the distribution design. These results do not close existing GitHub issues without their remaining targeted checks and, where needed, a same-machine master comparison.
