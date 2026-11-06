#!/usr/bin/env python3
"""Generates licensing/catalog-audit.json and CATALOG_AUDIT.md from a clone of the catalog.

Usage: python tools/audit_catalog.py <Bunny-Loader-Mods clone>

Reads the files through Git, so it works even if the checkout fails on Windows.
"""
import collections
import json
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / "licensing"

STATUS_LABELS = {
    "open": "Open license",
    "copyleft": "Copyleft",
    "declared": "License declared, no text",
    "credit-only": "No license: credit the authors",
    "reserved": "Rights reserved: requires permission",
    "conflict": "Conflicting information: clear it up with the author",
    "no-code": "No code: out of scope",
}

# Cases automatic detection cannot settle: restrictions written in prose, and contradictions.
OVERRIDES = {
    "isekaid": ("reserved", "Personal-use", "license.md forbids copying, changes and derivatives without written permission."),
    "lanternslot": ("reserved", "All rights reserved", "The manifest declares rights reserved, and license.md grants no license."),
    "tetovanitysets": ("reserved", "All rights reserved", "license.md says to ask the original author before redistributing."),
    "qualityofbunny": ("conflict", "MIT?", "The manifest declares MIT, but license.md says \"ain't no license\"."),
}

NOTES = {
    "consolaria": "The copyright notice has template brackets: \"[2022] [RoA Gang]\". Copy it as is.",
    "defaultdash": "Already in C#: it needs a package update, not a port. The manifest says \"no license\".",
    "gorilla": "The author field only names who made the port; the original creator is not identified.",
    "overhealth": "Declares AGPL-3.0 on a port of another author's mod; confirm the original's license.",
    "templopiramide": "The loader's LICENSING says samples/ is MIT, but this sample has no license.md.",
    "umamusumevanities": "The description cites permission from Nyacro to whoever made the JS port; it does not extend to this project.",
    "unifiedstorage": "license.md summarizes MIT, names no holder for the port's code, and credits Magic Storage for textures and concept.",
    "wmitf": "The copyright notice (Yuriy Grishchenko) names someone not in authors; confirm the holder.",
}

COPYRIGHT = re.compile(r"(?im)^.*copyright\s*(?:\(c\)|©|\d).*$")


def git(repo, *args):
    return subprocess.run(["git", "-C", str(repo), *args], capture_output=True, check=True).stdout


def show(repo, path):
    return git(repo, "show", f"HEAD:{path}").decode("utf-8", "replace")


def author_names(manifest):
    names = []
    for author in manifest.get("authors") or []:
        names.append(author.get("name") if isinstance(author, dict) else author)
    if not names and manifest.get("author"):
        names.append(manifest["author"])
    return [n for n in names if n]


def license_from_text(text):
    head = text.lstrip().lstrip("#").lstrip().lower()
    # Order matters: section 13 of the GPL-3.0 names the AGPL.
    if head.startswith("gnu general public license"):
        return "GPL-3.0"
    if "gnu affero general public license" in text.lower():
        return "AGPL-3.0"
    if head.startswith("apache license"):
        return "Apache-2.0"
    if head.startswith("mit license"):
        return "MIT"
    return None


def copyright_line(text):
    for match in COPYRIGHT.finditer(text):
        line = match.group(0).strip()
        # Skip the GPL's own notice and the template in its appendix.
        if "Free Software Foundation" not in line and "<year>" not in line:
            return line
    return None


def classify(mod_id, manifest, text):
    """Returns (status, license, note)."""
    if mod_id in OVERRIDES:
        return OVERRIDES[mod_id]
    note = NOTES.get(mod_id, "")
    declared = manifest.get("license") or ""
    if not manifest.get("entry"):
        return "no-code", declared or (license_from_text(text) if text else None) or "NONE", note
    if text:
        spdx = license_from_text(text)
        if spdx:
            return ("copyleft" if "GPL" in spdx else "open"), spdx, note
    if declared in ("MIT", "Apache-2.0"):
        return "declared", declared, note
    if declared in ("GPL-3.0", "AGPL-3.0"):
        return "declared", declared, note
    return "credit-only", "NONE", note


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    repo = pathlib.Path(sys.argv[1])
    commit = git(repo, "log", "-1", "--format=%h %cs").decode().strip()
    tree = set(git(repo, "ls-tree", "-r", "HEAD", "--name-only", "-z").decode("utf-8").split("\0"))
    index = json.loads(show(repo, "index.json"))

    mods = []
    for entry in sorted(index["mods"], key=lambda m: m["id"]):
        uid = entry["uid"]
        manifest = entry.get("manifest") or json.loads(show(repo, f"mods/{uid}/manifest.json"))
        lic_path = f"mods/{uid}/license.md"
        text = show(repo, lic_path) if lic_path in tree else None
        status, license_id, note = classify(entry["id"], manifest, text)
        script = manifest.get("entry") or ""
        mods.append({
            "id": entry["id"],
            "uid": uid,
            "name": entry.get("name") or manifest.get("name"),
            "version": entry.get("version") or manifest.get("version"),
            "sha256": (entry.get("download") or {}).get("sha256"),
            "language": "cs" if script.endswith(".cs") else "js" if script else "none",
            "authors": author_names(manifest),
            "status": status,
            "license": license_id,
            "hasLicenseText": text is not None,
            "copyright": copyright_line(text) if text else None,
            "note": note,
        })

    OUT.mkdir(exist_ok=True)
    data = {"catalog": "JonataOliveiraa/Bunny-Loader-Mods", "commit": commit, "mods": mods}
    (OUT / "catalog-audit.json").write_text(
        json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

    counts = collections.Counter(m["status"] for m in mods)
    lines = [
        "# Catalog license audit",
        "",
        f"Generated by `tools/audit_catalog.py` from `{data['catalog']}` at commit `{commit}`.",
        "Do not edit by hand. The rules for each status are in [LICENSING.md](../LICENSING.md).",
        "",
        "This table summarizes what the catalog declares: confirm with the original mod before porting.",
        "Author names and license names are copied from the catalog as written.",
        "",
        "| Status | Mods | Meaning |",
        "|---|---:|---|",
    ]
    lines += [f"| `{s}` | {counts[s]} | {label} |" for s, label in STATUS_LABELS.items()]
    lines += [f"| **Total** | **{len(mods)}** | |", "", "| Mod | Version | Code | Authors | License | Status | Note |", "|---|---|---|---|---|---|---|"]
    for m in mods:
        cells = [f"`{m['id']}`", m["version"], m["language"], ", ".join(m["authors"]) or "(not stated)",
                 m["license"], f"`{m['status']}`", m["note"]]
        lines.append("| " + " | ".join(str(c).replace("|", "\\|") for c in cells) + " |")
    (OUT / "CATALOG_AUDIT.md").write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")

    print(f"{len(mods)} mods audited at commit {commit}:")
    for status in STATUS_LABELS:
        print(f"  {status}: {counts[status]}")


if __name__ == "__main__":
    main()
