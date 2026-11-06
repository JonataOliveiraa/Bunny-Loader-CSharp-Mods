#!/usr/bin/env python3
"""Rejects anything that would violate the mods' licenses. The rules are in LICENSING.md.

Usage: python tools/check_licenses.py

Checks tracked and staged files.
"""
import json
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent

FORBIDDEN_SUFFIXES = {".bl", ".dll", ".apk", ".aab", ".so", ".xnb", ".keystore", ".jks"}
FORBIDDEN_NAMES = {"global-metadata.dat", "data.unity3d", "_token.txt"}
NO_LICENSE = "NONE"
NO_LICENSE_MANIFEST = "No license declared"
NEEDS_PERMISSION = {"reserved", "conflict"}


def tracked_files():
    out = subprocess.run(["git", "ls-files", "-z"], cwd=ROOT, capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


def check_forbidden(path):
    pure = pathlib.PurePosixPath(path)
    if pure.parts[0] == "_upstream":
        return f"{path}: a copy of the original mod must not be tracked"
    if pure.suffix.lower() in FORBIDDEN_SUFFIXES or pure.name.lower() in FORBIDDEN_NAMES:
        return f"{path}: file type forbidden in the repository"
    if pure.parts[0] == "mods" and pure.suffix.lower() == ".js":
        return f"{path}: the original JavaScript code must not be tracked"
    return None


def load_json(path, errors):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as exc:
        errors.append(f"{path.relative_to(ROOT).as_posix()}: could not read ({exc})")
        return None


def author_names(manifest):
    names = set()
    for author in manifest.get("authors") or []:
        names.add(str((author.get("name") if isinstance(author, dict) else author) or "").casefold())
    return names


def check_port(name, files, audit, errors):
    prefix = f"mods/{name}"
    missing = [f for f in ("manifest.json", "license.md", "port.json") if f"{prefix}/{f}" not in files]
    if missing:
        errors.append(f"{prefix}: missing {', '.join(missing)}")
        return

    folder = ROOT / prefix
    manifest = load_json(folder / "manifest.json", errors)
    port = load_json(folder / "port.json", errors)
    if manifest is None or port is None:
        return

    upstream = port.get("upstream") or {}
    original = audit.get(upstream.get("id"))
    if original is None:
        errors.append(f"{prefix}: upstream.id {upstream.get('id')!r} is not in the audit")
        return

    status = original["status"]
    if status == "no-code":
        errors.append(f"{prefix}: {original['id']} has no code to port")

    permission = port.get("permission") or {}
    has_permission = all(permission.get(k) for k in ("from", "date", "evidence"))
    if status in NEEDS_PERMISSION and not has_permission:
        errors.append(f"{prefix}: {original['id']} is {status}; record the author's permission in port.json")

    if manifest.get("uid") != original["uid"] or upstream.get("uid") != original["uid"]:
        errors.append(f"{prefix}: the uid must be the original's ({original['uid']})")
    for field in ("version", "sha256", "changes"):
        if not (upstream if field != "changes" else port).get(field):
            errors.append(f"{prefix}: port.json has no {field}")

    credited = author_names(manifest)
    for author in original["authors"]:
        if author.casefold() not in credited:
            errors.append(f"{prefix}: original author not credited: {author}")

    text = (folder / "license.md").read_text(encoding="utf-8")
    if not text.strip():
        errors.append(f"{prefix}: license.md is empty")
    if original["copyright"] and original["copyright"] not in text:
        errors.append(f"{prefix}: license.md is missing the original notice: {original['copyright']}")

    # With a recorded permission, the terms come from it, not from the audit.
    if status in NEEDS_PERMISSION:
        return
    if port.get("license") != original["license"]:
        errors.append(f"{prefix}: port.json must declare license {original['license']}")
    expected = NO_LICENSE_MANIFEST if original["license"] == NO_LICENSE else original["license"]
    if manifest.get("license") != expected:
        errors.append(f"{prefix}: manifest.json must declare \"license\": \"{expected}\"")


def main():
    files = tracked_files()
    tracked = set(files)
    errors = [e for e in map(check_forbidden, files) if e]

    audit_data = load_json(ROOT / "licensing" / "catalog-audit.json", errors)
    audit = {m["id"]: m for m in audit_data["mods"]} if audit_data else {}

    ports = sorted({p.split("/")[1] for p in files if p.startswith("mods/") and p.count("/") >= 2})
    for name in ports:
        check_port(name, tracked, audit, errors)

    if errors:
        print("License check failed:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        sys.exit(1)
    print(f"Licenses OK: {len(files)} files and {len(ports)} ports checked.")


if __name__ == "__main__":
    main()
