#!/bin/sh
set -eu

if [ "$(uname -s)" != Darwin ]; then
    echo 'Building the ARM64 MIDI library requires macOS and Xcode Command Line Tools.' >&2
    exit 1
fi
native_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
output=${1:-"$native_dir/../artifacts/native/osx-arm64/librtmidi.dylib"}
mkdir -p "$(dirname -- "$output")"
rebuild=false
if [ ! -f "$output" ]; then rebuild=true; fi
for input in "$0" "$native_dir"/rtmidi/*.cpp "$native_dir"/rtmidi/*.h; do
    if [ "$input" -nt "$output" ]; then rebuild=true; fi
done
if [ "$rebuild" = true ]; then
    temporary="$output.tmp"
    trap 'rm -f "$temporary"' EXIT HUP INT TERM
    xcrun --sdk macosx clang++ -std=c++11 -O2 -arch arm64 -mmacosx-version-min=11.0 \
        -D__MACOSX_CORE__ -dynamiclib \
        "$native_dir/rtmidi/RtMidi.cpp" "$native_dir/rtmidi/rtmidi_c.cpp" \
        -framework CoreMIDI -framework CoreAudio -framework CoreFoundation -framework CoreServices \
        -Wl,-install_name,@rpath/librtmidi.dylib -o "$temporary"
    xcrun lipo -verify_arch arm64 "$temporary"
    codesign --force --sign - "$temporary"
    mv -f "$temporary" "$output"
fi
xcrun lipo -verify_arch arm64 "$output"
