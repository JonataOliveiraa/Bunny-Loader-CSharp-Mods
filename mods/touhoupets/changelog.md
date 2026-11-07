## 2.0.0

Ported from JavaScript to C# on 2026-11-06, with the same pets, numbers, dialogue, drops and recipes. Pets, buffs and items bind to each other at compile time instead of by name. Differences: BirthdayParty has no C# proxy, so Iku's party line reads its PartyIsUp by name (if that ever fails, she never mentions a party); Journey research counts use Item.ResearchUnlockCount instead of writing the game's table directly.
