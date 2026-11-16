#!/usr/bin/env python3
"""Publishes ports to the online catalog that Bunny Loader reads.

    python tools/publish.py hellomod [semqueda ...]   # build, check, release, index
    python tools/publish.py hellomod --dry-run        # no network and no commit
    python tools/publish.py --package Mod.bl          # a ready package, sources not in mods/
    python tools/publish.py --remove <uid>            # out of the index and Featured
    python tools/publish.py --feature consolaria hellomod
    python tools/publish.py --feature                 # empty Featured

The app reads everything anonymously from `main`:

    store/index.json      every published mod, with its whole manifest
    store/featured.json   the Home tab's Featured carousel, in order
    store/stats.json      downloads per mod (tools/download_stats.py, every 6 h)
    store/popular.json    the 10 most downloaded mods (same Action)
    store/mods/<uid>/     each mod's showcase: icon, banner, description, authors
    Releases              one per version, tag mod-<id>-v<version>, with <id>.bl

The `.bl` is the format 3 zip, packed from the port's files as committed: the
manifest, the C# sources, Assets/, Common/, Content/, Localization/, the
showcase files and the license. No bin/, obj/, .csproj or port.json. The
player's Bunny Loader compiles the sources, as it did when the ports were tested.

Only publishing needs a token: a fine-grained token with "Contents: Read and
write" on this repository only, in BL_GITHUB_TOKEN or in `_token.txt` at the
root (ignored by Git and rejected by check_licenses.py). It stays on the PC.

The Release goes first and the index second. If the push of the index fails,
running again reuses the Release (the sha256 matches) and only redoes the index.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
import re
import shutil
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from datetime import datetime, timezone
from pathlib import Path

OWNER = "JonataOliveiraa"
REPO = "Bunny-Loader-CSharp-Mods"
BRANCH = "main"
NOREPLY_EMAIL = "122224076+JonataOliveiraa@users.noreply.github.com"
API = f"https://api.github.com/repos/{OWNER}/{REPO}"
UPLOADS = f"https://uploads.github.com/repos/{OWNER}/{REPO}"
DOWNLOAD = f"https://github.com/{OWNER}/{REPO}/releases/download"

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "store"
BUILD = ROOT / "build"
TOKEN_FILE = ROOT / "_token.txt"

FORMAT = 1
BL_VERSION = 3

UID = re.compile(r"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$")
# The app's ModSources.ID: the id is the name of the Mod class and of the assembly.
MOD_ID = re.compile(r"^[A-Za-z][A-Za-z0-9_]{0,63}$")
VERSION = re.compile(r"^\d+(\.\d+)*([-+][0-9A-Za-z.]+)?$")
# The app's RemoteCatalog.SAFE_PATH: what passes here, the showcase accepts.
SAFE_PATH = re.compile(r"^[A-Za-z0-9._ -]+(/[A-Za-z0-9._ -]+)*$")
PACKAGE_PATH = re.compile(r"^[A-Za-z0-9._ ()-]+(/[A-Za-z0-9._ ()-]+)*$")
HEX_COLOR = re.compile(r"^#?([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")
MD_IMAGE = re.compile(r"!\[[^\]]*\]\(([^)\s]+)")

# Banners in the order of the app's Catalog.BANNERS: it uses the first one there.
SHOWCASE_ROOT_FILES = ["manifest.json", "icon.png", "icon.gif",
                       "banner.gif", "banner.png", "banner.webp", "banner.jpg", "banner.jpeg",
                       "description.md", "changelog.md", "license.md"]
LOCALIZED_PAGE = re.compile(r"^(?:description|changelog)_[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*\.md$")
SHOWCASE_DIRS = ("authors/", "thumbnails/")
# Repository files that do not go in the package.
NOT_PACKAGED = {"port.json"}
NOT_PACKAGED_DIRS = {"bin", "obj", "build"}
FIXED_TIME = (2020, 1, 1, 0, 0, 0)

# The app's limits.
MAX_INDEX = 8 << 20
MAX_SMALL_JSON = 1 << 20
MAX_SHOWCASE_FILE = 4 << 20
MAX_PACKAGE = 200 << 20


class Fail(Exception):
    pass


def now() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


# ------------------------------------------------------------------ git

def git(*args: str, check: bool = True, data: bytes | None = None) -> subprocess.CompletedProcess:
    r = subprocess.run(["git", *args], cwd=ROOT, input=data, capture_output=True)
    if check and r.returncode != 0:
        raise Fail(f"git {' '.join(args)} failed:\n{r.stderr.decode(errors='replace').strip()}")
    return r


def git_text(*args: str) -> str:
    return git(*args).stdout.decode("utf-8").strip()


def committed_files(folder: str) -> dict[str, bytes]:
    """The folder's files as committed in HEAD, relative to it.

    The blobs, not the working tree: the package does not depend on local
    line endings or on files Git ignores, and it is what the tag points to.
    """
    listing = git("ls-tree", "-r", "-z", "--full-tree", "HEAD", "--", f"{folder}/").stdout
    entries = []
    for item in listing.decode("utf-8").split("\0"):
        if not item:
            continue
        meta, path = item.split("\t", 1)
        mode, kind, sha = meta.split()
        if kind != "blob" or mode == "120000":
            raise Fail(f"{path}: links and submodules cannot go in a package")
        entries.append((path[len(folder) + 1:], sha))
    if not entries:
        return {}
    out = git("cat-file", "--batch", data="".join(f"{sha}\n" for _, sha in entries).encode()).stdout
    files, at = {}, 0
    for rel, _ in entries:
        header_end = out.index(b"\n", at)
        size = int(out[at:header_end].split()[2])
        files[rel] = out[header_end + 1:header_end + 1 + size]
        at = header_end + 1 + size + 1
    return files


def package_files(path: Path) -> dict[str, bytes]:
    try:
        with zipfile.ZipFile(path) as z:
            return {i.filename: z.read(i) for i in z.infolist() if not i.is_dir()}
    except (OSError, zipfile.BadZipFile) as e:
        raise Fail(f"{path.name}: not a readable zip ({e})")


def prepare_repo(dry: bool) -> None:
    """Starts from what is on origin/main, so the index is never built on an old copy."""
    problems = []
    branch = git_text("rev-parse", "--abbrev-ref", "HEAD")
    if branch != BRANCH:
        problems.append(f"the checkout is on {branch!r}; publish from {BRANCH}")
    if git_text("status", "--porcelain", "--", "store"):
        problems.append("store/ has uncommitted changes; commit or discard them first")
    if not dry:
        git("fetch", "-q", "origin", BRANCH)
    if git("rev-parse", "--verify", "-q", f"origin/{BRANCH}", check=False).returncode != 0:
        problems.append(f"origin/{BRANCH} is unknown; run git fetch")
    else:
        # A commit of a failed push may wait here, but nothing else: the tool
        # pushes what is ahead, and only store/ is its to push.
        ahead = git_text("log", "--format=", "--name-only", f"origin/{BRANCH}..HEAD")
        outside = sorted({p for p in ahead.splitlines() if p and not p.startswith("store/")})
        if outside:
            problems.append(f"{BRANCH} has unpushed commits outside store/ ({', '.join(outside[:3])}"
                            f"{', ...' if len(outside) > 3 else ''}); push or drop them first")
    report(problems, dry)
    if not dry and git_text("rev-list", "--count", f"HEAD..origin/{BRANCH}") != "0":
        git("pull", "-q", "--rebase", "--autostash", "origin", BRANCH)


def report(problems: list[str], dry: bool) -> None:
    if problems and not dry:
        raise Fail("\n  ".join(problems))
    for p in problems:
        print(f"  warning: {p}")


def commit_and_push(message: str, dry: bool) -> None:
    if dry:
        print(f"  [dry-run] would commit \"{message}\" and push {BRANCH}")
        return
    git("add", "-A", "--", "store")
    if git("diff", "--cached", "--quiet", "--", "store", check=False).returncode == 0:
        print("  store/ unchanged")
    else:
        # The repository is public: the commit uses the noreply address, never
        # the machine's global one.
        git("-c", f"user.name={OWNER}", "-c", f"user.email={NOREPLY_EMAIL}",
            "commit", "-q", "-m", message, "--", "store")
    if git_text("rev-list", "--count", f"origin/{BRANCH}..HEAD") == "0":
        return
    for _ in range(3):
        if git("push", "-q", "origin", f"HEAD:{BRANCH}", check=False).returncode == 0:
            print(f"  {BRANCH} pushed")
            return
        # The download stats Action may have committed in between. It writes
        # other files, so the rebase is clean.
        if git("pull", "-q", "--rebase", "--autostash", "origin", BRANCH, check=False).returncode != 0:
            git("rebase", "--abort", check=False)
            break
    raise Fail(f"push of {BRANCH} failed; the commit is local. Run the same command again")


# ------------------------------------------------------------------ package

def packaged(rel: str) -> bool:
    parts = rel.split("/")
    return not (rel in NOT_PACKAGED
                or rel.lower().endswith(".csproj")
                or any(p.startswith(".") for p in parts)
                or any(p.lower() in NOT_PACKAGED_DIRS for p in parts[:-1]))


def pack(files: dict[str, bytes]) -> bytes:
    """The zip with a fixed order and date: the same files give the same sha256."""
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, "w", zipfile.ZIP_DEFLATED) as z:
        for name in sorted(files):
            info = zipfile.ZipInfo(name, FIXED_TIME)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            z.writestr(info, files[name])
    return buffer.getvalue()


class Port:
    """mods/<folder>/ as it will reach players, checked with the app's import rules."""

    def __init__(self, folder: str, files: dict[str, bytes], from_sources: bool = True):
        self.folder = folder
        self.from_sources = from_sources
        if not files:
            raise Fail(f"mods/{folder}: nothing committed; commit the port first")
        self.files = {rel: data for rel, data in files.items() if packaged(rel)}
        for rel in self.files:
            if not PACKAGE_PATH.fullmatch(rel) or any(s in (".", "..") for s in rel.split("/")):
                raise Fail(f"mods/{folder}: a path the app refuses: {rel!r}")
        if "manifest.json" not in self.files:
            raise Fail(f"mods/{folder}: no manifest.json")
        try:
            self.manifest = json.loads(self.files["manifest.json"].decode("utf-8-sig"))
        except ValueError as e:
            raise Fail(f"mods/{folder}: invalid manifest.json ({e})")
        m = self.manifest
        self.uid = str(m.get("uid", ""))
        self.id = str(m.get("id", ""))
        self.version = str(m.get("version", ""))
        self.name = str(m.get("name") or self.id)
        if not UID.fullmatch(self.uid):
            raise Fail(f"mods/{folder}: uid missing or not a lowercase UUID")
        if not MOD_ID.fullmatch(self.id):
            raise Fail(f"mods/{folder}: id {self.id!r} is not a C# class name "
                       "(a letter, then up to 63 letters, digits or underscores); the app refuses it")
        if not VERSION.fullmatch(self.version):
            raise Fail(f"mods/{folder}: version {self.version!r} is not like 1.2.0")
        if m.get("blVersion") != BL_VERSION:
            raise Fail(f"mods/{folder}: blVersion {m.get('blVersion')!r}; the app loads {BL_VERSION}")
        if f"{self.id}.cs" not in self.files and f"{self.id}.dll" not in self.files:
            raise Fail(f"mods/{folder}: no {self.id}.cs at the root")
        if "icon.png" not in self.files:
            print(f"  warning: mods/{folder} has no icon.png; the app shows the category icon")
        self.data = pack(self.files)
        self.sha256 = hashlib.sha256(self.data).hexdigest()
        self.size = len(self.data)
        if self.size > MAX_PACKAGE:
            raise Fail(f"mods/{folder}: the package has {self.size} bytes; the app's limit is 200 MB")

    @property
    def tag(self) -> str:
        return f"mod-{self.id}-v{self.version}"

    @property
    def asset(self) -> str:
        return f"{self.id}.bl"

    @property
    def url(self) -> str:
        return f"{DOWNLOAD}/{self.tag}/{self.asset}"

    def showcase(self) -> list[str]:
        """What the mod's page needs before the whole package is downloaded."""
        files = [f for f in SHOWCASE_ROOT_FILES if f in self.files]
        files += sorted(n for n in self.files if LOCALIZED_PAGE.fullmatch(n))
        files += sorted(n for n in self.files if n.startswith(SHOWCASE_DIRS))
        for page in self.manifest.get("pages") or []:
            if isinstance(page, dict) and page.get("file") in self.files:
                files.append(page["file"])
        # The page background (theme.background), when it is an image and not a color.
        theme = self.manifest.get("theme")
        background = str(theme.get("background", "")).strip() if isinstance(theme, dict) else ""
        if background and not HEX_COLOR.fullmatch(background):
            if background in self.files:
                files.append(background)
            else:
                print(f"  warning: mods/{self.folder}: theme.background {background!r} is not in the port")
        # Images the page's Markdown shows.
        for md in [f for f in files if f.endswith(".md")]:
            for ref in MD_IMAGE.findall(self.files[md].decode("utf-8", "replace")):
                ref = urllib.parse.unquote(ref.split("#")[0])
                if ref in self.files:
                    files.append(ref)
        out = list(dict.fromkeys(files))
        for f in out:
            if not SAFE_PATH.fullmatch(f) or any(s in (".", "..") for s in f.split("/")):
                raise Fail(f"mods/{self.folder}: a path the showcase refuses: {f!r}")
            if len(self.files[f]) > MAX_SHOWCASE_FILE:
                raise Fail(f"mods/{self.folder}: {f} is over 4 MB, the showcase's limit")
        return out


