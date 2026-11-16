# Bunny Loader C# Mods

C# ports of the JavaScript mods from the
[Bunny Loader](https://github.com/JonataOliveiraa/Bunny-Loader) catalog.

Since version 0.4.0, Bunny Loader runs only C#, and JavaScript packages no longer
load. This repository brings the mods from
[Bunny-Loader-Mods](https://github.com/JonataOliveiraa/Bunny-Loader-Mods) to the new
API while preserving the rights and credits of the people who made them.

> **The mods have different licenses.** The MIT license at the root covers only this
> repository's own files. Each port in `mods/` follows the terms of its `license.md`.
> See [LICENSING.md](LICENSING.md).

## Layout

| Path | Contents |
|---|---|
| `mods/<Id>/` | A port: C# sources, `manifest.json`, `license.md` and `port.json` |
| `store/` | The online catalog the app reads ([below](#the-online-catalog)) |
| `PORT_PLAN.md` | [Which mods to port, and in what order](PORT_PLAN.md) |
| `MISSING_API.md` | [What the C# API lacks](MISSING_API.md), and how the ports work around it |
| `licensing/` | [License audit of the catalog](licensing/CATALOG_AUDIT.md), mod by mod |
| `templates/ModPort/` | Starting point for a port |
| `tools/` | Publishing, download stats, license audit, license check and API extraction |
| `lib/` | Local `BunnyLoader.dll`, not in Git ([how to get it](lib/README.md)) |

## Setup

Requirements: the [.NET 10 SDK](https://dotnet.microsoft.com/download), Python 3 and a
Bunny Loader APK.

```bash
git config core.hooksPath .githooks
```

```bash
python tools/extract_api.py path/to/bunny-loader.apk
```

```bash
dotnet build mods/MyMod/MyMod.csproj -c Release
```

The first command turns on the license check before each commit. To run it by hand:

```bash
python tools/check_licenses.py
```

## Porting a mod

1. Find the mod in the [audit](licensing/CATALOG_AUDIT.md) and see what its status requires.
2. Copy `templates/ModPort/` to `mods/<Id>/` and fill in the files.
3. Follow the [migration guide](https://github.com/JonataOliveiraa/Bunny-Loader/blob/main/docs/mods-em-csharp/07-migracao-para-csharp.md).
   If the port keeps the `uid` and the class names, players keep their saves.

The full steps are in [CONTRIBUTING.md](CONTRIBUTING.md).

## The online catalog

Bunny Loader's Explore and Home tabs read this repository anonymously, from `main`.
Nothing in `store/` is edited by hand except `featured.json`.

| Path | Written by | Contents |
|---|---|---|
| `store/index.json` | `tools/publish.py` | Every published mod, with its whole manifest |
| `store/featured.json` | The maintainer, by hand or `publish.py --feature` | The Home carousel, in order |
| `store/stats.json` | The download stats Action, every 6 hours | Downloads per mod, all versions added up |
| `store/popular.json` | The same Action | The 10 most downloaded mods |
| `store/mods/<uid>/` | `tools/publish.py` | Each mod's page: icon, banner, description, authors |
| Release `mod-<id>-v<version>` | `tools/publish.py` | The `<id>.bl` package |

The `.bl` is a zip of the port as committed: the manifest, the C# sources, `Assets/`,
`Common/`, `Content/`, `Localization/`, the page files and the license. It has no
`bin/`, `obj/`, `.csproj` or `port.json`, and the player's Bunny Loader compiles it.

```bash
python tools/publish.py hellomod --dry-run
```

```bash
python tools/publish.py hellomod
```

The dry run builds the port, checks it, packs it in `build/packages/` and shows the
new `store/` in `build/dry-run/`, with no network and no commit. Publishing then
creates the Release, updates `store/` and pushes `main`. It needs a fine-grained
token with **Contents: Read and write** on this repository only, in `BL_GITHUB_TOKEN`
or in `_token.txt` at the root. Git ignores that file and the license check rejects it.

```bash
python tools/publish.py --feature consolaria hellomod
```

```bash
python tools/publish.py --remove <uid>
```

`--feature` rewrites `featured.json` in the given order (uids or ids), and with no
mods it empties it. `--remove` takes a mod out of the index and of Featured, and its
Releases stay.

## License

- This repository's own files: [MIT](LICENSE).
- Ports in `mods/`: the terms of each `license.md`.
- Bunny Loader, Terraria and other components: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Did you make one of the original mods and want it removed or credited differently?
[Open an issue](https://github.com/JonataOliveiraa/Bunny-Loader-CSharp-Mods/issues).
