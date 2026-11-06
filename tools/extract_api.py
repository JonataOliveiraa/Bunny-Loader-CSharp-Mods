#!/usr/bin/env python3
"""Extracts BunnyLoader.dll from a Bunny Loader APK into lib/.

Usage: python tools/extract_api.py <bunny-loader.apk>
"""
import pathlib
import sys
import zipfile

ROOT = pathlib.Path(__file__).resolve().parent.parent
ENTRY = "assets/dotnet/BunnyLoader.dll"


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    target = ROOT / "lib" / "BunnyLoader.dll"
    with zipfile.ZipFile(sys.argv[1]) as apk:
        try:
            target.write_bytes(apk.read(ENTRY))
        except KeyError:
            sys.exit(f"{ENTRY} is not in this APK; it must be version 0.4.0 or newer.")
    print(f"{target.relative_to(ROOT).as_posix()}: {target.stat().st_size} bytes")


if __name__ == "__main__":
    main()
