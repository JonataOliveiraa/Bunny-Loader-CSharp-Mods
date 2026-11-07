# Missing API

Members the ports wanted but the C# API does not expose, checked against
`BunnyLoader.dll` from Bunny Loader 0.5.2-beta. Each entry records how the port works
around the gap, so the workaround can go away if the API gains the member.

When a port needs something that is not there, add a row here and mention it in the
port's `port.json`.

**Status:** `works` ran on a phone. `unverified` only compiles: it binds by name at run
time, so a wrong name fails only in game. `missing` has no workaround.

## Classes with no C# proxy

These are reached by name through `GameInterop`, or hooked with `Hook.Install`. A hook
in `On_*` and a typed member on each would remove the workaround.

| Missing | Wanted by | Used for | Workaround | Status |
|---|---|---|---|---|
| `Reforge_Layout`, including `Instance` and `RightPageController` | `better-reforge` | Choosing the panel's page side | `GameInterop.Method` on the getters. `Instance` is a property here, not a field as on proxied layouts | works |
| `InventorySplitStack_Layout`, including `Instance` and `MainButton` | `wikithis` | Anchoring the wiki slot | `get_Instance()` plus a field offset | works |
| `On_GUIInventorySplitStack`, `GUIInventorySplitStack.Draw` | `wikithis` | Drawing the slot after that control | `Hook.Install` with an `[UnmanagedCallersOnly]` trampoline | No error logged, slot not yet seen on screen |
| `UnityEngine.Application.OpenURL` | `wikithis` | Opening the wiki | `GameInterop.Method` plus `Call` | works |
| `UnityEngine.Application.targetFrameRate` | `orycto` | Frame rate cap | `Engine.cs`, by name | No error logged, but the test phone stayed near 60 FPS |
| `UnityEngine.Input` touch and mouse members | `orycto` | Running full updates on tap frames | `Engine.cs`, by name; the phase is read from `Touch.m_Phase` | No error logged, touch phase unverified |
| `Game.FrameTickTime`, `FrameClampTickTime`, `FrameIgnoreTime` | `orycto` | The faster game loop | `GameInterop.ReadStatic` and `WriteStatic` | works |
| `XNAWorldCameraSettings.ApplyCamera`, `RecalculateVirtualScreen`, `On_XNAWorldCameraSettings` | `devtools` | Applying zoom, and turning zoom input into steps | `Hook.Install` | No error logged, zoom on screen unverified |
| `DeviceVPPICalculator._zoomScale`, `ZoomScale` | `devtools` | Reading and writing zoom | `GameInterop.Offset` | unverified |
| `CreativePowerManager.Instance`, `_powersByName`, `ASharedTogglePower.Enabled`, `SetPowerInfo`, `ModifyTimeRate.TargetTimeRate` | `devtools` | Time freeze and time speed | `CallFrame`, `GameDictionary` and backing fields | Freeze works, speed unverified |
| `FinalFractalHelper._fractalProfiles`, `FinalFractalProfile` | `consolaria` | Tizona in the Zenith animation | Raw constructor call and `GameDictionary` | No error logged, animation not yet seen |
| `StructureMap.CanPlace`, `AddStructure` | `consolaria` | Placing the Heart Shrine and Jungle Sanctum | Raw `GameInterop` call | unverified: needs a new world |
| `TEFoodPlatter.item` | `gnorpbuilder` | Food on the bar room's platters | `GameInterop.Offset` | unverified |
| `BirthdayParty.PartyIsUp` | `touhoupets` | A party dialogue line | `get_PartyIsUp()` by name | works |
| `WorldFileData.UniqueId`, `PlayerFileData.GetPlayTime()` | `bosschecklist` | World key and play time in records | Field offset and `GameInterop.Method` | works |
| `CreativeUnlocksTracker.ItemSacrifices`, `TryGetSacrificeNumbers` | `bosschecklist` | Journey research marks | Offset and method call | unverified |
| `Terraria.ID.TorchID.TorchColor` hook | `atmospheric-torches` | Retinting torches | `Hook.Install` | No error logged, tint not yet seen |
| `SmartCursorHelper.SmartCursorLookup` hook | `gravity-dont-flip-screen` | Smart cursor while gravity is flipped | `Hook.Install` | No error logged |
| `XNAWorldCameraSettings.get_MaxPixelScale`, `get_MaxWorldWidth`, `get_MaxWorldHeight`, `GUIZoom.ApplyPixelZoom`, `GUIZoom.PinchZoomMapEnabled` | `extendedzoom` | Zoom limits, smooth zoom, pinch | `Hook.Install`, `CallFrame`, `GameInterop.Offset` | No error logged, zoom not yet seen |
| `RemoteConnection.State` | `pingmod`, `universalmap` | Not sending before a client has joined | Raw offset | unverified: needs two devices |
| `WorldMap.IsRevealed`, `SetTile`, `_chunks`; `WorldMapChunk.*` | `universalmap` | Reading and writing the map | Raw calls | Read and write checks work; sharing needs two devices |
| `WorldCreateMenu_Layout`, `WorldSelectMenu_Layout` | `moddifficulty` | The difficulty list | Raw static and offset reads | Reads work, list not yet seen |
| `GUIWorldGenerateMenu.Draw`, `GUIStatusMenu.Draw`, `WorldGenerateMenu_Layout`, `ProgressBar_Layout`, `GUIProgressBar.DrawFilling2`, `StructureMap._structures` | `worldgenpreviewer` | The live preview screen | `Hook.Install` and raw calls | Bars read works; needs a new world |
| `IBestiaryUICollectionInfoProvider.GetEntryUICollectionInfo` | `bestiarydisplays` | Which creatures are unlocked | `GameInterop.Method` per class | works |
| `WorldMap.Clear`, `WorldMap.Load` | `topdownmode` | Resetting the map when entering or leaving the cave | By name | unverified: needs its own world |

