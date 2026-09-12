#!/usr/bin/env python3
"""Build and ad-hoc sign Apollo's app bundle using only Python and Apple's tools."""
import argparse
from pathlib import Path
import plistlib
import re
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
NAME = "Apollo Studio.app"
IDENTIFIER = "com.mat1jaczyyy.apollostudio"
ARCHITECTURES = {"osx-x64": "x86_64", "osx-arm64": "arm64"}


def version():
    source = (ROOT / "Apollo/Core/Program.cs").read_text(encoding="utf-8-sig")
    return re.search(r'Version = "Version (\d+\.\d+\.\d+)"', source).group(1)


def metadata(executable, name, identifier, build_version, architecture):
    return {
        "CFBundleExecutable": executable,
        "CFBundleName": name,
        "CFBundleDisplayName": name,
        "CFBundleIdentifier": identifier,
        "CFBundleInfoDictionaryVersion": "6.0",
        "CFBundlePackageType": "APPL",
        "CFBundleVersion": build_version,
        "CFBundleShortVersionString": build_version,
        "LSMinimumSystemVersion": "12.0",
        "LSArchitecturePriority": [architecture],
        "NSHighResolutionCapable": True,
    }


def make_bundle(app, updater, destination, rid, *, icon=None, connectors=None, build_version=None):
    app, updater, destination = Path(app), Path(updater), Path(destination)
    if not (app / "Apollo").is_file() or not (updater / "ApolloUpdate").is_file():
        raise ValueError("Publish both self-contained macOS projects before packaging.")
    architecture = ARCHITECTURES[rid]
    build_version = build_version or version()
    contents = destination / "Contents"
    contents.mkdir(parents=True, exist_ok=False)
    resources = contents / "Resources"
    resources.mkdir()
    helper = contents / "Helpers/Apollo Updater.app/Contents"
    helper.mkdir(parents=True)
    ignore = shutil.ignore_patterns("rtmidi.dll", "librtmidi.so", "elevate.exe", "handle64.exe")
    shutil.copytree(app, contents / "MacOS", ignore=ignore)
    shutil.copytree(updater, helper / "MacOS", ignore=ignore)
    (contents / "MacOS/Apollo").chmod(0o755)
    (helper / "MacOS/ApolloUpdate").chmod(0o755)
    shutil.copy2(icon or ROOT / "Assets/icon/icon.icns", resources / "Apollo.icns")
    shutil.copytree(connectors or ROOT / "M4L", resources / "M4L", ignore=lambda _, names: [n for n in names if not n.endswith(".amxd")])
    shutil.copy2(ROOT / "LICENSE", resources / "LICENSE.txt")
    shutil.copy2(ROOT / "Native/rtmidi/LICENSE.txt", resources / "RtMidi-LICENSE.txt")
    info = metadata("Apollo", "Apollo Studio", IDENTIFIER, build_version, architecture)
    info.update({"CFBundleIconFile": "Apollo.icns", "LSApplicationCategoryType": "public.app-category.music"})
    with (contents / "Info.plist").open("wb") as output:
        plistlib.dump(info, output)
    info = metadata("ApolloUpdate", "Apollo Updater", IDENTIFIER + ".updater", build_version, architecture)
    info["LSUIElement"] = True
    with (helper / "Info.plist").open("wb") as output:
        plistlib.dump(info, output)
    return destination


def sign_bundle(bundle, rid):
    bundle = Path(bundle)
    # Sign native code inside out. --deep is used for verification, never signing.
    magic = {b"\xcf\xfa\xed\xfe", b"\xfe\xed\xfa\xcf", b"\xca\xfe\xba\xbe", b"\xbe\xba\xfe\xca"}
    hosts = {bundle / "Contents/MacOS/Apollo",
             bundle / "Contents/Helpers/Apollo Updater.app/Contents/MacOS/ApolloUpdate"}
    for path in sorted(bundle.rglob("*")):
        if path.is_file():
            with path.open("rb") as source:
                native = source.read(4) in magic
            if native:
                subprocess.run(["xcrun", "lipo", str(path), "-verify_arch", ARCHITECTURES[rid]], check=True)
            # codesign treats all files directly in MacOS as nested code, including
            # managed DLLs and JSON. Non-Mach-O signatures use extended attributes,
            # preserved by our ditto archives.
            # Signing a main executable signs its enclosing bundle, so defer both
            # apphosts until every dependency (and the nested helper) is sealed.
            if path not in hosts and (native or path.suffix == ".dll" or path.parent.name == "MacOS"):
                subprocess.run(["codesign", "--force", "--sign", "-", str(path)], check=True)
    for path in (bundle / "Contents/Helpers/Apollo Updater.app", bundle):
        subprocess.run(["codesign", "--force", "--sign", "-", "--entitlements", str(ROOT / "Publish/macos-entitlements.plist"), str(path)], check=True)
    subprocess.run(["codesign", "--verify", "--deep", "--strict", str(bundle)], check=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rid", choices=ARCHITECTURES)
    parser.add_argument("app", type=Path)
    parser.add_argument("updater", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--sign", action="store_true", help="Ad-hoc sign on macOS; no account or certificate required")
    args = parser.parse_args()
    make_bundle(args.app, args.updater, args.destination, args.rid)
    if args.sign:
        sign_bundle(args.destination, args.rid)
