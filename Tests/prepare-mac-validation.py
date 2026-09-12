"""Copy a production app for desktop testing with an isolated Apollo profile."""
import argparse
import importlib.util
import json
import plistlib
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("macos_package", ROOT / "Publish/macos.py")
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rid", choices=package.ARCHITECTURES)
    parser.add_argument("destination", type=Path, help="Fresh directory for the app and profile")
    parser.add_argument("--seed-profile", type=Path, required=True,
                        help="An isolated scenario profile with updates/Discord/autosave disabled")
    parser.add_argument("--identifier", help="Optional unique bundle ID for side-by-side desktop tests; omit for updater tests")
    args = parser.parse_args()
    output = args.destination.resolve()
    output.mkdir(parents=True, exist_ok=False)
    profile = output / "profile/.apollostudio"
    profile.mkdir(parents=True)
    shutil.copy2(args.seed_profile / "Apollo.config", profile / "Apollo.config")
    bundle = output / package.NAME
    subprocess.run(["ditto", str(ROOT / f"Build/{args.rid}-app" / package.NAME), str(bundle)], check=True)
    if args.identifier:
        for relative, suffix in (("Contents/Info.plist", ""), ("Contents/Helpers/Apollo Updater.app/Contents/Info.plist", ".updater")):
            info_path = bundle / relative
            info = plistlib.loads(info_path.read_bytes())
            info["CFBundleIdentifier"] = args.identifier + suffix
            info_path.write_bytes(plistlib.dumps(info))
    for relative in ("Contents/MacOS/Apollo.runtimeconfig.json",
                     "Contents/Helpers/Apollo Updater.app/Contents/MacOS/ApolloUpdate.runtimeconfig.json"):
        config = bundle / relative
        runtime = json.loads(config.read_text())
        runtime["runtimeOptions"].setdefault("configProperties", {})["Apollo.UserPath"] = str(profile)
        config.write_text(json.dumps(runtime, indent=2) + "\n")
    package.sign_bundle(bundle, args.rid)
    print(bundle)
    print("Test data:", profile)
    print("Validation copy only: do not distribute this app with its machine-specific data path.")


if __name__ == "__main__":
    main()
