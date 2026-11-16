# How to port a mod

Everything in this repository is in English: identifiers, comments, documentation
and commit messages.

## 1. Check the status

Find the mod in [licensing/CATALOG_AUDIT.md](licensing/CATALOG_AUDIT.md). What each
status requires is in [LICENSING.md](LICENSING.md). `reserved` and `conflict` mods do
not start without the author's permission.

## 2. Download the original outside Git

Put the `.bl` package and the JavaScript code in `_upstream/<id>/`. Git ignores that
folder: the original code never enters the repository.

## 3. Create the port

Copy `templates/ModPort/` to `mods/<id>/` and rename `ModPort.cs` and `ModPort.csproj`
to the original mod's `id`. Keeping it preserves the mod's saved config and lets other
mods keep finding it by name.

| File | Rule |
|---|---|
| `manifest.json` | Keep the original `uid` and `id`, use `"blVersion": 3` and keep the authors exactly as in the JavaScript package. Do not add an entry for the C# port |
| `license.md` | Open license: copy the original text with its copyright notice. No license: fill in the template's notice |
| `port.json` | Identify the original (`id`, `uid`, `version`, `sha256`), the license and what changed |

In `port.json`, `license` is the license identifier, such as `MIT`, or `NONE` for
`credit-only` mods. The `permission` field is needed only for `reserved` and `conflict`:

```json
"permission": {
  "from": "Author name",
  "date": "2026-11-06",
  "evidence": "https://link-to-the-written-permission"
}
```

If the C# API lacks a method the original used, add it to [MISSING_API.md](MISSING_API.md)
with the workaround, instead of dropping the feature silently.

## 4. Build and check

```bash
dotnet build mods/MyMod/MyMod.csproj -c Release
```

```bash
python tools/check_licenses.py
```

Publish the `.cs` sources: the player's Bunny Loader compiles the package, and no
`.dll` enters Git.

## 5. Publish

Publish a port only after it loads on a device with no errors. The manifest `id`
must be a C# class name (a letter, then letters, digits or `_`), or the app refuses
the package.

1. Commit the port and push it. The Release tag must point to its published sources.
2. Check the package with no network:

   ```bash
   python tools/publish.py MyMod --dry-run
   ```

3. Publish:

   ```bash
   python tools/publish.py MyMod
   ```

For a new version, raise `version` in `manifest.json` first. The script refuses a
version that is already published with another file, and a version older than the
published one. [README.md](README.md#the-online-catalog) describes `store/` and the
token.

## 6. Refresh the audit when the catalog changes

The catalog changes often, and a mod may change its license. Before porting, generate
the audit again from a recent clone:

```bash
python tools/audit_catalog.py path/to/Bunny-Loader-Mods
```
