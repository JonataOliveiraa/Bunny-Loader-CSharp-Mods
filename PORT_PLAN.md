# Port plan

Plan for porting the catalog's JavaScript mods to C#. It covers the 50 most downloaded mods
plus 12 picked from the other 25.

Downloads come from the catalog's `stats.json` (updated 2026-11-06 19:31 UTC, commit `1dff6dc`).
They add up all versions of each mod and show which ones to do first, not a strict order.

## Summary

| Group | Mods | Downloads |
|---|---:|---:|
| Phase 1: Pilots | 6 | 4,604 |
| Phase 2: High demand, moderate size | 14 | 15,289 |
| Phase 3: Flagships | 9 | 12,711 |
| Phase 4: Long tail and selected mods | 25 | 5,607 |
| Blocked until the authors answer | 4 | 2,079 |
| No code: not a port | 4 | 2,038 |
| **Total in the plan** | **62** | **42,328** |
| Not selected | 13 | 1,323 |

The four phases cover 38,211 of the catalog's 43,651 downloads (88%).

## Before starting

- [ ] Get a Bunny Loader APK and run `tools/extract_api.py`. Until then, nothing builds against the real API.
- [ ] Download the packages in the plan to `_upstream/`, count the JavaScript in each one and correct the effort below.
- [ ] Set up a test device or emulator, as described in the loader's [first mod guide](https://github.com/JonataOliveiraa/Bunny-Loader/blob/main/docs/mods-em-csharp/01-primeiro-mod.md).
- [ ] Decide how ports reach players. The loader's migration guide still lists the catalog tools (`publish.py`, `pack.py`) and the online catalog as pending.
- [ ] Refresh the audit with `tools/audit_catalog.py` and check whether an author has already released a C# version.

**All 75 catalog packages are still `blVersion: 2`,** which the loader's documentation says no longer loads. Until they are replaced, these mods are unavailable to players on current versions.

## Definition of done

A port is done when:

- it builds against the real `BunnyLoader.dll`, and `tools/check_licenses.py` passes;
- it meets the requirements of its [license status](LICENSING.md), with every original author credited;
- it loads on a device with no errors in the log, and its features match the original;
- it keeps the original `uid` and class names, and an existing save still works;
- mods that run on every frame or draw on screen stay at the game's normal frame rate.

## How to read the tables

- **Rank:** position by downloads. `*` marks a mod picked from outside the top 50.
- **Package:** size of the `.bl`. Most of it may be assets, so it does not measure the code.
- **Effort:** an estimate from the size and description, made without reading the code. Correct it after the count above.
  - `S`: a few hooks or one small system.
  - `M`: a custom UI, several systems or a rendering effect.
  - `L`: a lot of content or heavy rendering.
  - `XL`: a lot of content plus systems of its own.

## Phase 1: Pilots

One small mod per license path, to prove the whole workflow before scaling up.

| Done | Rank | Mod | Downloads | License | Package | Effort | Notes |
|---|---:|---|---:|---|---:|---|---|
| [ ] | 50 | `hellomod` | 186 | `open` (MIT) | 14 KB | S | Open license path. Also listed as pending in the loader's migration guide. |
| [ ] | 63 * | `dobrodedano` | 109 | `open` (MIT) | 16 KB | S | Open license. Also pending in the loader's migration guide. |
| [ ] | 71 * | `semqueda` | 85 | `open` (MIT) | 11 KB | S | Open license. Also pending in the loader's migration guide. |
| [ ] | 3 | `veinminer` | 2,608 | `credit-only` | 47 KB | S | Credit-only path, and the #3 mod by downloads. |
| [ ] | 14 | `infinitypotionbuff` | 868 | `declared` (MIT) | 7 KB | S | Declared-license path: add the standard MIT text. |
| [ ] | 17 | `voluntaryhook` | 748 | `copyleft` (AGPL-3.0) | 4 KB | S | Copyleft path. Read the copyleft caveat in LICENSING.md before publishing. |

## Phase 2: High demand, moderate size

The most downloaded mods that do not need a large content or rendering port.

