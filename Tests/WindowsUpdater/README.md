# Disposable Windows updater transactions

These tests run on Windows with .NET 10 and MinGW-w64 g++ available. Close other Apollo instances first: the real app's single-instance socket is preserved. No computer use, screen capture, input injection, native dialog automation or installed app is involved.

```powershell
./Tests/WindowsUpdater/Build.ps1 -Output artifacts/windows-updater-build -Dotnet C:/path/to/dotnet.exe
./Tests/WindowsUpdater/Run.ps1 -AppPublish artifacts/windows-updater-build/Apollo -UpdaterPublish artifacts/windows-updater-build/Update -Hook artifacts/windows-updater-build/hook/WindowsUpdaterHook.dll -HandleProbe artifacts/windows-updater-build/handle-probe.exe -Output artifacts/windows-updater-runs
```

Use fresh output paths. Build.ps1 accepts -Cxx for a MinGW-w64 compiler path. Run.ps1 accepts -Case with a subset of the default cases.

## What is real and what is substituted

The app runs its normal Apollo.Core.Program entry point, starts real native Avalonia windows, and shuts down through its real lifetime. A test-only startup hook seeds offline splash metadata, disables unrelated integrations and redirects the profile before application initialization.

The staging case invokes the production private UpdateWindow.PrepareUpdate method with a locally built ZIP from inside the running app. The updater then runs its normal ApolloUpdate.Program entry point against actual files and directories; Windows file locks, deletes, directory moves, connector preservation and child-process launch are real. The relaunched production app must reach its native splash and exit cleanly. Paths include spaces and a non-ASCII character.

The following are deliberately substituted:

- No network release/download service is used, and neither elevate.exe nor UAC is invoked.
- The updater is published **untrimmed** so the isolation hook's framework dependencies remain loadable. Normal trimmed publishing is a separate build check; these transactions do not certify the trimmed runtime.
- handle64.exe is an inert native probe. It records that it was invoked, and never enumerates or closes handles. Real locks are created and released by the test controller.
- RegLoadAppKey creates a fresh private application hive. RegOverridePredefKey redirects the updater's HKCU access into it before Program.Main runs. No real registry hive or Apollo profile is copied or modified.
- Application integration settings and shutdown are controlled in process through reflection. This does not exercise a user's mouse/keyboard, the desktop compositor or native pickers.

## Cases and interpretation

- success: actual archive staging, replacement, native app relaunch, and preservation of custom/edited connectors.
- lock-release: a real FileStream prevents deletion, then the controller releases it and the updater completes.
- lock-persistent / read-only: observe the updater still retrying after seven seconds, then stop only the disposable process. Existing unbounded retry behavior is recorded; the harness itself is bounded.
- missing-executable / missing-m4l: record an unhandled post-replacement failure and the crash archive in the isolated profile.
- no-staging: updater exits successfully without changing the old installation.
- unsafe-archive / missing-apollo-folder: exercise production archive rejection and record which staging folders have already changed.

A passed observation of a known failure **does not mean the updater recovered correctly**. On the tested legacy code, persistent locks/read-only files permit partial deletion before endless retries; missing executables and missing connector directories fail after the old installation is gone. Archive path validation runs before deletion, while a structurally incomplete archive can replace Update before discovering that Apollo is missing.

Each case keeps its logs, private hive, isolated profile, payloads and JSON observations. No output cleanup is performed. Real elevation, ACL/UAC permission denial, Sysinternals handle closing, interruption rollback, release discovery and the normal trimmed end-to-end updater remain separate validation work.
