# Windows validation and PR #486 review — 2026-09-12

Tested production tree: 0c9936cceabe465946337c30d22cd981d9d0ef9e, the pushed macOS app-bundle stack. Host: Windows 10 build 19045, x64; SDK 10.0.401, runtime 10.0.12, Avalonia 12.1.2.

Work ran in a detached worktree under `D:/Code/apollo-studio/artifacts/windows-validation-20260912` on the connected Windows PC. Evidence paths below are relative to that directory. The original checkout stayed clean, and its four local branch tips remained 52e413bc / 76571149 / 5b9fac4f / d082c369. Fetch updated remote-tracking refs only. No branch was reset, merged, rebased or pushed here. No production source file changed.

## Regression and graceful Quit

The final matrix passed all **36 processes / 814 assertions**:

| Suite | Settings | Result |
|---|---|---|
| Headless | Dark | 135 / 135 |
| Headless | Light | 135 / 135 |
| Native Windows | Dark, default rendering | 86 / 86 |
| Native Windows | Light, explicit software rendering | 86 / 86 |
| Graceful Quit, eight cases | Headless Dark | 93 / 93 |
| Graceful Quit, eight cases | Headless Light | 93 / 93 |
| Graceful Quit, eight cases | Native Dark/default | 93 / 93 |
| Graceful Quit, eight cases | Native Light/software | 93 / 93 |

Both native runs match all 70 historical scenarios and window sets/visibility from the saved master baseline. All 24 device presets are byte-identical. This comparison reused the previously recorded master baseline; it was not a fresh master run.

Each process used its own test profile. Native tests create real Windows windows and invoke controls in process; headless tests exercise routed input and rendering. No Computer Use or OS input automation ran.

Evidence:
- final/regression-summary.json
- final/headless-dark, final/headless-light
- final/native-dark, final/native-light-software
- final/quit-*/{splash,saved,discard,save,track-only,picker-cancel,save-error,pending-close}
- final/master-comparison-dark.log and final/master-comparison-light.log

### Fixed test-fixture defect

Three initial regression processes failed at update-missing-compatible-asset-is-recoverable. Splash startup independently fetches release metadata even when update checks are disabled. The old fixture replaced Github.release without clearing Github.download, allowing a real cached release asset to contaminate the missing-asset test.

The new offline fixture seeds complete splash release/blog metadata and clears download before app startup. It prevents that fetch/cache race; the complete final matrix passes. Initial failure evidence remains in headless-light, native-dark and native-light-software. A separate startup-hook check also confirmed the diagnosis before editing the shared fixture.

## Windows updater: real transactions with explicit instrumentation

The main matrix covered **nine cases / 20 passing observation assertions**. Assertions include confirming failure behavior, so this does not certify the legacy updater as transactional or rollback-safe.

Real operations: production PrepareUpdate staging inside the normal Apollo app; production updater entry point; Windows FileStream locks; actual deletes, directory moves, connector copies; actual child-process launch; the relaunched normal Apollo executable reaching a native splash and exiting. Installation paths include spaces and ü.

Instrumentation:
- Startup hook redirects the process profile and maps updater HKCU to a newly created private application hive.
- The updater is untrimmed for hook compatibility. Normal self-contained app publishing and trimmed updater publishing separately passed; the original trimmed transaction was not executed.
- handle64.exe is replaced with an inert native probe. No external process handles are enumerated or closed.
- Release metadata/download bytes are local fixtures. Staging is invoked directly; elevate.exe and UAC are not invoked.

| Case | Observed behavior |
|---|---|
| Success | Production staging completed; updater replaced Apollo and relaunched its normal native entry point. Custom and edited connector files survived; new connector was added. |
| Released file lock | Updater waited while a real file lock blocked deletion, then completed replacement/relaunch after release. |
| Persistent file lock | Other old files had already been deleted. Updater was still retrying after seven seconds; harness stopped its own child. Source contains no retry timeout. |
| Read-only old file | Same partial deletion and indefinite-retry behavior; fixture restored the file attribute afterward. |
| Missing staged executable | Old installation was removed; updater failed launching Apollo.exe. Isolated crash ZIP was written; no rollback/relaunch. |
| Missing installed M4L directory | App replacement completed, then connector enumeration threw. Isolated crash ZIP was written; no relaunch or rollback. |
| No Temp staging | Updater exited 0 and left the old installation untouched. |
| Unsafe ZIP path | Production validation rejected the ZIP before clearing Update/Temp; old app/updater markers survived. |
| Missing Apollo directory entry | Staging rejected the archive after Update had already been replaced. The old app remained, but staging was partially changed. |

These failure behaviors come from the retained legacy Windows strategy, not newly introduced production changes in this pass. Relevant locations: ApolloUpdate/Program.cs:106 (delete retry), :115 (move), :121 (M4L enumeration), :134 (relaunch); Apollo/Windows/UpdateWindow.cs:157 (sequential staging).

