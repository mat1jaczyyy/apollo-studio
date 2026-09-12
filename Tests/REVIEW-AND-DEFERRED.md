# Branch review and deferred work — 2026-09-10

**Mac follow-up, 2026-09-12:** Native validation ran locally on the M3 MacBook Air with macOS 15.2. Build/signing/input failures were reproduced and fixed; both architecture packages, CoreMIDI loopback, editor/Quit suites and several native UI paths passed. Finder launch at the tested Unicode installation path remains unresolved. See [MACOS-VALIDATION.md](MACOS-VALIDATION.md) for exact results, updater qualification and the remaining matrix. The historical results below remain evidence from the Windows review, not claims that every deferred item is still untouched.

This is the current consolidated handoff for the Avalonia 12.1.2 / .NET 10 migration and its three follow-up branches. It supersedes older statements that the macOS branches had not been pushed. The branches existed on origin before this review; the new review commits are local.

## Review findings and fixes

- **First-item drag crash:** dropping the first item before itself could make the adjacent-slot adjustment call CanMove with index -2. Invalid positions and empty payloads are now rejected before indexing. Reproduced with the original code.
- **Escape did not cancel a custom drag:** the new pointer-driven loop never handled Escape. Cancellation now releases capture and restores the previous cursor. Reproduced before the fix.
- **Detached drag source could crash:** cancellation during viewer disposal could call Select on an already disposed DeviceViewer. This also appeared when shutting down a failed drag test. The drag now cancels safely when its source disappears; detachment caused by a successful move waits for the Drop handler to finish. A permanent source-removal regression test passes.
- **Drag feedback/cleanup:** move and copy now return their actual effects, the move cursor is distinct from copy, original window cursors are restored, minimized windows are excluded from hit testing, and duplicate event subscriptions are avoided.
- **Updater failures escaped async UI handlers:** missing compatible assets, cancelled/failed downloads, and invalid archives now show a readable error with a Close button. ZIP paths and links are validated before clearing staging directories. The WebClient is disposed after its asynchronous download. Tests use substituted data and do not install updates.
- **Legacy macOS ZIP metadata:** selecting the first extracted directory could choose __MACOSX instead of the installation. Selection now requires exactly one directory containing Apollo, Update and M4L. Tests include a metadata directory and ambiguous payloads. The ditto exit code is checked.
- **Bundle cleanup reporting:** a permission failure deleting staging files after a successful swap/relaunch no longer reports the completed update as failed. Cleanup remains best effort; this small change was identified by code review rather than a native Mac reproduction.

The friend's drag/reorder and resize-handle fixes are retained. After this review, the follow-up task rebased the stack at the user's request, removing its merge commits while preserving every branch's file contents and the friend's commits. The Mac checkout contains the resulting linear stack: avalonia-12.1.2 → codex/214-graceful-quit → codex/macos-arm64 → codex/macos-app-bundle. The Mac validation uses that existing branch; no Git state was transferred and no branch was reset or merged.

## What was verified in this pass

- Migration branch: 121 headless checks and 84 native Windows checks before adding the two legacy-payload cases.
- Complete stack: 123 headless checks and 86 native Windows checks, including those two new cases. Native final run uses the light theme and software rendering; the migration native run used the dark theme.
- All 70 original native scenarios and window states match the saved master baseline; all 24 device preset files are byte-identical.
- All eight graceful-Quit scenarios pass in both headless and native Windows modes: 93 checks per mode.
- Packaging: 28 C# bundle/update checks, 16 release-asset selection checks, two Python bundle tests with Intel/ARM subcases, and two legacy installer-project tests.
- Normal self-contained publishes passed for the Windows app, Intel macOS app and ARM64 updater. Mach-O headers match the Mac targets. The Windows publish has Apollo.Core.Program.Main, WindowsGui subsystem and no Apollo.Tests types. Debug builds after restoring the configuration-specific diagnostics package. Missing/Intel ARM RtMidi guards passed again.
- The new pointer tests cover move/copy/undo/redo, crossing track-window boundaries, Escape, detached sources, and horizontal resize handles at 100%/150%.

Evidence is local under artifacts/review and artifacts/review-*.log. Final UI results are in artifacts/review/final-headless and artifacts/review/final-native/run; Quit results are in artifacts/review/quit-headless and artifacts/review/quit-native. Published outputs are in artifacts/review/publish. The test guide contains the commands. Compare-Runs.ps1 -AllowAdditionalScenarios preserves comparison of every historical scenario and additionally requires new checks to pass.

The headless platform does not model actual monitor coordinates or desktop occlusion. Its cross-window test uses disjoint hit regions to verify Apollo's routing and data movement. Native Windows tests create real windows and invoke controls programmatically; they do not use computer-use automation or operate native dialogs. The master comparison uses the previously captured baseline, not a new master manual session.

## Deferred native UI and OS checks

These require an interactive desktop, relevant hardware, or the unavailable Mac; automated component checks do not establish them.

