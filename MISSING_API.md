# Missing API

Members the ports wanted but the C# API does not expose. Each entry records how the
port works around the gap, so the workaround can go away if the API gains the member.

The ports were first written against `BunnyLoader.dll` from Bunny Loader 0.5.2-beta.
They now build against a newer development build of the loader (build 541, which
includes "fix(csharp): API gaps found by the mod ports"), and most of the rows below
were closed by it. See [Resolved](#resolved-by-the-newer-loader).

When a port needs something that is not there, add a row here and mention it in the
port's `port.json`.

**Status:** `works` ran on a phone. `unverified` only compiles: it binds by name at run
time, so a wrong name fails only in game. `missing` has no workaround.

## Classes with no C# proxy

These are reached by name through `GameInterop`. A typed member on each would remove the
workaround.

| Missing | Wanted by | Used for | Workaround | Status |
|---|---|---|---|---|
| `UnityEngine.Application.OpenURL` | `wikithis` | Opening the wiki | `GameInterop.Method` plus `Call` | works |
| `UnityEngine.Application.targetFrameRate` | `orycto` | Frame rate cap | `Engine.cs`, by name | No error logged, but the test phone stayed near 60 FPS |
| `UnityEngine.Input` touch and mouse members, `UnityEngine.Touch` | `orycto`, `worldgenpreviewer` | Running full updates on tap frames; dragging the preview | By name; the phase and position are read from `Touch.m_Phase` and `m_Position` | No error logged, touch unverified |
| `UnityEngine.Screen` | `worldgenpreviewer` | Screen size for the preview | By name | unverified: needs a new world |
| `ItemsSacrificedUnlocksTracker.TryGetSacrificeNumbers` (the tracker's proxy has no members) | `bosschecklist` | Journey research marks | `GameInterop.Method` on the tracker read through `CreativeUnlocksTracker.ItemSacrifices` | unverified |
| `NPCNetIdBestiaryInfoElement` | `bestiarydisplays` | Finding a creature's entry | Entries listed by NPC id | works |
| A hook on `PingMapLayer` | `teleportkeeper` | Drawing waypoints with the map pings | The mod's own map layer | None expected |

## Methods and members

| Missing | Wanted by | The JavaScript version used | Workaround in the port | Effect on players |
|---|---|---|---|---|
| `Mod.dataDirectory` | `veinminer`, `lootbeams`, `weaponoutlite`, `teleportkeeper` | The folder `mod_data/<uid>` for the mod's own files | `veinminer` and `lootbeams`: a file in `Main.SavePath`. `weaponoutlite`: rebuilds the path from `Mod.Folder` | `veinminer` and `lootbeams` do not read files saved by the JavaScript version |
| `RarityLoader.ColorOf(int)` (not public) | `lootbeams` | Mod rarity colors | `GetRarity(rare).RarityColor` | None |
| `Liquid.Update()`, `WorldMap.Update()` with no arguments | `orycto` | The liquid and minimap throttle options | None. The game only has the per-cell `Liquid.Update(ref TileDataPointers)` and per-tile `WorldMap.Update(x, y, light)`, which the JavaScript did not hook | The options do nothing, as in JavaScript |
| `Main.UpdateNPC(int)`, `Wiring.Update()` | `orycto` | Three throttle options | None. They are not in the game, so these options never worked in JavaScript either | The options do nothing |
| `Dust.SetNotActive(int)` | `orycto` | Removing dust | `.active = false`, which is what the JavaScript fell back to: the game has `SetNotActive(Dust)` | None |
| `Lighting.PreRenderPhase`, `Main.DrawPlayers`, `Main.DrawWater`, two `DrawRain` overloads | `orycto` | Profiler probes | Dropped | None |
| `Asset<T>.Value` setter | `wikithis` | Swapping slot textures | Swapping `TextureAssets.*` entries for one draw | None expected |
| `Player.ApplyDamageToNPC(NPC, int, float, int, bool, DamageClass, bool)` (tModLoader signature; the game has the vanilla one) | `cooleritemvisualeffect` | Applying swing damage | `NPC.StrikeNPC`, which is what the JavaScript fell back to | Skips banner and DPS handling, and `OnHitNPC` hooks |
| `Main.notTheBees`, `Main.worldNotTheBees`, `Main.forTheWorthy` | `cooleritemvisualeffect` | World flag checks | `Main.getGoodWorld` for one; the other branch stays off, as in JavaScript | None |
| `ModCommand.Aliases` | `weaponoutlite` | The `/weaponout` alias | A second command class | Listed twice in `/help` |
| `ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY` | `weaponoutlite` | Flail classification | Treated as false | Same as JavaScript on the phone |
| A hook on `LegacyGameInterfaceLayer.DrawSelf` | `devtools` | Hiding other layers in photo mode | `ModifyInterfaceLayers` with `Active = false` | Photo mode applies one frame later |
| `NPC.netSkip`, `MessageID.SpiritHeal`, `MessageID.AddPlayerBuff` | `consolaria` | Multiplayer sync | Removed, or `Unknown66` and `AddPlayerBuffPvP` | None in single player; unverified in multiplayer |
| `MusicID.ConsoleMenu`, `ItemID.Sets.SortingPriorityBossSpawns` | `consolaria` | Menu theme, creative sorting | `TitleClassic`; removed | A different menu theme, and sort order |
| `Main.GetPlayerArmPosition`, `Chest.FindChestByGuessing`, `SceneMetrics.ExportTileCountsToMain`, `NPC.SpawnWithHigherTime`, `Main._rand`, `Item.SetDefaults(int)`, `GetSource_Misc`, `GetSource_OpenItem`, `GetSource_ItemUse`, `NextFromList`, `BitsByte` members, boxing numbers into `Dust.customData`, `Utils` extension methods | `consolaria` | Upstream tModLoader code | Small helpers in `Common/PhoneCompat.cs` and inline code | None seen |
| `ModBuff.OnRemove` | `mitanpc` | A sound when a buff ends | `ModBuff.RightClick` | The sound plays just before removal |
| `Item.master`, `Item.masterOnly`, `Item.NPCtoBanner`, `RecipeGroup.IconicItemId`, `Main.LoadExtra`, `SpriteBatch._samplerState` | `bosschecklist` | Mode checks, banners, group names, textures | `rare == -13`, `BannerSystem`, `Recipe.ProcessGroupsForText`, `TextureAssets.Extra`, `Main.DefaultSamplerState` | None |
| `GlobalNPC.SetupShop(type, shop, nextSlot)` | `terrarchitect` | Adding priced items to vanilla shops | `ModifyShop` | The Painter's items appear in both his shops |
| `ModTileEntity.ByID` (internal) | `bestiarydisplays` | Listing placed holograms | The mod's own list | None expected |
| A hook on `UserInterface.SetState` (the method exists, the hook does not) | `pingmod` | Closing panels when another mod opens a screen | Watching `UserInterface.ActiveInstance` | Panels can stay open over some screens |
| `ModConfig.OnChanged(key)` (only `OnChanged()` exists) | `coloreddamagetypes`, `weaponoutlite` | Knowing which option changed | Comparing with the last value | None |
| Texture wrap and filter modes on `Texture2D` | `teleportkeeper` | Setting the icon textures' wrap and filter | The game's default modes | Icons may look slightly different |

## Features and libraries

| Missing | Wanted by | Workaround in the port | Effect on players |
|---|---|---|---|
| A button option in `ModConfig` (`ModConfig.Button` in JavaScript) | `veinminer`, `lootbeams`, `weaponoutlite` | Commands: `/vein clear`, `/beams clear`, `/wo next`, `/wo turn`, `/wo clear` | The config screen has no buttons |
| `ModConfig` value suffixes, `enabledWhen`, and header `color` and `align` | `overhealth`, `devtools`, `weaponoutlite`, `cooleritemvisualeffect`, `chestpeek` | Plain options | Sliders show no `%` or `x`, and dependent options are not greyed out |
| `ModConfig.Cycle` with labels separate from the values | `lootbeams` | `[OptionStrings]` using the labels as values | An old saved `MinValue` resets |
| `ModConfig.SetOption`, `OnChanged(key)` | `weaponoutlite` | `Clone()` with `SaveChanges`, and a stored last value | None |

## Resolved by the newer loader

These were missing or broken in 0.5.2-beta. The ports now use the typed API, and ran
on the MuMu emulator with loader build 541: a test mod checked each binding below by code,
the same checks failed with the ports off, and all 53 ports loaded together with no error.
They have not run on a phone with this build yet.

| Now in the API | Ports that use it | Replaced |
|---|---|---|
| `Reforge_Layout.Instance`, `RightPageController` | `better-reforge` | Getter calls by name |
| `InventorySplitStack_Layout.Instance`, `MainButton`, `On_GUIInventorySplitStack.Draw` | `wikithis` | `Hook.Install` and field offsets |
| `Game.FrameTickTime`, `FrameClampTickTime`, `FrameIgnoreTime` | `orycto` | `ReadStatic` and `WriteStatic` |
| `On_XNAWorldCameraSettings` (`ApplyCamera`, `RecalculateVirtualScreen`, `get_MaxPixelScale`, `get_MaxWorldWidth`, `get_MaxWorldHeight`), `_zoomScale`, `ZoomScale` | `devtools`, `extendedzoom` | `Hook.Install` and offsets |
| `GUIZoom.ApplyPixelZoom`, `PinchZoomMapEnabled`, `On_GUIZoom` | `extendedzoom` | `Hook.Install`, `CallFrame` and an offset |
| `CreativePowerManager.Instance`, `_powersByName`, `ASharedTogglePower.Enabled`, `SetPowerInfo`, `ModifyTimeRate.TargetTimeRate` | `devtools` | Calls and backing fields by name |
| `FinalFractalHelper._fractalProfiles`, `FinalFractalProfile` | `consolaria` | A raw constructor call, which also stored the profile wrongly |
| `StructureMap.CanPlace`, `AddStructure`, `_structures`, `_protectedStructures` | `consolaria`, `worldgenpreviewer` | Raw calls and offsets |
| `TEFoodPlatter.item` | `gnorpbuilder` | An offset |
| `BirthdayParty.PartyIsUp` | `touhoupets` | A getter by name |
| `WorldFileData.UniqueId`, `PlayerFileData.GetPlayTime()`, `CreativeUnlocksTracker.ItemSacrifices` | `bosschecklist` | An offset and a call by name |
| `On_TorchID.TorchColor` | `atmospheric-torches` | `Hook.Install` |
| `On_SmartCursorHelper.SmartCursorLookup` | `gravity-dont-flip-screen` | `Hook.Install` |
| `RemoteConnection.State` | `pingmod`, `universalmap` | An offset |
| `WorldMap` (`IsRevealed`, `SetTile`, `_chunks`, `MaxChunkWidth`, `MaxWidth`, `MaxHeight`, `Allocate`, `Update`, `Clear`, `Load`), `WorldMapChunk`, `MapTile.Create` | `universalmap`, `topdownmode`, `worldgenpreviewer` | Raw calls, offsets, and a hand-filled `MapTile` |
| `WorldCreateMenu_Layout`, `WorldSelectMenu_Layout.ItemMode` | `moddifficulty` | Static and offset reads |
| `On_GUIWorldGenerateMenu.Draw`, `On_GUIStatusMenu.Draw`, `WorldGenerateMenu_Layout`, `ProgressBar_Layout`, `GUIProgressBar.DrawFilling2` | `worldgenpreviewer` | `Hook.Install`, offsets and a hand-built call |
| `IBestiaryUICollectionInfoProvider.GetEntryUICollectionInfo` | `bestiarydisplays` | A method bound per implementing class |
| `HitTile.AddDamage`, `On_HitTile.AddDamage`, `HitTileObjectData` | `veinminer` | `On_Player.PickTile` with a before and after compare |
| `ModItem.Update` | `consolaria` | `PostUpdate` (Rainbow Piece) |
| `Recipe.SortAfter`, `SortAfterFirstRecipesOf`, `SortBeforeFirstRecipesOf` | `consolaria` | Do-nothing extensions |
| `TileLoader.CanExplode`, `WallLoader.CanExplode` | `consolaria` | Asking the `ModTile` or `ModWall` directly |
| `BigProgressBarSystem.NeverValid` | `consolaria` | A `ModBossBar` of the port's own |
| `SoundID.Zombie26`-`29`, `ScaryScream` | `consolaria` | `PlaySound(SoundID.Zombie, pos, N)` |
| `Condition.DownedMoonLord` and the other tModLoader conditions | `consolaria`, `gnorpbuilder` | Hand-made conditions |
| Mood text of mod NPCs from `Mods.<Mod>.NPCs.<NPC>.TownNPCMood` | `gnorpbuilder`, `mitanpc` | A hook on `AddHappinessReportText`, or root keys `TownNPCMood_<mod>/<NPC>` |
| `System.Text.Json` on the phone | `gnorpbuilder`, `menumedia`, `mitanpc`, `weaponoutlite` | Small hand-written parsers |

Loader issues found while porting, now fixed in the loader:

| Issue | Seen in | What the port does now |
|---|---|---|
| `Recipe.AddIngredient` on one of the game's own recipes wrote to the empty slot object that all the game's recipes share | `consolaria` (Tizona in the Zenith recipe) | Keeps `requiredItem[slot] = Item.New(type, default)`, which is still correct |
| Content classes were built before `Main.rand` and similar existed, so a field initializer using them stopped the whole mod from loading | `consolaria` | Keeps the values set in `OnSpawn` or read lazily |
| `new Color[3] { ... }` assigned to `ItemID.Sets.FoodParticleColors` threw `NotSupportedException` | `consolaria` | Keeps the collection expressions |
| `[AutoloadEquip]` built its path from the namespace with no fallback | `umamusumevanities` | Keeps the mod id as the namespace root |

Other rows that turned out not to be needed: `HitTile.data` (`veinminer` reads
`HitTileObjectData`), `Main.notTheBeesWorld`, `Dust.SetNotActive(Dust)` and the vanilla
`Player.ApplyDamageToNPC` exist, but the JavaScript versions asked for other signatures,
so the ports keep what the JavaScript did on the phone.

## Loader issues still open

| Issue | Seen in | Effect | Workaround in the port |
|---|---|---|---|
| `ModContent.Request` and `EquipLoader.FileOf` now find a mod whatever the case of its name, but `Mod.AddNPCHeadTexture` (`NPCHeadLoader.File`) still strips `Mod.Name + "/"` case-sensitively | `consolaria` | `Consolaria/...` head textures are not found, and the whole mod fails to load | `Common/PhoneCompat.cs` lowercases the first path segment of default textures |
