## 2.0.0

Ported from JavaScript to C# on 2026-11-06, with the same stats, shops, prices, dialogue, voices and town melee behavior. Mood lines move to the game's own TownNPCMood_mitanpc/<NPC> keys in Localization/<culture>.hjson, with the game's {BiomeName} and {NPCName} placeholders, because the C# loader has no mapping from TownNPCMood.<NPC> as the JavaScript loader had. The love for the other resident is set in SetStaticDefaults instead of a PostSetupContent step. The Fly's removal sound plays when the buff icon is tapped, just before the removal instead of just after. Dialogues.json is read with a small scanner because the phone has no System.Text.Json.

# 1.0.2

- Add complete Brazilian Portuguese and Spanish localization, including all town NPC dialogue.
- Complete Russian projectile names and revise all item descriptions in English and Russian.
- Replace tModLoader tooltip references and unfilled parameters with explicit bonuses and durations.
- Fix the Game Cartridge return collision call by selecting the Rectangle.Intersects overload explicitly.

# 1.0.1

- Restore the native walking and idle animation for all four town NPCs.
- Keep melee attack frames limited to drawing so they do not interrupt the walking cycle.

# 1.0.0

- Initial Bunny Loader port of Mita NPC 0.0.9.
- Four town NPCs, 12 items, two pet buffs and the original sound assets.
- Travel Ring touch control, permanent manual saves and mobile town melee combat.
- English and Russian localization from the original mod.
