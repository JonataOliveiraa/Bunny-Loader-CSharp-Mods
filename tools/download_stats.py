#!/usr/bin/env python3
"""Counts each published mod's downloads and ranks the most downloaded.

    python tools/download_stats.py [repository root]

GitHub counts every download of a Release asset (`download_count`). For each mod
in store/index.json, this adds up the `.bl` of all its Releases
(`mod-<id>-v<version>`, older versions included) and writes:

    store/stats.json    {"format": 1, "updatedAt": "...", "downloads": {"<uid>": 123}}
    store/popular.json  {"format": 1, "updatedAt": "...", "mods": [{"uid": "<uid>", "downloads": 1234}]}

popular.json has at most 10 mods, most downloaded first, with no mod at 0 and
ties broken by the mod's name. A file is rewritten only when its numbers change.

The download-stats Action runs this every 6 hours. Standard library only: the
Action installs nothing. With GH_TOKEN (the Action passes its own) the API
allows 1000 requests per hour; without it, 60.
"""
from __future__ import annotations

import json
import os
import sys
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

REPO = os.environ.get("GITHUB_REPOSITORY", "JonataOliveiraa/Bunny-Loader-CSharp-Mods")
FORMAT = 1
POPULAR_COUNT = 10


def tag_id(tag: str) -> str | None:
    """`mod-hellomod-v2.0.0` -> `hellomod`. Ids have no dash, so the first one ends it."""
    if not tag.startswith("mod-"):
        return None
    mod_id, dash, rest = tag[4:].partition("-")
    return mod_id if mod_id and dash and rest.startswith("v") else None


def release_tag(url: str) -> str | None:
    """The Release tag in the index's download link."""
    marker = "/releases/download/"
    if marker not in url:
        return None
    return url.split(marker, 1)[1].split("/", 1)[0]


def count(index: dict, releases: list[dict]) -> dict[str, int]:
    """Downloads of each uid in the index. A mod with none has 0."""
    by_id: dict[str, str] = {}
    for mod in index.get("mods", []):
        uid = mod["uid"]
        if mod.get("manifest", {}).get("id"):
            by_id[mod["manifest"]["id"]] = uid
        # The current link too: after an id change, the old Releases still count.
        tid = tag_id(release_tag(mod.get("download", {}).get("url", "")) or "")
        if tid:
            by_id[tid] = uid
    counts = {mod["uid"]: 0 for mod in index.get("mods", [])}
    for release in releases:
        uid = by_id.get(tag_id(release.get("tag_name", "")) or "")
        if uid is None:
            continue
        counts[uid] += sum(int(asset.get("download_count", 0)) for asset in release.get("assets", [])
                           if asset.get("name", "").endswith(".bl"))
    return dict(sorted(counts.items()))


def popular(index: dict, downloads: dict[str, int]) -> list[dict]:
    """The top 10, most downloaded first, ties by name."""
    names = {mod["uid"]: str(mod.get("manifest", {}).get("name", "")).casefold() for mod in index.get("mods", [])}
    ranked = sorted((uid for uid, n in downloads.items() if n > 0),
                    key=lambda uid: (-downloads[uid], names.get(uid, ""), uid))
    return [{"uid": uid, "downloads": downloads[uid]} for uid in ranked[:POPULAR_COUNT]]


def fetch_releases() -> list[dict]:
    headers = {"Accept": "application/vnd.github+json", "User-Agent": "bunny-loader-csharp-mods-stats"}
    token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
    if token:
        headers["Authorization"] = f"Bearer {token}"
    out: list[dict] = []
    page = 1
    while True:
        url = f"https://api.github.com/repos/{REPO}/releases?per_page=100&page={page}"
        with urllib.request.urlopen(urllib.request.Request(url, headers=headers), timeout=30) as r:
            batch = json.load(r)
        if not batch:
            return out
        out += batch
        page += 1


def write(target: Path, key: str, value) -> bool:
    """Writes the file if `key` changed. The date alone is not worth a commit."""
    try:
        old = json.loads(target.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        old = {}
    if key in old and old[key] == value:
        return False
    data = {
        "format": FORMAT,
        "updatedAt": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        key: value,
    }
    target.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    return True


def main() -> int:
    store = (Path(sys.argv[1]) if len(sys.argv) > 1 else Path(".")) / "store"
    index = json.loads((store / "index.json").read_text(encoding="utf-8"))
    downloads = count(index, fetch_releases())
    top = popular(index, downloads)
    changed = [name for name, key, value in (("stats.json", "downloads", downloads), ("popular.json", "mods", top))
               if write(store / name, key, value)]
    print(f"{sum(downloads.values())} downloads in {len(downloads)} mods; "
          + (f"wrote {', '.join(changed)}" if changed else "no change"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
