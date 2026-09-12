# Unicode installation-path launch investigation

September 13, 2026, MacBook Air M3, macOS 15.2, APFS. The earlier `ž` installation failure is now isolated to a **Launch Services path-normalization/registration interaction**, also reproducible without Apollo, .NET or Avalonia. The precise macOS implementation defect and behavior on other OS versions remain unverified. This is not a blanket failure of Unicode paths.

## Observed results

All launches below were initiated by double-clicking the app in Finder. Apollo's exact process path was checked before acquiring it through desktop automation, which can otherwise launch a different copy and mask a failure.

| Control | Result |
| --- | --- |
| Minimal signed native Foundation bundle, ASCII parent | Started; wrote its bundle path and PID. |
| Same native program, fresh composed `ž` parent (`U+017E`, NFC), unique bundle ID | Finder error; no process marker. RunningBoard reported invalid `LSApplicationProxy`, `NSPOSIXErrorDomain` 22 and Launch Services `-10810`. |
| Native program, decomposed `z` + caron parent (`U+007A U+030C`, NFD) | Started; exact path recorded. |
| Native program, `漢字` parent | Started; exact path recorded. |
| Force-register failing NFC native bundle using an NFD-spelled path argument | Registration returned 0; subsequent Finder launch still failed. |
| Rename only failing native control's directory entry to NFD, through an intermediate name | Same bundle/ID and directory inode; all file hashes unchanged. Finder launch then succeeded. |
| Rename only the previously failing Apollo validation directory to NFD | All 290 file hashes unchanged; signature still valid. Finder started PID 41480 at the intended path; opening a saved project showed BPM 142. |
| Final production build, real Apollo bundle ID, isolated profile in NFD parent | Finder cold launch started PID 42199 at the intended path; saved project opened at BPM 121. |
| Actual signed bundle update at that NFD installation | Replacement PID 42457 started at the exact intended path, opened the saved project at BPM 121 and quit normally. Both replacement and retained previous bundle signatures verified; project/custom connector hashes were preserved and payload/archive staging was removed. |
| Previously successful production-ID installation renamed back to NFC | Launched at the intended path. Prior launch/registration state affects the outcome; this does not establish a fresh NFC installation works. |
| Fresh NFC copy with the production ID while other validation copies were registered | Finder started an older validation copy elsewhere (PID 42539), not the requested installation. This is not a successful NFC installation launch. |

The NFD production copy retained the final normal app/updater entry points and assemblies. Only its isolated-profile runtime configuration and renewed ad-hoc signature differ from the normal build. The local update payload additionally has a resource marker to prove replacement. These are disposable fixtures, not distributable artifacts. The updater's production extraction, validation, helper, swap and Launch Services relaunch were used; the download was a locally prepared signed ZIP.

The first four native controls have distinct bundle IDs. The rename comparison preserves the failing control's ID, inode, bundle bytes and signature. Together they rule out an Apollo/.NET-specific startup defect for the reproduced failure. The successful rename-back and wrong-copy launch show why normalization alone is not a complete description of Launch Services' state-dependent behavior.

No production code workaround was added: Apollo cannot repair a launch that fails before its process starts, and an app must not silently rename its user's installation parents. ASCII installation paths remain the simplest qualified choice on this machine. NFD installations now have direct Finder and updater evidence; arbitrary NFC paths, a clean single-install environment, other macOS versions, browser quarantine and actual App Translocation still require validation. No Launch Services database reset or security-policy changes were made.

## Reproduce the native control

From the repository root on macOS with Xcode command-line tools, use a fresh output directory whose ancestors are ASCII, to isolate each final directory component:

```sh
python3 Tests/Packaging/prepare_unicode_launch.py artifacts/unicode-launch-control
```

The script compiles `UnicodeLaunchProbe.m`, creates four ad-hoc signed bundles and verifies their signatures. It does not launch or register them. Each bundle has a fresh ID; `cases.json` records its exact path, Unicode code points and executable hash. Existing output directories are refused.

In Finder, enter each case directory and double-click `Launch Probe.app`. The process exits immediately after writing `launch-<pid>.json` beside its bundle. A marker with the exact expected bundle path proves native entry was reached; absence alone is not a diagnosis, so also inspect any Finder alert and scoped Launch Services/RunningBoard logs. Do not acquire a control app through an automation API that implicitly launches it before recording the result.

For the rename control, rename **only the disposable NFC case directory** through a distinct intermediate name to its NFD spelling. APFS considers the two spellings equivalent for lookup, so an intermediate rename is needed to reliably change the stored directory entry. Compare directory listings/code points and hashes before/after, then repeat the Finder launch. Do not apply this experiment to user installation directories.

Unregister just the exact disposable app paths with `lsregister -u` after testing, including their current renamed spelling. Do not use database-wide reset/delete flags. Preserve the markers, manifest and logs with the OS/version information.

## Local evidence

Ignored evidence is under `artifacts/unicode-launch-20260913/`: native source and marker JSON, `native-control-launch.log`, `register-nfd-spelling.json`, `rename-evidence.json`, exact Apollo process JSON, production normalization logs, signed updater setup/run logs and preservation hashes. The `repro/` directory verifies the checked-in preparer compiles/signs all four controls, refuses overwriting evidence and produces a valid exact-path marker from an actual Finder launch.

The previously failing `artifacts/mac-followup/Finder ž` fixture now has an NFD directory entry; its original NFC spelling still resolves on APFS. The evidence records both forms. Test apps were closed and the task's temporary Finder window was closed after validation.
