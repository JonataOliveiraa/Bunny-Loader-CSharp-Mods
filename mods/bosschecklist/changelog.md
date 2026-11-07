## 2.0.0

Ported from JavaScript to C#. Multiplayer packets are written as binary instead of JSON text, so this version does not talk to a JavaScript client. The map icons use a ModMapLayer instead of a hook on PingMapLayer.Draw. WorldFileData.UniqueId, PlayerFileData.GetPlayTime and ItemsSacrificedUnlocksTracker.TryGetSacrificeNumbers have no C# proxy and are bound by name at run time; if one fails it is logged once, and records fall back to Main.worldID as the world key, the first victory play time to 1, and the Journey research marks are not shown. Event pages now show their banner grid: the JavaScript version called Item.NPCtoBanner, which the phone game does not have, so its grid stayed empty; this port uses BannerSystem. Recipe ingredients now show their group name through Recipe.ProcessGroupsForText: the JavaScript lookup used RecipeGroup members the phone game does not have, so it always showed the item name. The texture wrap and filter settings were dropped because the loader already sets them on mod textures. A closed Boss Log leaves its interface without a state, so opening it blocks clicks for the loader's short state-change delay. Call keeps its messages and arguments for C# mods: downed and availability are Func<bool>, texts are string, LocalizedText or a Func of them, ids are a number or any list of numbers, extras are a Dictionary<string, object>; the mod argument must be a Mod. The description's example is now in C#.

# Changelog

## 1.1.1

- Fixed the Boss Log not opening on multiplayer clients: textures stay valid
  after joining a server.
- Description and changelog in every Terraria language.

## 1.1.0

- Interface rebuilt for touch with Bunny Loader's new UI: touch buttons open
  the Boss Log and the checklist, a tap opens an entry and a long-press marks
  it as defeated.
- Times shown in days, hours, minutes and seconds.

## 1.0.2

- Fixed some texts that showed incorrectly.

## 1.0.1

- Fixed some texture bugs.

## 1.0.0

- First release.
