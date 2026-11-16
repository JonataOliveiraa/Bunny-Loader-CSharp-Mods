## 2.2.5

Lighter on the phone. Stunned no longer makes every NPC, vanilla ones included, enter C# three times per update: the stun is read from the NPC's buffs in its AI hook, and the buff's NPC update, the reset and the empty frame hook are gone. Removed hooks that did nothing: the body glowmask of the Titan Mail, which colored a glowmask the armor does not have, an empty world update, and empty kite and McMoneypants AI hooks. The Titan set bonus text is built once per language instead of on every update, the Titan glowmasks find their slots without a lookup by name, and Rainbow Rain picks its drop position only when a piece drops.

## 2.2.4

Moved off the port's workarounds onto the typed Bunny Loader API. Recipes are sorted as upstream does it. Rainbow Piece vanishes at night from ModItem.Update, as upstream. The Holy Hand Grenades ask TileLoader.CanExplode and WallLoader.CanExplode, so other mods' GlobalTile and GlobalWall hooks too. Turkor's head uses the never valid boss bar, as upstream. The Arch Demon and the Old Lady pet play upstream's sounds. Tizona's Zenith profile and the Heart Shrine and Jungle Sanctum placement use typed calls.

## 2.2.3

Ported from the original tModLoader C# source (Consolaria 2.2.3, commit 9d039e7), replacing the JavaScript package 1.0.2 with the same uid and id. New from the C# source: the Mythical Wyvern kite, Soul of Blight in a Bottle, Dread Skull, the Topper 505 and Star Topper 4 tree toppers, Orca, the full Old Lady pet and the Worm pet's voice lines. Saved names keep the JavaScript ones: ConsolariaPetItem and the KillLepus, KillOcram and KillTurkor achievements. Everything saved under another class name in the JavaScript package was already the same in C#. Not ported, because those mods do not exist on the phone: Content/Crossmod (Thorium, Rise of Ages), the download buttons, CrossContentIntegration (BossChecklist, Fargos, Munchies, MusicDisplay, Thorium) and the Heartbeataria drops of the Red Envelope with their config option. Without Heartbeataria the Red Envelope never gave the kite in upstream either. Phone changes: natural spawn replacement goes through NPC.Spawner (from the JavaScript package) instead of a hook on NPC.NewNPC per NPC. The vanilla Pyramids pass stays, and Consolaria's second room hooks WorldGen.Pyramid with the phone's signature, as the JavaScript package did; nothing generates in a skyblock world. The Pyramid config still forces the extra desert pyramid, in a small pass before the vanilla one. Rainbow Piece's vanishing at night moved from ModItem.Update, which does not exist here, to PostUpdate, so it also vanishes while being pulled to a player. Graveyard tile counts use ModSystem.TileCountsAvailable instead of walking the tiles around the player. Turkor's head bar follows the body. Consolaria's menu plays the classic title theme, because the phone has no console menu theme. Recipe.SortAfter and friends do not exist, so recipes stay in the order they are added. Boss summon items keep the default creative sort, because ItemID.Sets.SortingPriorityBossSpawns does not exist. NPC.netSkip does not exist, so eggs and McMoneypants sync on the normal schedule in multiplayer. Mod tile and wall CanExplode are asked directly, so GlobalTile and GlobalWall CanExplode hooks of other mods are not asked by the Holy Hand Grenades. Added from the JavaScript package: the wiki link of Wikithis and the sprite turn of WeaponOut Lite. Not kept from it: the graphics quality, reduce dusts and Turkor neck options, and the Like a Boss achievement patch. Chinese translations come from the JavaScript package.

## 1.0.2 - 2026-10-22
- General fixes and improvements

## 1.0.1 - 2026-10-21
- Fixed some missing translations
- Fixed Turkor boss music

## 1.0.0 - 2026-10-01
- Initial release, ported from the original Consolaria 2.0 mod
