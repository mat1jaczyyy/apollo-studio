# macOS app publishing

## Decision and zero-cost options

The default artifacts are a normal `Apollo Studio.app`, a drag-and-drop DMG, and an app ZIP, built separately for Intel and Apple Silicon. Launch Services starts the apphost directly, so Finder/Dock launches and bundle-update relaunches do not open Terminal. Packaging the published executable and its dependencies in a bundle follows [Avalonia's macOS deployment model](https://docs.avaloniaui.net/docs/deployment/macos).

No Apple enrollment is necessary for this build path. Python assembles the bundle and Apple's Command Line Tools ad-hoc sign it locally. Ad-hoc signatures seal the files but do not identify the publisher to Gatekeeper or provide notarization. The script signs dependencies before the updater and outer app; signing an apphost seals its containing bundle. With this layout, managed DLLs and JSON in `Contents/MacOS` also need signatures, stored in extended attributes for non-Mach-O files. The `ditto` archive flow preserves those attributes; ordinary ZIP tools that discard them can invalidate the app. Apple's [code-signing guidance](https://developer.apple.com/library/archive/technotes/tn2206/_index.html) explains why nested code must be sealed first. The completed bundle is verified with `--deep --strict`; `--deep` is not used for signing.

| Option | Cost/account | Distribution consequence |
| --- | --- | --- |
| Current ad-hoc `.app` + DMG/ZIP | Free; no account | Works with the recipient's approval for an unidentified developer. |
| Free Apple developer registration / Personal Team | Free | Useful for development; does not provide Developer ID distribution or notarization. |
| Developer ID signing + notarization | Apple Developer Program membership, normally US$99/year or local equivalent | Apple's normal route for distributing an identified, notarized Mac app. |

Apple distinguishes [free registration from program membership](https://developer.apple.com/support/compare-memberships/), and [Developer ID certificates require a program team](https://developer.apple.com/developer-id/). Its [fee waiver](https://developer.apple.com/help/account/membership/fee-waivers/) is for qualifying nonprofit, educational or government legal entities and explicitly excludes individuals, sole proprietors and single-person businesses. Merely releasing free/open-source software does not establish waiver eligibility. No enrollment, payment or certificate creation was performed for this branch.

For an Internet download on macOS 15, try opening the installed app, then use **System Settings → Privacy & Security → Open Anyway** if macOS blocks it. Apple documents this [per-app approval procedure](https://support.apple.com/en-us/102445). Approval may need repeating for a new build. No packaging/updating script disables Gatekeeper or removes quarantine attributes. Signing verification does not mean Gatekeeper approval or Apple notarization.

Alternatives researched: Avalonia's [Parcel](https://docs.avaloniaui.net/docs/deployment/macos) automates similar packaging tasks, but does not remove Apple's signing requirements. The existing Packages installer can remain an optional legacy artifact; installing an ordinary app does not need an installer. [Sparkle](https://sparkle-project.org/) is a free native updater with update signing and established installation behavior. Adopting it would require native framework integration, an appcast and release-key management; this branch instead adapts Apollo's existing GitHub updater and keeps the same release source. That is an implementation choice, not a claim that ad-hoc signatures authenticate downloaded updates.

## Building

On macOS, install a compatible version of Xcode Command Line Tools, Python 3, and the .NET SDK pinned in `global.json`. Full Xcode, an Apple account, `fileicon`, and Packages are not needed for the default artifacts.

```sh
sh Publish/publish.sh osx-arm64  # Apple Silicon
sh Publish/publish.sh osx-x64    # Intel
sh Publish/publish.sh all       # both
```

Each target produces:

| Purpose | Intel | Apple Silicon |
| --- | --- | --- |
| Drag-and-drop installation | `Apollo-Mac.dmg` | `Apollo-Mac-arm64.dmg` |
| App bundle / bundle updater | `Apollo-Mac-app.zip` | `Apollo-Mac-arm64-app.zip` |
| Legacy executable updater | `Apollo-Mac.zip` | `Apollo-Mac-arm64.zip` |

Artifacts are under `Dist/`; app bundles are under `Build/<rid>-app/`. Upload all three formats for each released architecture if both legacy and bundle installations should receive updates. Version-prefixed release names also work. A bundle never falls back to a loose-executable archive, or to the other CPU architecture.

For the old `.pkg`, use `MACOS_LEGACY_PKG=1 sh Publish/publish.sh <rid>` with `fileicon` and Packages installed. This optional installer retains the old executable/Terminal installation; the DMG is the new default. Nothing automatically removes an existing legacy installation.

Bundle assembly itself can run on Windows using `python Publish/macos.py <rid> <app-publish-dir> <updater-publish-dir> <destination.app>`. Such an output is only a structural preview. Run packaging with `--sign` on macOS before distribution, and generate the DMG there. ARM publishing still needs the native MIDI library described in [Native/README.md](../Native/README.md).

## Installation, data and updates

Copy the app to `/Applications` or a writable `~/Applications` folder, then eject the DMG. Existing users switch to the bundle by installing it from the DMG; the legacy updater continues updating its existing layout. Do not run both installations at once.

Preferences, recent projects and crash recovery keep using `~/.apollostudio`. The M4L locator copies connectors into `~/.apollostudio/M4L`, preserving existing files. It first imports any connectors from the old standard `/Applications/Apollo Studio/M4L` folder, then adds missing bundled defaults. For a legacy installation in another location, copy custom connector files into the user M4L folder yourself. Existing project files and legacy installations are left in place.

The app bundle remains sealed: editable files and update staging live outside it. The updater validates the bundle identity, processor architecture and code signature, waits for Apollo to exit, copies the replacement onto the installation volume, and swaps whole bundles. A failed rename or Launch Services command rolls back the swap. The preceding bundle is retained alongside the app as `.<installed-app-name>.previous` until the next replacement, for manual recovery if the new process later crashes. No test can infer a successful UI launch solely from `open` returning success.

Updates require a writable installation folder. The app checks this before closing and refuses in-place updates from mounted DMGs or App Translocation paths. The updater requests no administrative access. Failures produce a native alert and `~/.apollostudio/Crashes/bundle-update.log`. Failed downloads/payloads and small staged helper bundles can remain under `~/.apollostudio/Updates`; remove that folder only when no update is running.

The bundle's declared minimum is macOS 12, matching the .NET native deployment target; current upstream support starts at macOS 14. Compatibility on 12/13 is unverified. No file associations are registered yet: opening project files through Apollo continues to work, and a future Finder document integration must handle Avalonia activation events before adding an association.

## Validation on the M3 test machine

On 2026-09-12, local validation ran on the MacBook Air M3 with macOS 15.2. Both Intel and ARM app/updater targets published successfully after fixing `lipo` argument ordering and real bundle signing. Both app ZIPs retained valid deep/strict signatures after extraction; both DMGs verified and mounted read-only with the expected signed app and Applications link. ARM CoreMIDI and the Intel library under Rosetta passed virtual note loopback. The app ran on both architectures, and native editing, save/open, several graceful-Quit paths and the M4L locator were exercised with isolated profiles. See [the detailed results and remaining checks](../Tests/MACOS-VALIDATION.md).

**Known unresolved launch result:** Finder could not launch the signed validation app from the tested parent directory containing `ž`, before an Apollo process started. macOS reported an invalid `LSApplicationProxy`. An identical copy launched from ASCII-only paths, including a parent directory with spaces. This is distinct from opening project files at Unicode paths, which passed. The cause and scope remain unresolved; do not claim arbitrary Unicode installation paths are validated.

The native local updater test at an ASCII installation path containing spaces verified signed whole-bundle replacement, retention of the previous app, project/custom-connector preservation, staging cleanup and a responsive replacement UI at the exact expected process path. A successful relaunch command alone does not prove the new UI is running at the intended path; see the report's separate qualification of the Unicode-path test.

Portable checks additionally cover metadata, archive validation, architecture/release selection, legacy connectors and rollback. The CI workflow now includes headless editor/Quit checks and production-entry-point verification alongside publishing and virtual MIDI. The updated workflow has not been run remotely. Browser-download quarantine, installation/Gatekeeper approval, Dock launch, physical MIDI/Ableton, updater failure/recovery and OS-floor checks remain pending. No artifacts were released in this local validation pass.
