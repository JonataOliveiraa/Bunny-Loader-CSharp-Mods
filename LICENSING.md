# Licensing

A port from JavaScript to C# is a derivative work of the original mod. That is why
the root license does not apply to the ports: each one follows the rules of the mod
it came from.

This document records the project's policy. It is not legal advice.

## What applies to each part

| Part | License |
|---|---|
| Documentation, `tools/`, `templates/` and build configuration | [MIT](LICENSE) |
| `mods/<Id>/` | The terms of `mods/<Id>/license.md` |
| Original mods | Their authors' terms. They are not stored in this repository |
| Bunny Loader and `BunnyLoader.dll` | PolyForm Noncommercial 1.0.0. Not redistributed here |
| Terraria | Owned by Re-Logic. No game files enter this repository |

[Bunny Loader's LICENSING.md](https://github.com/JonataOliveiraa/Bunny-Loader/blob/main/LICENSING.md)
treats mods that use only the public API as separate works, and each mod's author
picks its license.

## Rules by status

Each mod's status is in the [audit](licensing/CATALOG_AUDIT.md).

| Status | Meaning | The port must |
|---|---|---|
| `open` | Permissive license text (MIT, Apache-2.0) | Keep the same license and the copyright notice |
| `copyleft` | GPL-3.0 or AGPL-3.0 text | Keep the same license and publish the sources |
| `declared` | The manifest declares a license but has no text | Keep the license and include its standard text |
| `credit-only` | No license declared | Credit every author and declare no license |
| `reserved` | The author reserves rights or requires permission | Have written permission recorded in `port.json` |
| `conflict` | The license information contradicts itself | Clear it up with the author and record it in `port.json` |
| `no-code` | Texture or font pack, no code | Nothing: out of scope for ports |

In every case, each author listed on the original mod stays in `authors`.

### Mods with no license (`credit-only`)

The project's convention for these mods is credit in `authors`. Credit is
acknowledgement, not a license: with no license declared, the rights stay with the
authors. So the port:

- declares `"license": "No license declared"` in its manifest and never adopts MIT or another open license;
- states in `license.md` that the rights stay with the authors;
- is removed or adjusted if one of the authors asks.

Permission given to someone else, such as whoever made the JavaScript port, does not
automatically extend to this project.

### Copyleft caveat

GPL and AGPL ports compile against `BunnyLoader.dll`, which is PolyForm
Noncommercial. Bunny Loader treats mods as separate works, but that does not settle
the question for the mods' authors, who hold their own rights under the GPL. When in
doubt, ask the author before publishing.

### Assets

A mod's license covers only what its authors could license. Check where each port's
assets come from. Art from Terraria or other brands does not become openly licensed
by being inside a mod with an open license.

## How this is checked

`tools/check_licenses.py` runs before each commit and on GitHub Actions. It rejects:

- tracked `.bl` packages, `.dll` files, APKs, game files and `.js` code;
- ports with no `license.md`, `manifest.json` or `port.json`, or without the original copyright notice;
- removed original authors, or a license different from the original;
- `reserved` or `conflict` mods with no recorded permission.

The checker only verifies what the files record. Making sure a permission is genuine
remains the responsibility of whoever makes the port.