Evidence:
- updater-transactions/results.json and per-case logs/JSON/private profiles.
- reusable-build and reusable-smoke: the new reusable build script and no-staging/lock-release cases also passed (four observations).
- isolation.json: real Apollo profile file hashes/timestamps unchanged, real Sysinternals EulaAccepted unchanged, zero remaining Apollo/ApolloUpdate processes.
- publish-app.log, publish-updater.log, publish-updater-instrumented.log.
- updater-initial / updater-isolation retain instrumentation setup failures: trimmed metadata unavailable, then an unsuitable copied template hive. The final harness instead creates an empty private application hive; neither failure is an Apollo product regression.

## PR #486 assessment

Reviewed open [PR #486](https://github.com/mat1jaczyyy/apollo-studio/pull/486), head f9833da8547fa63a6313c85b2d1aee3d011cc143. No production changes from it were integrated.

The proposed Abandon path can avoid entering a native cleanup path after device disappearance. It is understandable as an emergency workaround, but I would not integrate it unchanged:

- It is unconditional across Windows, macOS and Linux. Every real-device disconnect abandons native allocations instead of freeing them.
- Suppressing the finalizer deliberately leaks the native input/output objects and callback proxy. Reconnect cycles can accumulate these allocations.
- It does not unregister the native callback or prove callbacks have stopped. Once the old managed wrapper/delegate becomes collectible, a late callback could target an invalid managed thunk. This is a risk from inspection, not a reproduced hardware crash.
- MIDI.Disconnect's disappearance check only considers matching output names. That is not a proof that every input callback and native resource is quiescent.
- It avoids one cleanup path but leaves normal Reconnect/Dispose cleanup behavior unchanged.

### Reproduced native cleanup defect

A separate native C++ child-process fixture compiles the pinned vendored RtMidi source and substitutes WinMM MIDI input functions. It never opens a real MIDI device.

| Mode | Result |
|---|---|
| Healthy close | Exit 0; connected=0, critical-section depth=0, four unprepare calls. |
| Inject MMSYSERR_INVALHANDLE | First close throws while leaving connected=1 and critical-section depth=1; header zero has already been freed. |
| Close again after that error | Process terminates with **0xC0000374 (heap corruption)**. |

This demonstrates a source-level failure compatible with Apollo's managed Close/Dispose retry sequence. It does **not** prove the exact shipped rtmidi.dll or a particular USB driver follows this same sequence. The historical Windows DLL was retained in the migration.

Native/rtmidi/RtMidi.cpp:2570 frees header/buffer memory before completing error handling, and the error path skips connected_=false and LeaveCriticalSection. Adding only LeaveCriticalSection would leave stale pointers/state and the repeat-close failure.

A native fix should make cleanup exception-safe and idempotent, coordinate pending callbacks, and respect buffer ownership. Microsoft documents that unprepare can report an invalid handle or a still-queued buffer, and requires driver completion before freeing buffers: [WinMM buffer contract](https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midiinunprepareheader).

Additional existing interop concerns to review with such a fix: RtMidiInputDevice checks the wrapper after Input.Free; the C wrapper stores err.what() beyond the caught exception's lifetime; managed callback invocation can race event detachment. These are code-review findings, not independently reproduced by this fixture.

Evidence: winmm-build.log, winmm-{healthy,error-state,repeated-close}.{stdout,stderr}.log, Tests/WindowsMidi/WinMMFault.cpp.

## Still deferred from this bounded Windows pass

- Real elevate.exe/UAC approval and ACL-denied locations; actual Sysinternals handle enumeration/closure; a full normal-trimmed updater transaction.
- Redesign/fix of the confirmed Windows partial-deletion, retry, malformed-payload, connector-directory and rollback behavior.
- Physical Launchpad unplug/reconnect on affected Windows versions/drivers, sustained MIDI traffic, pending callbacks, device counts, Ableton and the shipped DLL's native call stack.
- A production native RtMidi fix and a controlled ABI-compatible Windows DLL build. Keep the pinned custom C ABI; substituting an arbitrary stock RtMidi build is unsafe.
- Native pickers, real mouse/keyboard and cross-application drag/drop, compositor/GPU artifacts, mixed-monitor DPI/occlusion and appearance comparisons. None were bypassed using another UI automation mechanism.
- Windows checks for subsequent Mac Finder-activation code: this report validates production tree 0c9936cc, plus the isolated offline test fixture, not later concurrent production edits.

The Windows task returned its offline fixture, updater helpers and native WinMM fixture as patches. The Mac task integrated them on the migration and ARM source branches respectively, with linear rebases of the higher branches. Finder changes are validated separately on Mac; the Windows matrix predates them.
