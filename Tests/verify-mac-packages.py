"""Verify actual signed ZIP/DMG payloads on macOS, including archive metadata."""
import argparse
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def run(*args):
    subprocess.run(args, check=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output", type=Path, help="Fresh directory for extracted payloads and mount points")
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    for asset, architecture in (("Apollo-Mac", "x86_64"), ("Apollo-Mac-arm64", "arm64")):
        extracted = output / asset
        run("ditto", "-x", "-k", "--rsrc", str(ROOT / "Dist" / (asset + "-app.zip")), str(extracted))
        bundle = extracted / "Apollo Studio.app"
        run("codesign", "--verify", "--deep", "--strict", str(bundle))
        run("xcrun", "lipo", str(bundle / "Contents/MacOS/Apollo"), "-verify_arch", architecture)
        for library in bundle.rglob("*.dylib"):
            run("xcrun", "lipo", str(library), "-verify_arch", architecture)
        dmg = ROOT / "Dist" / (asset + ".dmg")
        run("hdiutil", "verify", str(dmg))
        mount = output / (asset + "-mounted")
        mount.mkdir()
        run("hdiutil", "attach", "-nobrowse", "-readonly", "-mountpoint", str(mount), str(dmg))
        try:
            run("codesign", "--verify", "--deep", "--strict", str(mount / "Apollo Studio.app"))
            assert (mount / "Applications").is_symlink()
            assert (mount / "Applications").readlink() == Path("/Applications")
        finally:
            run("hdiutil", "detach", str(mount))
        print("PASS signed ZIP roundtrip, native architectures and mounted DMG:", asset, flush=True)


if __name__ == "__main__":
    main()