def build(folder: str) -> None:
    """Compiles the port against lib/BunnyLoader.dll: a port that does not build is not published."""
    if not (ROOT / "lib" / "BunnyLoader.dll").is_file():
        raise Fail("no lib/BunnyLoader.dll; see lib/README.md")
    projects = sorted((ROOT / "mods" / folder).glob("*.csproj"))
    if len(projects) != 1:
        raise Fail(f"mods/{folder}: expected one .csproj, found {len(projects)}")
    r = subprocess.run(["dotnet", "build", str(projects[0]), "-c", "Release", "-nologo", "-v", "q"],
                       cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        raise Fail(f"mods/{folder} does not build:\n{(r.stdout + r.stderr).strip()[-2000:]}")
    print("  build OK")


def check_licenses() -> None:
    r = subprocess.run([sys.executable, str(ROOT / "tools" / "check_licenses.py")],
                       cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        raise Fail((r.stdout + r.stderr).strip())
    print("  licenses OK")


def check_port_pushed(folder: str, dry: bool) -> None:
    problems = []
    if git_text("status", "--porcelain", "--", f"mods/{folder}"):
        problems.append(f"mods/{folder} has uncommitted changes, which are not in the package")
    if git("diff", "--quiet", f"origin/{BRANCH}", "HEAD", "--", f"mods/{folder}", check=False).returncode != 0:
        problems.append(f"mods/{folder} differs from origin/{BRANCH}; push the port first, "
                        "so the Release tag points to its sources")
    report(problems, dry)


# ------------------------------------------------------------------ GitHub

_token: str | None = None


def token() -> str:
    global _token
    if _token:
        return _token
    t = os.environ.get("BL_GITHUB_TOKEN", "")
    if not t and TOKEN_FILE.is_file():
        t = TOKEN_FILE.read_text(encoding="utf-8")
    if not t.strip() and sys.stdin.isatty():
        import getpass
        t = getpass.getpass("GitHub token (paste and press Enter; it is not shown): ")
    t = t.strip()
    if not t:
        raise Fail(f"no token: set BL_GITHUB_TOKEN or put it in {TOKEN_FILE.name} "
                   f"(fine-grained, Contents: Read and write on {OWNER}/{REPO} only)")
    _token = t
    return t


def gh(method: str, url: str, body=None, raw: bytes | None = None, content_type: str = "application/json"):
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    req = urllib.request.Request(url, data=data, method=method, headers={
        "Authorization": f"Bearer {token()}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
        "User-Agent": "bunny-loader-csharp-mods-publish",
        **({"Content-Type": content_type} if data is not None else {}),
    })
    try:
        with urllib.request.urlopen(req, timeout=600) as r:
            text = r.read()
            return json.loads(text) if text else None
    except urllib.error.HTTPError as e:
        if e.code == 404:
            return None
        raise Fail(f"GitHub {method} {url}: {e.code} {e.read().decode(errors='replace')[:300]}")


def release(port: Port, dry: bool) -> None:
    """Creates the Release with the .bl, or checks the one that already exists."""
    if dry:
        print(f"  [dry-run] would create Release {port.tag} with {port.asset}")
        return
    existing = gh("GET", f"{API}/releases/tags/{port.tag}")
    if existing:
        asset = next((a for a in existing.get("assets", []) if a["name"] == port.asset), None)
        if asset:
            digest = (asset.get("digest") or "").removeprefix("sha256:")
            if not digest:
                with urllib.request.urlopen(asset["browser_download_url"], timeout=600) as r:
                    digest = hashlib.sha256(r.read()).hexdigest()
            if digest != port.sha256:
                raise Fail(f"{port.tag} was already published with another file. "
                           "Raise the version in manifest.json instead of replacing a published one")
            print(f"  Release {port.tag} already has this file: reused")
            return
        target = existing
    else:
        commit = git_text("rev-parse", f"origin/{BRANCH}")
        target = gh("POST", f"{API}/releases", {
            "tag_name": port.tag,
            # The tag points to the published sources of this exact package.
            "target_commitish": commit,
            "name": f"{port.name} v{port.version}",
            "body": (f"{port.name} for Bunny Loader.\n\n"
                     f"- uid: `{port.uid}`\n"
                     f"- sha256: `{port.sha256}`\n"
                     f"- license: {port.manifest.get('license') or 'see license.md'}\n"
                     + (f"- sources: https://github.com/{OWNER}/{REPO}/tree/{commit}/mods/{port.folder}\n"
                        if port.from_sources else "")),
            "make_latest": "false",
        })
    q = urllib.parse.urlencode({"name": port.asset})
    gh("POST", f"{UPLOADS}/releases/{target['id']}/assets?{q}", raw=port.data, content_type="application/zip")
    print(f"  Release {port.tag} published")


# ------------------------------------------------------------------ store

def load(store: Path, name: str, default: dict) -> dict:
    f = store / name
    return json.loads(f.read_text(encoding="utf-8")) if f.is_file() else default


def save(store: Path, name: str, data: dict, limit: int) -> None:
    text = json.dumps(data, ensure_ascii=False, indent=2) + "\n"
    if len(text.encode("utf-8")) > limit:
        raise Fail(f"store/{name} would be over the app's limit of {limit >> 20} MB")
    (store / name).write_text(text, encoding="utf-8", newline="\n")


def load_index(store: Path) -> dict:
    return load(store, "index.json", {"format": FORMAT, "mods": []})


def save_index(store: Path, index: dict) -> None:
    index["format"] = FORMAT
    index["mods"].sort(key=lambda m: (str(m["manifest"].get("name", "")).casefold(), m["uid"]))
    save(store, "index.json", index, MAX_INDEX)


def version_key(version: str) -> tuple[int, ...]:
    return tuple(int(p) for p in re.match(r"\d+(\.\d+)*", version).group(0).split("."))


def check_against_index(index: dict, port: Port) -> None:
    """Refuses what would confuse the app or overwrite a published package."""
    for m in index["mods"]:
        if m["uid"] != port.uid and m["manifest"].get("id") == port.id:
            raise Fail(f"{port.id}: another mod ({m['uid']}) already uses this id and its Release tags")
    old = next((m for m in index["mods"] if m["uid"] == port.uid), None)
    if old is None:
        return
    if old["version"] == port.version and old["download"]["sha256"] != port.sha256:
        raise Fail(f"{port.name} v{port.version} was already published with another file. "
                   "Raise the version in manifest.json instead of replacing a published one")
    if version_key(port.version) < version_key(old["version"]):
        raise Fail(f"{port.name}: v{port.version} is older than the published v{old['version']}")


def index_entry(index: dict, port: Port, files: list[str], published_at: str) -> dict:
    old = next((m for m in index["mods"] if m["uid"] == port.uid), None)
    return {
        "uid": port.uid,
        "version": port.version,
        "download": {"url": port.url, "sha256": port.sha256, "size": port.size},
        "files": files,
        "manifest": port.manifest,
        # The first publication's date stays: the app shows "new" for 7 days.
        "publishedAt": (old or {}).get("publishedAt") or published_at,
    }


def write_showcase(store: Path, port: Port) -> list[str]:
    files = port.showcase()
    target = store / "mods" / port.uid
    if target.exists():
        shutil.rmtree(target)
    for f in files:
        out = target / f
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_bytes(port.files[f])
    return files


def resolve(index: dict, key: str) -> dict:
    found = next((m for m in index["mods"] if key in (m["uid"], m["manifest"].get("id"))), None)
    if found is None:
        raise Fail(f"{key} is not in store/index.json")
    return found


def store_for(dry: bool) -> Path:
    """The real store/, or a copy in build/dry-run/ that a dry run may change."""
    if not dry:
        return STORE
    copy = BUILD / "dry-run" / "store"
    if copy.exists():
        shutil.rmtree(copy)
    shutil.copytree(STORE, copy)
    return copy


# ------------------------------------------------------------------ commands

def publish(folders: list[str], dry: bool, packages: list[Path] = ()) -> None:
    prepare_repo(dry)
    check_licenses()
    ports = []
    for folder in folders:
        if not (ROOT / "mods" / folder).is_dir():
            raise Fail(f"no mods/{folder}/")
        print(f"mods/{folder}")
        check_port_pushed(folder, dry)
        port = Port(folder, committed_files(f"mods/{folder}"))
        build(folder)
        print(f"  {port.name} v{port.version} ({port.uid})")
        print(f"  {port.asset}: {port.size} bytes, sha256 {port.sha256}")
        ports.append(port)
    for path in packages:
        print(path.name)
        port = Port(path.name, package_files(path), from_sources=False)
        print(f"  {port.name} v{port.version} ({port.uid})")
        print(f"  {port.asset}: {port.size} bytes, sha256 {port.sha256}")
        ports.append(port)
    if len({p.uid for p in ports}) != len(ports):
        raise Fail("two ports with the same uid in one call")

    store = store_for(dry)
    index = load_index(store)
    for port in ports:
        check_against_index(index, port)
    BUILD.mkdir(exist_ok=True)
    for port in ports:
        out = BUILD / "packages" / f"{port.id}-{port.version}.bl"
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_bytes(port.data)
        print(f"{port.name}: {out.relative_to(ROOT).as_posix()}")
        release(port, dry)
        files = write_showcase(store, port)
        entry = index_entry(index, port, files, now())
        index["mods"] = [m for m in index["mods"] if m["uid"] != port.uid] + [entry]
        print(f"  showcase: {', '.join(files)}")
    save_index(store, index)
    if dry:
        print(f"  [dry-run] the store as it would be: {store.relative_to(ROOT).as_posix()}")
    commit_and_push("Publish " + ", ".join(f"{p.name} v{p.version}" for p in ports), dry)


def remove(uid: str, dry: bool) -> None:
    prepare_repo(dry)
    store = store_for(dry)
    index = load_index(store)
    mod = next((m for m in index["mods"] if m["uid"] == uid), None)
    if mod is None:
        raise Fail(f"{uid} is not in store/index.json")
    index["mods"].remove(mod)
    featured = load(store, "featured.json", {"format": FORMAT, "mods": []})
    featured["mods"] = [u for u in featured["mods"] if u != uid]
    shutil.rmtree(store / "mods" / uid, ignore_errors=True)
    save_index(store, index)
    save(store, "featured.json", featured, MAX_SMALL_JSON)
    name = mod["manifest"].get("name", uid)
    print(f"  {name} removed; its Releases stay")
    commit_and_push(f"Remove {name} from the catalog", dry)


def feature(keys: list[str], dry: bool) -> None:
    prepare_repo(dry)
    store = store_for(dry)
    index = load_index(store)
    mods = list(dict.fromkeys(resolve(index, k)["uid"] for k in keys))
    names = [resolve(index, u)["manifest"].get("name", u) for u in mods]
    save(store, "featured.json", {"format": FORMAT, "mods": mods}, MAX_SMALL_JSON)
    print(f"  Featured: {', '.join(names) if names else '(none)'}")
    commit_and_push(f"Feature {', '.join(names)}" if names else "Clear Featured", dry)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("ports", nargs="*", metavar="Id", help="folders in mods/ to publish")
    ap.add_argument("--package", nargs="+", default=[], metavar="FILE",
                    help="ready .bl or .zip packages whose sources are not in mods/")
    ap.add_argument("--dry-run", action="store_true", help="check and pack, with no network and no commit")
    ap.add_argument("--remove", metavar="UID", help="take a mod out of the index and Featured")
    ap.add_argument("--feature", nargs="*", metavar="UID_OR_ID", help="rewrite Featured in this order")
    args = ap.parse_args()
    try:
        if args.remove:
            remove(args.remove, args.dry_run)
        elif args.feature is not None:
            feature(args.feature, args.dry_run)
        elif args.ports or args.package:
            publish(args.ports, args.dry_run, [Path(p) for p in args.package])
        else:
            ap.print_help()
            return 2
    except Fail as e:
        print(f"error: {e}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