## Methods and members

| Missing | Wanted by | The JavaScript version used | Workaround in the port | Effect on players |
|---|---|---|---|---|
| `Terraria.HitTile.AddDamage(int tileId, int damageAmount, bool updateAmount)` and `On_HitTile.AddDamage` | `veinminer` | A hook that fires when a hit brings a tile to 100% damage | `On_Player.PickTile`: compare the tile before and after the call | None seen. Breaks that do not go through `PickTile` are not detected |
| `Terraria.HitTile.data`, `HitTileObjectData` | `veinminer` | Finding the position of the tile that was hit | `PickTile` already receives `x` and `y` | None |
| `Mod.dataDirectory` | `veinminer`, `lootbeams`, `weaponoutlite` | The folder `mod_data/<uid>` for the mod's own files | `veinminer` and `lootbeams`: a file in `Main.SavePath`. `weaponoutlite`: rebuilds the path from `Mod.Folder` | `veinminer` and `lootbeams` do not read files saved by the JavaScript version |
| `RarityLoader.ColorOf(int)` (internal in C#) | `lootbeams` | Mod rarity colors | `GetRarity(rare).RarityColor` | None |
| `Liquid.Update`, `On_Liquid` | `orycto` | The liquid throttle option | `Hook.Install`, which fails: `void Update()` is not found in game | The option does nothing |
| `WorldMap.Update`, `On_WorldMap` | `orycto` | The minimap throttle option | `Hook.Install`, which fails the same way | The option does nothing |
| `Main.UpdateNPC(int)`, `Wiring.Update()` | `orycto` | Three throttle options | None. They are not in the game, so these options never worked in JavaScript either | The options do nothing |
| `Dust.SetNotActive(int)` | `orycto` | Removing dust | `.active = false` | None |
| `Lighting.PreRenderPhase`, `Main.DrawPlayers`, `Main.DrawWater`, two `DrawRain` overloads | `orycto` | Profiler probes | Dropped | None |
| `Asset<T>.Value` setter | `wikithis` | Swapping slot textures | Swapping `TextureAssets.*` entries for one draw | None expected |
| `Player.ApplyDamageToNPC(NPC, int, float, int, bool, DamageClass, bool)` | `cooleritemvisualeffect` | Applying swing damage | `NPC.StrikeNPC` | Skips banner and DPS handling, and `OnHitNPC` hooks |
| `Main.notTheBees`, `Main.worldNotTheBees`, `Main.forTheWorthy` | `cooleritemvisualeffect` | World flag checks | `Main.getGoodWorld` for one; the other branch stays off, as in JavaScript | None |
| `ModCommand.Aliases` | `weaponoutlite` | The `/weaponout` alias | A second command class | Listed twice in `/help` |
| `ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY` | `weaponoutlite` | Flail classification | Treated as false | Same as JavaScript on the phone |
| A hook on `LegacyGameInterfaceLayer.DrawSelf` | `devtools` | Hiding other layers in photo mode | `ModifyInterfaceLayers` with `Active = false` | Photo mode applies one frame later |
| `ModItem.Update` | `consolaria` | Rainbow Piece vanishing at night | `PostUpdate` | It also vanishes while being pulled to a player |
| `Recipe.SortAfter`, `SortAfterFirstRecipesOf`, `SortBeforeFirstRecipesOf` | `consolaria` | Recipe order | Do-nothing extensions | Only the order of the recipe list |
| `NPC.netSkip`, `MessageID.SpiritHeal`, `MessageID.AddPlayerBuff` | `consolaria` | Multiplayer sync | Removed, or `Unknown66` and `AddPlayerBuffPvP` | None in single player; unverified in multiplayer |
| `TileLoader.CanExplode`, `WallLoader.CanExplode` | `consolaria` | Holy Hand Grenades | Ask the `ModTile` or `ModWall` directly | Other mods' global hooks are not asked |
| `BigProgressBarSystem.NeverValid`, `MusicID.ConsoleMenu`, `ItemID.Sets.SortingPriorityBossSpawns` | `consolaria` | Boss bar, menu theme, creative sorting | A `ModBossBar`, `TitleClassic`, removed | A different menu theme, and sort order |
| `SoundID.Zombie26`-`29`, `ScaryScream` | `consolaria` | ArchDemon and Old Lady sounds | `PlaySound(SoundID.Zombie, pos, N)` | Could be the wrong sound: not checked by ear |
| Mood text for mod NPCs: the loader never reads `TownNPCMood` keys | `gnorpbuilder`, `mitanpc` | Town NPC happiness lines | A hook on `AddHappinessReportText`, or root keys `TownNPCMood_<mod>/<NPC>` | None seen |
| `ModBuff.OnRemove` | `mitanpc` | A sound when a buff ends | `ModBuff.RightClick` | The sound plays just before removal |
| `Condition.DownedMoonLord` | `gnorpbuilder` | A shop condition | A `Condition` on `NPC.downedMoonlord` | None |
| `Item.master`, `Item.masterOnly`, `Item.NPCtoBanner`, `RecipeGroup.IconicItemId`, `Main.LoadExtra`, `SpriteBatch._samplerState` | `bosschecklist` | Mode checks, banners, group names, textures | `rare == -13`, `BannerSystem`, `Recipe.ProcessGroupsForText`, `TextureAssets.Extra`, `Main.DefaultSamplerState` | None |
| `MapTile.Create` | `universalmap` | Building a map tile | The struct's fields are filled directly | None seen |
| `GlobalNPC.SetupShop(type, shop, nextSlot)` | `terrarchitect` | Adding priced items to vanilla shops | `ModifyShop` | The Painter's items appear in both his shops |
| `ModTileEntity.ByID` (internal) | `bestiarydisplays` | Listing placed holograms | The mod's own list | None expected |
| A hook on `UserInterface.SetState` | `pingmod` | Closing panels when another mod opens a screen | Watching `UserInterface.ActiveInstance` | Panels can stay open over some screens |
| `ModConfig.OnChanged(key)` | `coloreddamagetypes`, `weaponoutlite` | Knowing which option changed | Comparing with the last value | None |

`Terraria.HitTile` has no game members in the C# API: only `From`, `Is`, `IsNull`,
`Class`, `Pointer` and the base object methods.

## Features and libraries

| Missing | Wanted by | Workaround in the port | Effect on players |
|---|---|---|---|
| A button option in `ModConfig` (`ModConfig.Button` in JavaScript) | `veinminer`, `lootbeams`, `weaponoutlite` | Commands: `/vein clear`, `/beams clear`, `/wo next`, `/wo turn`, `/wo clear` | The config screen has no buttons |
| `ModConfig` value suffixes, `enabledWhen`, and header `color` and `align` | `overhealth`, `devtools`, `weaponoutlite`, `cooleritemvisualeffect` | Plain options | Sliders show no `%` or `x`, and dependent options are not greyed out |
| `ModConfig.Cycle` with labels separate from the values | `lootbeams` | `[OptionStrings]` using the labels as values | An old saved `MinValue` resets |
| `ModConfig.SetOption`, `OnChanged(key)` | `weaponoutlite` | `Clone()` with `SaveChanges`, and a stored last value | None |
| `System.Text.Json` on the phone | `veinminer`, `lootbeams`, `weaponoutlite` | Plain text files, or a small parser | None, except the old files above |

## Loader issues found while porting

These behave differently from tModLoader. They are worth fixing in Bunny Loader, because any mod can hit them.

| Issue | Seen in | Effect | Workaround in the port |
|---|---|---|---|
| `Recipe.AddIngredient` on one of the game's own recipes writes to the empty slot object, and all of the game's recipes share that object | `consolaria` (Tizona in the Zenith recipe) | Every empty slot of every recipe becomes that item, `UpdateMaterialFieldForAllRecipes` throws, and players see an error box | `requiredItem[slot] = Item.New(type, default)` |
| Content classes are built during load, before the game exists | `consolaria` | A field initializer using `Main.rand` or `Main.DiscoColor`, or a `Tooltip` reading `Main.ReversedUpDownArmorSetBonuses`, stops the whole mod from loading | Set these in `OnSpawn`, or read them lazily |
| Assigning `new Color[3] { ... }` to `ItemID.Sets.FoodParticleColors` | `consolaria` | `NotSupportedException: Color` | A collection expression, `[ ... ]` |
| `[AutoloadEquip]` builds its path from the namespace with no fallback, unlike default textures | `umamusumevanities` | With a namespace root other than the mod id, the whole mod fails to load | Use the mod id as the namespace root |
| `ModContent.Request` and `EquipLoader.FileOf` compare `Mod.Name` case-sensitively | `consolaria` | Paths from a tModLoader mod fail when the id differs in case | `Common/PhoneCompat.cs` lowercases the first path segment |
