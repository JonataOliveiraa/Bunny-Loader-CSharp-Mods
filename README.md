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
| `licensing/` | [License audit of the catalog](licensing/CATALOG_AUDIT.md), mod by mod |
| `templates/ModPort/` | Starting point for a port |
| `tools/` | License audit, license check and API extraction |
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

## License

- This repository's own files: [MIT](LICENSE).
- Ports in `mods/`: the terms of each `license.md`.
- Bunny Loader, Terraria and other components: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Did you make one of the original mods and want it removed or credited differently?
[Open an issue](https://github.com/JonataOliveiraa/Bunny-Loader-CSharp-Mods/issues).
