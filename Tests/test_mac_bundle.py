"""Portable checks for real app-bundle metadata and payload assembly."""
import importlib.util
from pathlib import Path
import plistlib
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("mac_bundle", ROOT / "Publish/macos.py")
package = importlib.util.module_from_spec(spec)
spec.loader.exec_module(package)


class MacBundleTests(unittest.TestCase):
    def test_each_architecture_has_a_gui_bundle_and_hidden_helper(self):
        for rid, architecture in package.ARCHITECTURES.items():
            with self.subTest(rid=rid), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                app, updater, connectors = (root / name for name in ("app", "updater", "connectors"))
                for directory in (app, updater, connectors):
                    directory.mkdir()
                for name in ("Apollo", "Apollo.dll", "libcoreclr.dylib", "rtmidi.dll", "librtmidi.so"):
                    (app / name).write_bytes(name.encode())
                for name in ("ApolloUpdate", "ApolloUpdate.dll", "handle64.exe"):
                    (updater / name).write_bytes(name.encode())
                (connectors / "Apollo.amxd").write_bytes(b"connector")
                icon = root / "icon.icns"
                icon.write_bytes(b"icns")
                bundle = package.make_bundle(app, updater, root / package.NAME, rid, icon=icon, connectors=connectors, build_version="1.8.17")
                contents = bundle / "Contents"
                info = plistlib.loads((contents / "Info.plist").read_bytes())
                self.assertEqual(info["CFBundleIdentifier"], package.IDENTIFIER)
                self.assertEqual(info["CFBundleExecutable"], "Apollo")
                self.assertEqual(info["CFBundleVersion"], "1.8.17")
                self.assertEqual(info["LSArchitecturePriority"], [architecture])
                self.assertEqual(info["LSMinimumSystemVersion"], "12.0")
                self.assertNotIn("LSUIElement", info)
                self.assertNotIn("CFBundleDocumentTypes", info)  # No unimplemented Finder document handlers.
                self.assertTrue(info["NSHighResolutionCapable"])
                self.assertEqual((contents / "MacOS/Apollo.dll").read_bytes(), b"Apollo.dll")
                self.assertFalse((contents / "MacOS/rtmidi.dll").exists())
                self.assertFalse((contents / "MacOS/librtmidi.so").exists())
                self.assertEqual((contents / "Resources/Apollo.icns").read_bytes(), b"icns")
                self.assertEqual((contents / "Resources/M4L/Apollo.amxd").read_bytes(), b"connector")
                helper = contents / "Helpers/Apollo Updater.app/Contents"
                self.assertTrue(plistlib.loads((helper / "Info.plist").read_bytes())["LSUIElement"])
                self.assertTrue((helper / "MacOS/ApolloUpdate.dll").is_file())
                self.assertFalse((helper / "MacOS/handle64.exe").exists())
                self.assertTrue((app / "rtmidi.dll").exists())  # Original legacy output is intact.

    def test_missing_apphost_is_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            with self.assertRaises(ValueError):
                package.make_bundle(root / "app", root / "updater", root / package.NAME, "osx-arm64")
            self.assertFalse((root / package.NAME).exists())


if __name__ == "__main__":
    unittest.main()