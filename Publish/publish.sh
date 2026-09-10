#!/bin/sh
set -eu

case ${1:-osx-x64} in
    osx-x64) targets=osx-x64 ;;
    osx-arm64) targets=osx-arm64 ;;
    all) targets='osx-x64 osx-arm64' ;;
    *) echo 'Usage: sh Publish/publish.sh [osx-x64|osx-arm64|all]' >&2; exit 2 ;;
esac
if [ "$(uname -s)" != Darwin ]; then
    echo 'macOS packaging requires macOS, .NET 10, Xcode Command Line Tools and Python 3.' >&2
    exit 1
fi
for tool in dotnet xcrun python3 codesign ditto hdiutil; do
    command -v "$tool" >/dev/null || { echo "Missing build tool: $tool" >&2; exit 1; }
done
case ${MACOS_LEGACY_PKG:-0} in
    0) ;;
    1) for tool in fileicon packagesbuild; do command -v "$tool" >/dev/null || { echo "Missing optional installer tool: $tool" >&2; exit 1; }; done ;;
    *) echo 'MACOS_LEGACY_PKG must be 0 or 1.' >&2; exit 2 ;;
esac
repo=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repo"
mkdir -p "$repo/Build" "$repo/Dist" "$repo/artifacts"
work_dir=$(mktemp -d "$repo/artifacts/mac-package.XXXXXX")
project="$work_dir/Apollo.pkgproj"
trap 'rm -f "$project"; rmdir "$work_dir"' EXIT HUP INT TERM

for rid in $targets; do
    case $rid in
        osx-x64) asset=Apollo-Mac; arch=x86_64 ;;
        osx-arm64) asset=Apollo-Mac-arm64; arch=arm64 ;;
    esac
    echo "Publishing $rid..."
    app="$repo/Apollo/bin/Release/net10.0/$rid/publish"
    updater="$repo/ApolloUpdate/bin/Release/net10.0/$rid/publish"
    rm -rf "$app" "$updater"
    dotnet publish Apollo/Apollo.csproj --self-contained true -r "$rid" -c Release
    dotnet publish ApolloUpdate/ApolloUpdate.csproj --self-contained true -r "$rid" -c Release
    # Catch mixed-architecture app hosts and native MIDI libraries before packaging.
    xcrun lipo "$app/Apollo" -verify_arch "$arch"
    xcrun lipo "$app/librtmidi.dylib" -verify_arch "$arch"
    xcrun lipo "$updater/ApolloUpdate" -verify_arch "$arch"

    # The validated RID limits cleanup to this target; the other architecture remains intact.
    stage="$repo/Build/$rid"
    rm -rf "$stage"
    mkdir -p "$stage/Apollo" "$stage/Update" "$stage/M4L"
    cp -R "$app/." "$stage/Apollo/"
    cp -R "$updater/." "$stage/Update/"
    cp M4L/*.amxd "$stage/M4L/"
    if [ "${MACOS_LEGACY_PKG:-0}" = 1 ]; then
        # Finder resource forks belong only on the legacy copies, not signed code.
        fileicon set "$stage/Apollo/Apollo" Apollo/icon.ico
        fileicon set "$stage/Update/ApolloUpdate" ApolloUpdate/icon.ico
    fi

    # Retain the legacy archive format for already-installed loose executables.
    rm -f "$repo/Dist/$asset.zip"
    ditto -c -k --sequesterRsrc --rsrc --keepParent "$stage" "$repo/Dist/$asset.zip"
    if [ "${MACOS_LEGACY_PKG:-0}" = 1 ]; then
        python3 Publish/prepare-mac-package.py "$rid" "$project"
        rm -f "$repo/Dist/$asset.pkg"
        packagesbuild "$project"
    fi

    bundle_stage="$repo/Build/$rid-app"
    rm -rf "$bundle_stage"
    mkdir -p "$bundle_stage"
    bundle="$bundle_stage/Apollo Studio.app"
    python3 Publish/macos.py "$rid" "$app" "$updater" "$bundle" --sign
    rm -f "$repo/Dist/$asset-app.zip" "$repo/Dist/$asset.dmg"
    ditto -c -k --sequesterRsrc --rsrc --keepParent "$bundle" "$repo/Dist/$asset-app.zip"
    ln -s /Applications "$bundle_stage/Applications"
    hdiutil create -volname 'Apollo Studio' -srcfolder "$bundle_stage" -format UDZO -ov "$repo/Dist/$asset.dmg"
    echo "Created Dist/$asset.dmg, Dist/$asset-app.zip and legacy Dist/$asset.zip"
done
