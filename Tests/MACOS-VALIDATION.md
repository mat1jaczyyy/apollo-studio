# Mac validation — 2026-09-12–13

The migration and macOS follow-up stack now builds and runs locally on the MacBook Air M3 with macOS 15.2. Native testing reproduced and fixed build/signing failures and a text-entry regression. Both architecture packages verify, the automated suites pass, and native project editing, dialogs and several graceful-Quit paths work. **The Unicode installation failure is now narrowed to Launch Services normalization/registration behavior: decomposed (NFD) `ž` installations pass Finder launch and actual updater relaunch; fresh composed (NFC) controls still fail or launch another registered copy.** This pass does not establish release readiness or complete the remaining hardware/distribution matrix.

## Context and checkout

Recovered all 12 turns across both history pages of the connected Windows task **Port to latest Avalonia and .NET 10** (`01a08852-5ddd-7be0-baa1-ee3c1be5b7c1`), including its original migration, subsequent reviews, deferred checks and final history cleanup. Also read [REVIEW-AND-DEFERRED.md](REVIEW-AND-DEFERRED.md). Decisions retained:

- Avalonia 12.1.2, .NET SDK 10.0.401 / runtime 10.0.12; retain Apollo's custom project/track/window lifecycle. Whole-application Quit suppresses replacement windows, while ordinary window close retains them.
- Separate Intel and Apple Silicon app/updater targets, using Apollo's pinned custom RtMidi fork; stock RtMidi is not an ABI-compatible replacement.
- Free ad-hoc signed app bundles, DMGs and app ZIPs alongside the existing legacy archives. Whole-bundle replacement retains the previous app for recovery. Developer ID, notarization and Sparkle remain deferred; Finder document activation was implemented in the follow-up below.
- Preserve the friend's resize/drag changes and the rebased linear stack: `avalonia-12.1.2` → `codex/214-graceful-quit` → `codex/macos-arm64` → `codex/macos-app-bundle`.

Validation began in `/Users/mat1jaczyyy/Code/apollo-studio` on the existing `codex/macos-app-bundle` branch at `d082c369`. The first pass distributed commits to their owning branches and rebased the higher branches; all 355 final file hashes matched its validation snapshot. Those commits were subsequently pushed, and this follow-up started at `0c9936cc`. The user requested another coordinated commit/rebase/push pass. The Mac task owns publication; Windows supplied test-only patches from a detached worktree. No Git state was transferred between machines, and no branch was reset or merged.

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

## Unicode installation launch: normalization follow-up

Finder double-click and Open on `artifacts/mac-validation/Desktop final ž/Apollo Studio.app` produced **“The application ‘Apollo Studio’ can't be opened.”** No Apollo process started. The app has a valid deep/strict signature, valid plist, executable ARM apphost and no quarantine attribute. This was not an observed Gatekeeper approval prompt or a managed startup exception.

macOS 15.2's RunningBoard reports `NSPOSIXErrorDomain` code 22, `Unable to load a valid LSApplicationProxy`; Launch Services returns `-10810` (`kLSUnknownErr`). Apollo-specific native diagnostics are saved in `finder-launch-diagnostics.log`. An identical `ditto` copy launches from ASCII-only parent paths both with and without spaces. Its profile still points at the Unicode directory, so the control narrows the failure to bundle launch/installation-path handling rather than profile access or the runtime configuration.

On September 13, a minimal signed native Foundation app reproduced the same error in a fresh NFC `ž` directory, while ASCII, NFD `z` + caron and `漢字` directories launched. Registering the NFC path with an NFD-spelled argument did not fix it. Changing only the stored directory entry to NFD, through an intermediate rename, made both the native control and the formerly failing Apollo copy launch with unchanged bundle hashes/signatures. The exact Apollo path was verified before desktop inspection.

The final production build with the real bundle identifier also passed Finder cold launch and an actual signed updater replacement/relaunch at an NFD installation path, including responsive project UI, signed previous-bundle retention, project/custom-connector preservation and staging cleanup. A rename back to NFC after successful launch still worked, while a fresh NFC production-ID copy launched an older registered validation copy instead. Registration history therefore matters; these observations do not establish arbitrary NFC installations work or isolate the precise macOS implementation defect. See [the normalization controls, reproducer and evidence](Packaging/UNICODE-LAUNCH.md).

