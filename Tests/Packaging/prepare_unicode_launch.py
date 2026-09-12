#!/usr/bin/env python3
"""Create disposable native bundles for manual Finder launch comparison on macOS."""

import argparse
import hashlib
import json
from pathlib import Path
import plistlib
import shutil
import subprocess
import sys
import unicodedata
import uuid


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output", type=Path, help="new output directory on the volume being tested")
    args = parser.parse_args()
    if sys.platform != "darwin":
        parser.error("requires macOS with Xcode command-line tools")
    output = args.output.resolve()
    # Refuse to overwrite evidence or an existing installation.
    output.mkdir(parents=True, exist_ok=False)
    binary = output / "Probe"
    subprocess.run([
        "xcrun", "clang", "-framework", "Foundation",
        str(Path(__file__).with_name("UnicodeLaunchProbe.m")), "-o", str(binary)
    ], check=True)
    run_id = uuid.uuid4().hex
    cases = {
        "ascii": "ASCII control",
        "nfc": "NFC \u017e",
        "nfd": unicodedata.normalize("NFD", "NFD \u017e"),
        "han": "\u6f22\u5b57",
    }
    manifest = []
    for case, folder in cases.items():
        app = output / folder / "Launch Probe.app"
        executable = app / "Contents/MacOS/Probe"
        executable.parent.mkdir(parents=True)
        shutil.copy2(binary, executable)
        identifier = f"com.apollostudio.validation.unicodelaunch.{run_id}.{case}"
        info = {
            "CFBundleExecutable": "Probe",
            "CFBundleIdentifier": identifier,
            "CFBundleName": "Launch Probe",
            "CFBundlePackageType": "APPL",
            "CFBundleVersion": "1",
            "CFBundleShortVersionString": "1.0",
            "LSUIElement": True,
        }
        (app / "Contents/Info.plist").write_bytes(plistlib.dumps(info))
        subprocess.run(["codesign", "--force", "--sign", "-", str(app)], check=True)
        subprocess.run(["codesign", "--verify", "--deep", "--strict", str(app)], check=True)
        manifest.append({
            "case": case, "app": str(app), "identifier": identifier,
            "folder_codepoints": [f"U+{ord(c):04X}" for c in folder],
            "executable_sha256": hashlib.sha256(executable.read_bytes()).hexdigest(),
        })
    (output / "cases.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(f"Prepared controls in {output}")
    print("Double-click each Launch Probe.app in Finder. A started process writes launch-<pid>.json beside its bundle.")
    print("No apps were launched and no Launch Services registrations or security settings were changed by this script.")


if __name__ == "__main__":
    main()