| Done | Rank | Mod | Downloads | License | Package | Effort | Notes |
|---|---:|---|---:|---|---:|---|---|
| [ ] | 23 | `defaultdash` | 598 | `credit-only` | 85 KB | S | Already C#: update the package format instead of porting. |
| [ ] | 7 | `bosscursor` | 1,546 | `credit-only` | 105 KB | S |  |
| [ ] | 29 | `instantplatformfallthrough` | 423 | `open` (MIT) | 976 KB | S |  |
| [ ] | 27 | `overhealth` | 481 | `declared` (AGPL-3.0) | 13 KB | S | Confirm the original's license: it declares AGPL-3.0 on a port of another author's mod. |
| [ ] | 11 | `improvedmovement` | 1,136 | `credit-only` | 44 KB | M | Runs on every frame: check the cost. |
| [ ] | 5 | `weaponoutlite` | 2,226 | `open` (MIT) | 164 KB | M | Draw layers for each item pose. |
| [ ] | 6 | `cooleritemvisualeffect` | 2,063 | `credit-only` | 38 KB | M | Rendering effect: check the cost. |
| [ ] | 8 | `directionalswings` | 1,476 | `declared` (MIT) | 1.3 MB | M | Changes melee hitboxes: test in multiplayer. |
| [ ] | 9 | `better-reforge` | 1,400 | `credit-only` | 1.5 MB | M | Replaces a game UI. |
| [ ] | 13 | `lootbeams` | 977 | `credit-only` | 115 KB | M | Draws per dropped item: check the cost. |
| [ ] | 21 | `bunnysocial` | 690 | `credit-only` | 1.6 MB | M | Changes how accessory slots behave. |
| [ ] | 22 | `wikithis` | 646 | `open` (Apache-2.0) | 1.4 MB | M |  |
| [ ] | 25 | `devtools` | 535 | `credit-only` | 221 KB | M | Broad tool: many small features. |
| [ ] | 12 | `orycto` | 1,092 | `credit-only` | 78 KB | M | Performance mod: its gains must be measured again, because hook costs differ in C#. |

## Phase 3: Flagships

Large mods with their own systems, content or rendering. They benefit from what the earlier phases teach.

| Done | Rank | Mod | Downloads | License | Package | Effort | Notes |
|---|---:|---|---:|---|---:|---|---|
| [ ] | 2 | `bosschecklist` | 2,712 | `credit-only` | 961 KB | L | Large UI. Other mods may integrate with it, so keep its public names. |
| [ ] | 4 | `lightsandshadows` | 2,572 | `credit-only` | 258 KB | L | Heavy rendering: the phone frame budget is the main risk. |
| [ ] | 10 | `unifiedstorage` | 1,354 | `open` (MIT) | 456 KB | XL | Storage network, UI and saved data. |
| [ ] | 16 | `mitanpc` | 766 | `open` (MIT) | 2.9 MB | L | Town NPCs with shops, voiced dialogue and items. |
| [ ] | 15 | `gnorpbuilder` | 799 | `credit-only` | 4.7 MB | L | Town NPC and structure generation. |
| [ ] | 39 | `kineticgore` | 308 | `credit-only` | 285 KB | L | Physics engine: the frame budget is the main risk. |
| [ ] | 28 | `touhoupets` | 458 | `copyleft` (GPL-3.0) | 251 KB | L | Copyleft. 19 pets with AI and dialogue. |
| [ ] | 35 | `wildandhorned` | 362 | `copyleft` (GPL-3.0) | 178 KB | L | Copyleft. Check what it needs from the Touhou Little Friend framework. |
| [ ] | 1 | `consolaria` | 3,380 | `open` (MIT) | 26.1 MB | XL | #1 by downloads and the largest package (26 MB). Comes last so it benefits from the API experience, but can start earlier and in slices. |

## Phase 4: Long tail and selected mods

The rest of the top 50, then the mods picked from outside it.

