#!/usr/bin/env python3
"""Derive an architecture-specific Packages project without changing the template."""
import argparse
from pathlib import Path
import plistlib

TARGETS = {"osx-x64": ("Apollo-Mac", "x86_64"), "osx-arm64": ("Apollo-Mac-arm64", "arm64")}


def prepare(rid, template):
    template = Path(template).resolve()
    with template.open("rb") as source:
        project = plistlib.load(source)

    def resolve_paths(value):
        if isinstance(value, dict):
            if value.get("PATH_TYPE") == 1 and value.get("PATH"):
                path = value["PATH"]
                if path.startswith("../Build/"):
                    path = path.replace("../Build/", f"../Build/{rid}/", 1)
                value["PATH"] = str((template.parent / path).resolve())
                value["PATH_TYPE"] = 0
            for child in value.values():
                resolve_paths(child)
        elif isinstance(value, list):
            for child in value:
                resolve_paths(child)

    resolve_paths(project)
    name, architecture = TARGETS[rid]
    settings = project["PROJECT"]["PROJECT_SETTINGS"]
    settings["NAME"] = name
    settings.setdefault("ADVANCED_OPTIONS", {})["installer-script.options:hostArchitectures"] = [architecture]
    return project


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rid", choices=TARGETS)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    with args.output.open("wb") as destination:
        plistlib.dump(prepare(args.rid, Path(__file__).with_name("Apollo.pkgproj")), destination, sort_keys=False)
