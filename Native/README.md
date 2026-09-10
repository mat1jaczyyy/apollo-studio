# Native macOS MIDI dependency

Apollo's existing `Apollo/librtmidi.dylib` is Intel-only. It is retained unchanged for `osx-x64`.

`rtmidi/` contains the unmodified source files from [micdah/rtmidi commit 473ddc10aafd935d6d5d16e8876a566aa7c55a20](https://github.com/micdah/rtmidi/tree/473ddc10aafd935d6d5d16e8876a566aa7c55a20), the fork used by [RtMidi.Core](https://github.com/micdah/RtMidi.Core/blob/master/RTMIDI.md). This commit immediately precedes RtMidi.Core's February 2019 Intel binary update; Apollo's Intel library has the same Git blob hash, `c66416e49ee6838fab5b011c76acf7f97eefe095`. Its custom C ABI, including string-returning `rtmidi_get_port_name` and `rtmidi_sizeof_rtmidi_api`, differs from stock RtMidi. Substituting an arbitrary Homebrew/current upstream library is not compatible with Apollo's bindings.

On macOS, `dotnet build`/`publish` targeting `osx-arm64` automatically runs `build-rtmidi-macos.sh`. A normal build with an ARM64 macOS SDK also uses this library. Xcode Command Line Tools provide the compiler and Apple frameworks. The script builds CoreMIDI only, adds an ad-hoc signature, and writes a thin ARM64 dylib under `artifacts/native/osx-arm64`. It rebuilds when these sources or the build script change. Its native deployment floor of macOS 11 does not override the higher requirements of the application's .NET/Avalonia dependencies.

Windows/Linux cross-publishing needs a previously built library:

```sh
dotnet publish Apollo/Apollo.csproj -c Release -r osx-arm64 --self-contained true -p:RtMidiArm64Library=/absolute/path/to/librtmidi.dylib
```

The build fails if that file is missing or has an incompatible Mach-O architecture/type. It never silently copies the Intel MIDI library into an ARM build. The updater has no MIDI dependency and can be cross-published directly.

The license is included in the vendored source and copied into ARM app outputs as `RtMidi-LICENSE.txt`. Both architectures still need real MIDI-device and macOS lifecycle smoke tests.
