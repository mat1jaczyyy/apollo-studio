"""Validate both generated installers against Apollo's existing payload structure."""
import importlib.util
from pathlib import Path
import plistlib
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("mac_package", ROOT / "Publish/prepare-mac-package.py")
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)


def paths(value):
    if isinstance(value, dict):
        if "PATH" in value:
            yield value["PATH"]
        for child in value.values():
            yield from paths(child)
    elif isinstance(value, list):
        for child in value:
            yield from paths(child)


class MacPackageTests(unittest.TestCase):
    def test_both_architectures_keep_payload_and_resources(self):
        template = ROOT / "Publish/Apollo.pkgproj"
        original = template.read_bytes()
        original_project = plistlib.loads(original)
        for rid, (name, architecture) in package.TARGETS.items():
            with self.subTest(rid=rid):
                result = package.prepare(rid, template)
                result = plistlib.loads(plistlib.dumps(result))
                settings = result["PROJECT"]["PROJECT_SETTINGS"]
                self.assertEqual(settings["NAME"], name)
                self.assertEqual(settings["ADVANCED_OPTIONS"]["installer-script.options:hostArchitectures"], [architecture])
                result_paths = set(paths(result))
                for folder in ("Apollo", "Update", "M4L"):
                    self.assertIn(str(ROOT / "Build" / rid / folder), result_paths)
                self.assertIn(str(ROOT / "Publish/post.sh"), result_paths)
                self.assertIn(str(ROOT / "Dist"), result_paths)
                self.assertEqual(result["PACKAGES"][0]["PACKAGE_SETTINGS"], original_project["PACKAGES"][0]["PACKAGE_SETTINGS"])
                for path in result_paths:
                    self.assertNotIn("../", path)
                self.assertEqual(template.read_bytes(), original)

    def test_architectures_do_not_share_output_paths(self):
        intel = package.prepare("osx-x64", ROOT / "Publish/Apollo.pkgproj")
        arm = package.prepare("osx-arm64", ROOT / "Publish/Apollo.pkgproj")
        self.assertNotEqual(intel["PROJECT"]["PROJECT_SETTINGS"]["NAME"], arm["PROJECT"]["PROJECT_SETTINGS"]["NAME"])
        self.assertNotIn(str(ROOT / "Build/osx-x64/Apollo"), set(paths(arm)))
        self.assertNotIn(str(ROOT / "Build/osx-arm64/Apollo"), set(paths(intel)))


if __name__ == "__main__":
    unittest.main()