No production workaround renames user directories, resets Launch Services or changes security settings. A clean single-install environment, other Mac/OS versions, default `/Applications` installation, browser quarantine and Dock launch still need their own checks.

## Local updater transaction and its qualification

The production `MacBundle.PrepareUpdate` extracted and validated a signed local app ZIP and staged the real helper outside the installed app. Running that production helper replaced the isolated installation under `Update test ž/`, retained the signed previous bundle as `.Apollo Studio.app.previous`, preserved a saved project and custom connector byte-for-byte, and cleaned the staged payload/archive. Both installed and previous bundle signatures verified. Evidence: `update-prepare.log`, `update-run.log`, and the ignored `setup-update.py` / `run-update.py` fixtures.

**That original Unicode-path run proves the local file transaction and a successful relaunch-command return, not reliable Finder/Launch Services relaunch at that path.** Earlier automation saw an Apollo UI after the helper ran, but copies shared the product identifier and the exact launched process path was not established. The later Finder failure invalidates a stronger claim. The September 13 NFD-path follow-up above has separate, exact process/UI evidence and does not retroactively validate this original run.

A repeat using the production bundle identity under **`Updater ASCII spaces/` passed replacement and verified relaunch**. Before attaching desktop automation, `ps` identified the replacement's exact `Contents/MacOS/Apollo` path (PID 16896). Its splash and Preferences UI responded; dark theme, always-on-top off, Discord off, updates off, autosave off and backups off matched the seeded settings. Command+Q then exited normally. The replacement marker, signed previous bundle, unchanged project/custom-connector hashes and staging cleanup all passed. Evidence: `update-ascii-prepare.log`, `update-ascii-run.log`, `update-ascii-source/preserved-sha256.json` and the desktop observations. Configuration bytes are not compared after startup because Apollo writes session state; the settings were checked in the UI. These tests used locally prepared signed payloads, without downloading a GitHub release or changing an installed user app.

The 28 portable packaging tests exercise replacement/rollback using test fixtures. The follow-up below adds signed-bundle failure tests; actual translocation, interruption during the swap and late startup-crash recovery remain separate distribution checks.

## Finder activation and coordinated follow-up

The bundle now exports the Apollo Project document type and registers `.approj` with the Editor role. The app subscribes to Avalonia file activation before startup, queues events until the splash is ready, deduplicates pending/current-document requests, and serializes project replacement around existing messages, Quit decisions, native pickers and a pending startup crash-recovery choice. Save/Discard/Cancel applies to the current workspace. Cancel clears the batch; unreadable files leave the current project and recovery data intact. Old project, track, Pattern and Undo windows close without triggering replacement windows or terminating the app. Reopen restores a minimized project. Windows/Linux command-line opening remains available.

Evidence in this section is under ignored `artifacts/mac-followup/`:

| Check | Result | Evidence |
| --- | --- | --- |
| Activation, headless and native Mac | 34/34 each | `activation-recovery-headless/run`, `activation-recovery-native/run` |
| Editor regression, headless dark | 135/135 | `regression-final-headless/run` |
| Editor regression, native light/software | 86/86 | `native-final/regression` |
| Quit, headless dark and native light/software | 8 cases / 93 checks each | `quit-final-headless`, `native-final/quit-*` |
| Signed native Mac failure/recovery matrix | 23/23 observations | `native-update-failures-final.log` |
| Intel/ARM production publishes and package verification | Passed; no test entry points | `publish-final.log`, `package-final.log`, packaging executable output |
| Portable bundle/update checks | 28/28; Python bundle/legacy checks 2/2 each | `packaging-portable.log`, test commands |

The activation suite covers startup queuing and waiting for an existing crash-recovery choice, Unicode paths, duplicate requests, all unsaved decisions, focused text, recovery snapshots, unreadable/missing files, waiting for a picker, cancelling a picker opened during activation, old editor disposal, ordered batches and Reopen. Its picker and storage-item objects are test proxies. An early added recovery assertion incorrectly expected a filename inside the project binary; the format stores it in preferences. The corrected check verifies project contents. The native failure harness initially omitted `InvalidDataException` from its expected-exception filter; the product correctly rejected the opposite architecture. Both test-fixture failures are retained in the initial run logs.

Separate Finder desktop observations used the signed production app under `Finder activation/` with an isolated profile and unique bundle identifier:

- Double-clicking `First project ž.approj` in Finder opened it in the running app with BPM 121. Finder showed the registered Apollo document icon.
- Command+Q followed by double-clicking `Second project.approj` started a new process (PID 33467, replacing PID 27316) at the exact expected app path. The project appeared with BPM 142, without Terminal or a stray splash. Process identity was checked before attaching UI automation.
- Typing BPM 167 while the field remained focused, then opening the other file in Finder, produced the unsaved prompt. Cancel kept BPM 167. Repeating and choosing Yes opened the other project; reopening the saved file through Finder showed BPM 167.
- A fresh signed app at `Finder ž/`, with a new bundle identifier, still failed before process creation. CoreServicesUIAgent displayed the same error and RunningBoard again reported invalid `LSApplicationProxy` / Launch Services `-10810`. Evidence: `finder-unicode-launch.log`. The alert and validation Finder window were closed; the original Finder window was preserved.

The 23 native updater observations use real signed bundles and tools. Read-only parent permissions and an actual read-only DMG mount reject updates before replacement. Wrong-architecture, corrupt ZIP and tampered signed payloads preserve the installed app. Stopping before helper handoff leaves it usable. A file blocking the backup rename leaves both installed and candidate bundles intact; an injected relaunch callback exception restores the signed previous bundle. The translocation guard uses a synthetic path, and launch failure is injected at the callback: neither proves actual translocation or late process-crash recovery. No user installation was replaced.

The connected Windows task used no Computer Use or OS input automation. It passed 814 regression/Quit assertions and returned an offline GitHub fixture after finding that splash metadata fetching could contaminate the missing-asset test. Its instrumented real updater transactions confirmed success and several unsafe legacy failure behaviors; native WinMM fault injection reproduced repeat-close heap corruption. See [WINDOWS-VALIDATION.md](WINDOWS-VALIDATION.md) for exact substitutions, artifact locations, PR #486 assessment and deferred fixes. The Windows pass predates the concurrent Finder production changes.

To conserve disk space, some rebuildable earlier binaries/extracted packages were removed; logs, results, screenshots, profiles and diagnostic launch copies remain. No source, user project or profile was removed. No release artifacts, installers, Apple identities or security exceptions were published/created.

## Remaining work

- Resolve/qualify fresh NFC installation launch and selection among multiple registered copies on a clean system/other macOS versions. NFD Finder launch and actual updater relaunch now have exact process/UI evidence. Actual App Translocation, interruption during swap, helper error UI and late startup-crash/manual recovery remain untested; read-only installs, mounted-DMG rejection and the signed failure cases above now have coverage.
- Browser-download the ARM DMG, install through Finder and exercise per-app Gatekeeper approval, Finder/Dock launch and a released update. No release assets were uploaded, no Apple account/signing identity was created, and no security settings were changed in this pass.
- Complete graceful Quit through Dock and Activity Monitor, with minimized and track-only workspaces; complete native overwrite/cancel/long-path dialog coverage, focus/activation, clipboard and external file drag/drop.
- Test QWERTZ/QWERTY and text shortcuts (#466), M1 sustained rendering (#437), reliable minimize/restore (#385), mixed-DPI/offset displays and menus (#444), fullscreen and a broader Pattern Editor reopen run (#370), and native dragging/resizing under occlusion and across monitors.
- Physical Launchpad input/output, hot-plugging, LEDs, load/latency, multiple devices, #407's original reproducer, Ableton/M4L import/use and Discord reconnect require their actual integrations/hardware. Virtual MIDI and locating connector files do not establish these.
- Run the modified GitHub macOS CI matrix: the workflow triggers on pull requests/manual dispatch, not ordinary branch pushes, and no stack PR or recorded workflow run was available. Test physical Intel hardware and OS boundary versions; macOS 12/13 compatibility remains unverified. Linux and legacy `.pkg` installation remain deferred. Windows updater fixes, real elevation/handle closure, trimmed runtime transactions and physical MIDI remain deferred as qualified in the new Windows report.

See [README.md](README.md#portable-runner-and-mac-desktop-validation) for repeatable commands and [Publish/MACOS.md](../Publish/MACOS.md) for the distribution design. These results do not close existing GitHub issues without their remaining targeted checks and, where needed, a same-machine master comparison.