| Done | Rank | Mod | Downloads | License | Package | Effort | Notes |
|---|---:|---|---:|---|---:|---|---|
| [ ] | 32 | `townnpchome` | 373 | `credit-only` | 34 KB | S |  |
| [ ] | 34 | `wmitf` | 371 | `open` (MIT) | 978 KB | S | Confirm the copyright holder (see the audit). |
| [ ] | 37 | `atmospheric-torches` | 316 | `credit-only` | 569 KB | S |  |
| [ ] | 38 | `coloreddamagetypes` | 310 | `credit-only` | 102 KB | S |  |
| [ ] | 40 | `extendedzoom` | 283 | `declared` (MIT) | 8 KB | S |  |
| [ ] | 42 | `disablecorruptionspread` | 246 | `credit-only` | 32 KB | S |  |
| [ ] | 45 | `tooltipicon` | 228 | `credit-only` | 104 KB | S |  |
| [ ] | 47 | `gravity-dont-flip-screen` | 226 | `credit-only` | 1.1 MB | S |  |
| [ ] | 48 | `chestpeek` | 209 | `declared` (GPL-3.0) | 621 KB | S | GPL-3.0 declared, no text: include the standard text. |
| [ ] | 49 | `npcunlockannouncer` | 208 | `credit-only` | 668 KB | S |  |
| [ ] | 26 | `worldgenpreviewer` | 486 | `credit-only` | 138 KB | M | Hooks into world generation. |
| [ ] | 33 | `umamusumevanities` | 372 | `credit-only` | 243 KB | M | Third-party characters (Cygames). The cited permission went to the JS porter, not to this project. |
| [ ] | 41 | `menumedia` | 262 | `open` (MIT) | 139 KB | M | GIF and audio playback in the menu. |
| [ ] | 44 | `terrarchitect` | 237 | `credit-only` | 2.2 MB | L | Many furniture items and tiles. |
| [ ] | 46 | `topdownmode` | 228 | `credit-only` | 2.7 MB | L | Experimental: changes movement and world generation. |
| [ ] | 51 * | `no-useless-pickups` | 166 | `credit-only` | 106 KB | S | Selected: small quality-of-life fix. |
| [ ] | 58 * | `coyote-frames` | 125 | `open` (MIT) | 338 KB | S | Selected: open license, improves movement feel. |
| [ ] | 69 * | `resized-frames` | 90 | `credit-only` | 1.2 MB | S | Selected: visual fix for item frames. |
| [ ] | 57 * | `flatseed` | 126 | `credit-only` | 53 KB | S | Selected: flat world seed, useful for building and for testing other ports. |
| [ ] | 56 * | `universalmap` | 131 | `credit-only` | 69 KB | M | Selected: shared map in multiplayer. |
| [ ] | 61 * | `pingmod` | 118 | `credit-only` | 116 KB | M | Selected: multiplayer diagnostics. |
| [ ] | 60 * | `teleportkeeper` | 122 | `credit-only` | 91 KB | M | Selected: teleport list usable from the map. |
| [ ] | 54 * | `moddifficulty` | 148 | `credit-only` | 47 KB | M | Selected: adds a custom world difficulty. |
| [ ] | 67 * | `displaybase` | 104 | `credit-only` | 283 KB | M | Selected: decoration with item display. |
| [ ] | 59 * | `bestiarydisplays` | 122 | `credit-only` | 162 KB | M | Selected: decoration with animated holograms. |

## Blocked until the authors answer

These mods cannot start until their rights are settled. Record any written permission in `port.json`, as [CONTRIBUTING.md](CONTRIBUTING.md) describes. If an author says no, the mod leaves the plan.

| Asked | Rank | Mod | Downloads | Status | Ask |
|---|---:|---|---:|---|---|
| [ ] | 24 | `isekaid` | 557 | `reserved` | Zayah |
| [ ] | 30 | `lanternslot` | 408 | `reserved` | Nyacro (original mod) and Mantovani (Android adaptation) |
| [ ] | 31 | `tetovanitysets` | 392 | `reserved` | Makeight (original mod). The catalog also credits Ink of the Eternity and Bloa |
| [ ] | 19 | `qualityofbunny` | 722 | `conflict` | VowTz: the manifest says MIT, the license.md says otherwise |

## No code: not a port

These top 50 packages are texture or font packs, so there is no JavaScript to port and `tools/check_licenses.py` rejects them in `mods/`. They still need the package update the loader's [fonts and packs guide](https://github.com/JonataOliveiraa/Bunny-Loader/blob/main/docs/mods-em-csharp/22-fontes-e-pacotes.md) describes, which belongs in the catalog.

| Rank | Mod | Downloads | Package |
|---:|---|---:|---:|
| 18 | `boss-colored-relics` | 738 | 182 KB |
| 20 | `darkfantasyhealthmana` | 713 | 17 KB |
| 36 | `fontecomicneue` | 341 | 762 KB |
| 43 | `dont-starve-inspired-npc-portraits` | 246 | 1.3 MB |

## Not selected

Outside the top 50 and left out for now. This is a judgment call: any of them can move into Phase 4.

| Rank | Mod | Downloads | Why |
|---:|---|---:|---|
| 52 | `fonte-ringbearer` | 155 | Font pack: no code. |
| 53 | `abusegoblin` | 150 | Gag mod with a narrow use. |
| 55 | `gorilla` | 132 | Meme mod, and its original creator is not identified. |
| 62 | `templopiramide` | 117 | Loader sample with no license text. The loader's migration guide tracks it. |
| 64 | `vidacheia` | 108 | Loader sample for a cheat. The loader's migration guide tracks it. |
| 65 | `npcfalldamage` | 107 | Gag mod with a narrow use. |
| 66 | `ropemod` | 106 | Gag mod with a narrow use. |
| 68 | `fonte-aclonica` | 100 | Font pack: no code. |
| 70 | `sharedinventory` | 87 | Narrow use: shares the host's whole inventory. |
| 72 | `mc-paintings` | 84 | Decoration based on Minecraft art. |
| 73 | `fonte-pescadero` | 80 | Font pack: no code. |
| 74 | `fonte-ale-and-wenches` | 63 | Font pack: no code. |
| 75 | `picassonpcportraits` | 34 | Joke texture mod, the least downloaded in the catalog. |