| Area | Remaining checks |
| --- | --- |
| macOS graceful Quit (#214) | Command+Q, menu Quit, Dock Quit and Activity Monitor's normal Quit. Include saved/unsaved projects, multiple/track-only/minimized windows, Save/Discard/Cancel and cancelling a native save picker. Force Quit is outside graceful shutdown. |
| macOS framework bug candidates | #466 native keyboard-layout translation (QWERTZ/QWERTY and text shortcuts); #437 M1 flicker and longer editing sessions; #385 minimize/restore; #444 popup placement across offset/mixed-DPI displays; #370 maximizing/fullscreen and repeatedly closing/reopening Pattern Editor. |
| Windows compositor and dialogs | #361 black regions after minimizing during animation, with GPU and software rendering; #325 native open/save at long paths, cancellation and overwrite. The historical #325 Windows 7 report cannot be proved fixed by a Windows 10 test. |
| Pointer behavior on real desktops | Drag/drop between overlapping windows, actual z-order, external apps covering Apollo, monitor offsets/scaling, modifier changes and native cursors. The in-process hit tester only knows Apollo windows; external-app occlusion is a remaining limitation to investigate. Test custom resize edges/corners, clamping, dragging across monitors and macOS scaling; automated coverage currently exercises horizontal edges at fixed scales. |
| Native integrations | Windows/macOS/Linux open/save pickers, desktop clipboard and external file drag/drop (including Finder/Explorer), focus/activation, window centering and context-menu placement/responsiveness on draggable controls. Older 0.10 PR #394 also recorded those placement/menu concerns. |
| Linux | Actual startup, window/lifecycle behavior, rendering/display backend, dependencies, dialogs, clipboard and MIDI. Cross-publishing establishes build compatibility only. |
| OS floors | Running on older boundary OS versions has not been tested. The bundle declares macOS 12; compatibility on 12/13 remains unverified and the documented upstream support floor is 14. Earlier unsupported Windows versions are not restored by this migration. |
| Appearance | Remaining native compositor, font, theme, scaling and window-decoration differences need visual review. Rendered content PNGs cannot establish exact physical-screen parity. |

The detailed GitHub evidence and repro steps are in GITHUB-ISSUE-TRIAGE.md. These remain retest candidates, not automatically closed bugs. Where possible, reproduce master on the same machine before claiming a before/after fix.

## Deferred MIDI, Ableton and other integrations

- Compile and run the ARM64 native RtMidi library on a Mac and run the CoreMIDI virtual-loopback smoke test. Missing/wrong-architecture guards and cross-published updater binaries do not establish ARM app runtime functionality.
- Test physical Launchpad input/output, LED correctness, latency/timing, sustained high input, USB drivers, multiple applications sharing MIDI ports, hot-plugging/reconnect (#397), and more than ten devices (#480).
- Reproduce #407's garbled effects/freezing with the original project, hardware, cables and load; compare editor-open/editor-closed behavior.
- Test Ableton Live and the M4L connector on the actual platforms, including finding/importing connectors after moving to the app bundle.
- Check Discord RPC with a running Discord client, including disconnect/reconnect.
- #396's compressed preset text being altered by messaging apps is unresolved. The clipboard API migration did not change that encoding.

## Deferred macOS build, distribution and update validation

- Run the GitHub macOS build matrix on both Intel and ARM runners. A read-only check during this review found no recorded macOS workflow run and no PR for this branch stack. Existing branches are pushed; the new review commits still need publication/integration before remote CI can validate them.
- On macOS, produce and verify the ad-hoc signatures, app ZIPs and DMGs; run the native MIDI smoke test and launch both architecture targets. Windows-created bundle previews and fake Mach-O fixtures are structural tests only.
- On the unavailable M3 MacBook Air / macOS 15.2: download the ARM DMG through a browser, install it, exercise Gatekeeper's per-app approval, and verify Finder/Dock launch and updater relaunch without Terminal. Test the Intel build on an Intel Mac or under Rosetta as appropriate.
- Verify the app signature before and after ordinary use; projects, preferences, caches and editable M4L connectors must remain outside the sealed bundle.
- Exercise a real bundle-to-bundle update from paths with spaces/non-ASCII characters, including user and legacy connectors, settings preservation, writable/non-writable installation locations, mounted-DMG and App Translocation rejection, interruption, failed staging/replacement/relaunch and recovery of the previous app.
- A successful open command does not prove the new UI stays running. Late startup crashes require checking the retained previous bundle and manual recovery path.
- Test the legacy macOS executable update path and the optional Packages installer, including actual package creation/installation and its Terminal-based launch. This intentionally retains the old layout; it is not the new default app installation.
- Test the Windows updater's real handoff, permissions/UAC/elevation, file locks, installation replacement and relaunch. The legacy installer still has its existing replacement strategy; the new failure tests only cover downloading/staging and do not certify installation.
- Publish the intended release assets for each architecture/layout, then verify that installed clients discover the right artifact and remain on their architecture/layout. No release, installer deployment or end-user update was published in this review.

See ../Publish/MACOS.md for the publishing commands and platform test plan.

## Deliberately deferred implementation or out-of-scope work

- Finder document associations and activation-event handling. Registering .approj associations before implementing activation would be incomplete.
- Sparkle/native updater integration, appcast hosting and release-key management.
- Developer ID signing, notarization and App Store distribution. The chosen path remains free ad-hoc signing; no Apple membership/enrollment/payment has been performed. A free Personal Team does not supply the required distribution identity/notarization. An organizational fee waiver would require actual eligibility.
- Automatic migration/removal of legacy executable installations. Users install the new app explicitly; legacy layouts remain supported. Custom connector folders outside the old standard install location need manual migration.
- Linux auto-updating, which remains unsupported.
- Replacing the obsolete WebClient API and redesigning the legacy updater. This review handles its failure paths and disposal, but does not replace its download stack or certify its install transaction.
- Feature/refactoring requests #10, #279, #314, #426, #446, #475 and #478, plus the older 0.10 migration PR #394. They were not completed or merged as part of this migration.
- Closing upstream/project issues based on native validation, final release notes/OS support claims, merging the branch stack and publishing a release remain release follow-ups. No future testing or monitoring task has been scheduled.
