"""Run Apollo integration executables sequentially on macOS, Linux or Windows."""
import argparse
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
QUIT_CASES = ("splash", "saved", "discard", "save", "track-only", "picker-cancel", "save-error", "pending-close")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--mode", choices=("native", "headless"), default="headless")
    parser.add_argument("--suite", choices=("regression", "quit"), default="regression")
    parser.add_argument("--theme", choices=("Dark", "Light"), default="Dark")
    parser.add_argument("--software", action="store_true")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    native = args.mode == "native"
    project = ROOT / ("Apollo/Apollo.csproj" if native else "Tests/Apollo.Scenarios.csproj")
    command = [args.dotnet, "build", str(project), "-c", "Release", "--nologo", "-v:q"]
    if native:
        command += ["-p:CustomAfterMicrosoftCommonTargets=" + str(ROOT / "Tests/Scenarios.targets"),
                    "-p:StartupObject=Apollo.Tests.Scenarios", "-o", str(output / "app")]
    with (output / "build.log").open("w") as log:
        subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, check=True, timeout=300)
    assembly = output / "app/Apollo.dll" if native else ROOT / "Tests/bin/Release/net10.0/Apollo.Scenarios.dll"
    environment = os.environ.copy()
    environment.pop("APOLLO_TEST_RELEASE_ASSETS", None)
    environment.pop("APOLLO_TEST_QUIT_CASE", None)
    environment["APOLLO_TEST_THEME"] = args.theme
    environment["APOLLO_TEST_SOFTWARE"] = "1" if args.software else "0"
    for case in QUIT_CASES if args.suite == "quit" else ("run",):
        run = output / case
        run.mkdir()
        if args.suite == "quit":
            environment["APOLLO_TEST_QUIT_CASE"] = case
        with (run / "stdout.log").open("w") as stdout, (run / "stderr.log").open("w") as stderr:
            subprocess.run([args.dotnet, str(assembly), str(run)], cwd=ROOT, env=environment,
                           stdout=stdout, stderr=stderr, check=True, timeout=60)
        results = json.loads((run / "results.json").read_text())
        if not results or any(not result["passed"] for result in results):
            raise RuntimeError("Failed assertions in " + str(run))
        print(f"PASS {args.mode} {case}: {len(results)} checks ({run})", flush=True)


if __name__ == "__main__":
    main()
