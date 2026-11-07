using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Events;

namespace BossChecklistMod.Common;

public sealed class HeadSpec
{
    public int Item, Extra, Boss = -1;
    public string? Mod;
    public int[]? Npcs;
}

public sealed record VanillaEntry(
    string key, int type, double prog, string name, string en, int[] ids, Func<bool> downed,
    string? portrait = null, HeadSpec? head = null, int[]? limbs = null, string? available = null, bool despawn = false, string? set = null);

public static class Data
{
    public const int BOSS = 0, MINIBOSS = 1, EVENT = 2;
    public static readonly string[] TYPE_NAMES = { "Boss", "MiniBoss", "Event" };

    public const int RELIC = 0, PET = 1, TROPHY = 2, MASK = 3, MUSIC = 4, GENERIC = 5;
    public static readonly string[] COLLECTIBLE_NAMES = { "Relic", "MasterPet", "Trophy", "Mask", "Music", "Generic" };

    static bool DS(string flag) => World.downed.Contains(flag);
    static bool Torches()
    {
        Player p = Util.Local;
        return !p.IsNull && p.unlockedBiomeTorches;
    }

    public static readonly VanillaEntry[] VANILLA =
    {
        new("KingSlime", BOSS, 1, "NPCName.KingSlime", "King Slime", new[] { 50 }, () => NPC.downedSlimeKing, portrait: "Boss50", despawn: true),
        new("TorchGod", EVENT, 1.5, "NPCName.TorchGod", "Torch God", new[] { 664 }, Torches, head: new HeadSpec { Item = 5043 }),
        new("EyeofCthulhu", BOSS, 2, "NPCName.EyeofCthulhu", "Eye of Cthulhu", new[] { 4 }, () => NPC.downedBoss1, despawn: true),
        new("BloodMoon", EVENT, 2.5, "Bestiary_Events.BloodMoon", "Blood Moon", new[] { 489, 490, 53, 536, 47, 464, 57, 465, 168, 470, 109, 378, 587, 586, 620, 621, 619, 618 }, () => DS("bloodmoon"), portrait: "EventBloodMoon", head: new HeadSpec { Mod = "EventBloodMoon_Head" }),
        new("EaterofWorlds", BOSS, 3, "NPCName.EaterofWorldsHead", "Eater of Worlds", new[] { 13, 14, 15 }, () => NPC.downedBoss2, available: "corruption", portrait: "Boss13", despawn: true),
        new("BrainofCthulhu", BOSS, 3, "NPCName.BrainofCthulhu", "Brain of Cthulhu", new[] { 266 }, () => NPC.downedBoss2, available: "crimson", despawn: true),
        new("GoblinArmy", EVENT, 3.33, "LegacyInterface.88", "Goblin Army", new[] { 26, 27, 28, 29, 111, 471 }, () => NPC.downedGoblins, portrait: "EventGoblinArmy", head: new HeadSpec { Extra = 9 }),
        new("OldOnesArmy", EVENT, 3.66, "DungeonDefenders2.InvasionProgressTitle", "Old One's Army", new[] { 551, 552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 570, 571, 572, 573, 574, 575, 576, 577, 578 }, () => DD2Event.DownedInvasionAnyDifficulty, portrait: "EventOldOnesArmy", head: new HeadSpec { Extra = 79 }, set: "BelongsToInvasionOldOnesArmy"),
        new("DD2DarkMageT3", MINIBOSS, 3.67, "NPCName.DD2DarkMageT3", "Dark Mage", new[] { 565, 564 }, () => DS("darkmage"), portrait: "Boss565"),
        new("QueenBee", BOSS, 4, "NPCName.QueenBee", "Queen Bee", new[] { 222 }, () => NPC.downedQueenBee, despawn: true),
        new("Skeletron", BOSS, 5, "NPCName.SkeletronHead", "Skeletron", new[] { 35 }, () => NPC.downedBoss3, limbs: new[] { 36 }, portrait: "Boss35", despawn: true),
        new("Deerclops", BOSS, 6, "NPCName.Deerclops", "Deerclops", new[] { 668 }, () => NPC.downedDeerclops, portrait: "Boss668", despawn: true),
        new("WallofFlesh", BOSS, 7, "NPCName.WallofFlesh", "Wall of Flesh", new[] { 113 }, () => Main.hardMode, portrait: "Boss113", despawn: true),
        new("FrostLegion", EVENT, 7.33, "LegacyInterface.87", "Frost Legion", new[] { 143, 144, 145 }, () => NPC.downedFrost, available: "xmas", portrait: "EventFrostLegion", head: new HeadSpec { Extra = 7 }),
        new("PirateInvasion", EVENT, 7.66, "LegacyInterface.86", "Pirate Invasion", new[] { 212, 213, 214, 215, 216, 491 }, () => NPC.downedPirates, portrait: "EventPirateInvasion", head: new HeadSpec { Extra = 11 }),
        new("PirateShip", MINIBOSS, 7.67, "NPCName.PirateShip", "Flying Dutchman", new[] { 491 }, () => DS("flyingdutchman"), portrait: "Boss491"),
        new("QueenSlimeBoss", BOSS, 8, "NPCName.QueenSlimeBoss", "Queen Slime", new[] { 657 }, () => NPC.downedQueenSlime, portrait: "Boss657", despawn: true),
        new("TheTwins", BOSS, 9, "Enemies.TheTwins", "The Twins", new[] { 125, 126 }, () => NPC.downedMechBoss2, limbs: new[] { 125, 126 }, portrait: "Boss125", despawn: true),
        new("TheDestroyer", BOSS, 10, "NPCName.TheDestroyer", "The Destroyer", new[] { 134 }, () => NPC.downedMechBoss1, portrait: "Boss134", despawn: true),
        new("SkeletronPrime", BOSS, 11, "NPCName.SkeletronPrime", "Skeletron Prime", new[] { 127 }, () => NPC.downedMechBoss3, limbs: new[] { 128, 129, 130, 131 }, portrait: "Boss127", despawn: true),
        new("DD2OgreT3", MINIBOSS, 11.01, "NPCName.DD2OgreT3", "Ogre", new[] { 577, 576 }, () => DS("ogre"), portrait: "Boss577"),
        new("Eclipse", EVENT, 11.5, "Bestiary_Events.Eclipse", "Solar Eclipse", new[] { 251, 162, 166, 159, 461, 462, 469, 253, 460, 467, 468, 463, 466, 477, 479 }, () => DS("solareclipse"), portrait: "EventSolarEclipse", head: new HeadSpec { Mod = "EventSolarEclipse_Head" }),
        new("Plantera", BOSS, 12, "NPCName.Plantera", "Plantera", new[] { 262 }, () => NPC.downedPlantBoss, despawn: true),
        new("Golem", BOSS, 13, "NPCName.Golem", "Golem", new[] { 245 }, () => NPC.downedGolemBoss, limbs: new[] { 247, 248, 246 }, portrait: "Boss245", head: new HeadSpec { Boss = 5 }, despawn: true),
        new("PumpkinMoon", EVENT, 13.25, "LegacyInterface.84", "Pumpkin Moon", new[] { 305, 326, 329, 330, 315, 325, 327 }, () => DS("pumpkinmoon"), portrait: "EventPumpkinMoon", head: new HeadSpec { Extra = 12 }),
        new("MourningWood", MINIBOSS, 13.26, "NPCName.MourningWood", "Mourning Wood", new[] { 325 }, () => NPC.downedHalloweenTree),
        new("Pumpking", MINIBOSS, 13.27, "NPCName.Pumpking", "Pumpking", new[] { 327 }, () => NPC.downedHalloweenKing, portrait: "Boss327"),
        new("FrostMoon", EVENT, 13.5, "LegacyInterface.83", "Frost Moon", new[] { 341, 352, 342, 338, 350, 348, 343, 347, 351, 344, 346, 345 }, () => DS("frostmoon"), portrait: "EventFrostMoon", head: new HeadSpec { Extra = 8 }),
        new("Everscream", MINIBOSS, 13.51, "NPCName.Everscream", "Everscream", new[] { 344 }, () => NPC.downedChristmasTree),
        new("SantaNK1", MINIBOSS, 13.52, "NPCName.SantaNK1", "Santa-NK1", new[] { 346 }, () => NPC.downedChristmasSantank),
        new("IceQueen", MINIBOSS, 13.53, "NPCName.IceQueen", "Ice Queen", new[] { 345 }, () => NPC.downedChristmasIceQueen),
        new("MartianMadness", EVENT, 13.75, "LegacyInterface.85", "Martian Madness", new[] { 381, 382, 383, 385, 386, 388, 389, 390, 391, 395, 520 }, () => NPC.downedMartians, portrait: "EventMartianMadness", head: new HeadSpec { Extra = 10 }),
        new("MartianSaucer", MINIBOSS, 13.76, "NPCName.MartianSaucer", "Martian Saucer", new[] { 392, 395 }, () => DS("martiansaucer")),
        new("DukeFishron", BOSS, 14, "NPCName.DukeFishron", "Duke Fishron", new[] { 370 }, () => NPC.downedFishron, despawn: true),
        new("HallowBoss", BOSS, 15, "NPCName.HallowBoss", "Empress of Light", new[] { 636 }, () => NPC.downedEmpressOfLight, portrait: "Boss636", despawn: true),
        new("DD2Betsy", BOSS, 16, "NPCName.DD2Betsy", "Betsy", new[] { 551 }, () => DD2Event.DownedInvasionT3, portrait: "Boss551"),
        new("CultistBoss", BOSS, 17, "NPCName.CultistBoss", "Lunatic Cultist", new[] { 439 }, () => NPC.downedAncientCultist, portrait: "Boss439", despawn: true),
        new("LunarEvent", EVENT, 17.01, "Checklist.CelestialPillars", "Celestial Pillars", new[] { 517, 419, 518, 418, 417, 412, 415, 416, 422, 427, 426, 428, 425, 429, 507, 423, 420, 421, 424, 493, 405, 407, 411, 409, 402 }, () => NPC.downedTowers, limbs: new[] { 422, 493, 507, 517 }, portrait: "EventLunarEvent", head: new HeadSpec { Npcs = new[] { 507, 422, 517, 493 } }),
        new("MoonLord", BOSS, 18, "Enemies.MoonLord", "Moon Lord", new[] { 396, 398 }, () => NPC.downedMoonlord, limbs: new[] { 396, 397 }, portrait: "Boss396", despawn: true),
    };

    public static readonly Dictionary<int, string> LIMB_NAMES = new()
    {
        [36] = "SkeletronHand", [125] = "Retinazer", [126] = "Spazmatism", [128] = "PrimeCannon", [129] = "PrimeSaw", [130] = "PrimeVice", [131] = "PrimeLaser",
        [246] = "GolemHead", [247] = "GolemFistLeft", [248] = "GolemFistRight", [396] = "MoonLordHead", [397] = "MoonLordHand",
        [422] = "LunarTowerVortex", [493] = "LunarTowerStardust", [507] = "LunarTowerNebula", [517] = "LunarTowerSolar",
    };

    public static readonly Dictionary<string, int[]> SPAWN_ITEMS = new()
    {
        ["KingSlime"] = new[] { 560 }, ["EyeofCthulhu"] = new[] { 43 }, ["EaterofWorlds"] = new[] { 70 }, ["BrainofCthulhu"] = new[] { 1331 }, ["QueenBee"] = new[] { 1133 }, ["Skeletron"] = new[] { 1307 },
        ["Deerclops"] = new[] { 5120 }, ["WallofFlesh"] = new[] { 267 }, ["QueenSlimeBoss"] = new[] { 4988 }, ["TheTwins"] = new[] { 544 }, ["TheDestroyer"] = new[] { 556 }, ["SkeletronPrime"] = new[] { 557 },
        ["Golem"] = new[] { 1292, 1293 }, ["HallowBoss"] = new[] { 4961 }, ["DD2Betsy"] = new[] { 3828, 3816 }, ["DukeFishron"] = new[] { 2673 }, ["MoonLord"] = new[] { 3601 }, ["TorchGod"] = new[] { 8 },
        ["BloodMoon"] = new[] { 4271 }, ["GoblinArmy"] = new[] { 361 }, ["OldOnesArmy"] = new[] { 3828, 3816 }, ["FrostLegion"] = new[] { 602 }, ["Eclipse"] = new[] { 2767 }, ["PirateInvasion"] = new[] { 1315 },
        ["PumpkinMoon"] = new[] { 1844 }, ["FrostMoon"] = new[] { 1958 },
    };

    const int R = RELIC, P = PET, T = TROPHY, M = MASK, U = MUSIC, G = GENERIC;

    public static readonly Dictionary<string, (int id, int type)[]> COLLECTIBLES = new()
    {
        ["KingSlime"] = new[] { (4929, R), (4797, P), (2489, T), (2493, M), (567, U), (5033, U) },
        ["EyeofCthulhu"] = new[] { (4924, R), (4798, P), (1360, T), (2112, M), (567, U), (5033, U), (3763, G), (5004, G) },
        ["EaterofWorlds"] = new[] { (4925, R), (4799, P), (1361, T), (2111, M), (567, U), (5033, U), (994, G) },
        ["BrainofCthulhu"] = new[] { (4926, R), (4800, P), (1362, T), (2104, M), (574, U), (5033, U), (3060, G) },
        ["QueenBee"] = new[] { (4928, R), (4802, P), (1364, T), (2108, M), (1607, U), (5033, U), (1170, G) },
        ["Skeletron"] = new[] { (4927, R), (4801, P), (1363, T), (1281, M), (567, U), (5033, U), (4993, G) },
        ["Deerclops"] = new[] { (5110, R), (5090, P), (5108, T), (5109, M), (5112, U), (5033, U) },
        ["WallofFlesh"] = new[] { (4930, R), (4795, P), (1365, T), (2105, M), (572, U), (5039, U), (5004, G) },
        ["QueenSlimeBoss"] = new[] { (4950, R), (4960, P), (4958, T), (4959, M), (4979, U), (5032, U) },
        ["TheTwins"] = new[] { (4931, R), (4804, P), (1368, T), (1369, T), (2106, M), (572, U), (5032, U) },
        ["TheDestroyer"] = new[] { (4932, R), (4803, P), (1366, T), (2113, M), (574, U), (5032, U) },
        ["SkeletronPrime"] = new[] { (4933, R), (4805, P), (1367, T), (2107, M), (567, U), (5032, U) },
        ["Plantera"] = new[] { (4934, R), (4806, P), (1370, T), (2109, M), (1606, U), (5037, U), (1182, G) },
        ["Golem"] = new[] { (4935, R), (4807, P), (1371, T), (2110, M), (1607, U), (5032, U) },
        ["HallowBoss"] = new[] { (4949, R), (4811, P), (4783, T), (4784, M), (4985, U), (5032, U), (4778, G), (5075, G) },
        ["DD2Betsy"] = new[] { (4948, R), (4817, P), (3866, T), (3863, P), (3869, U), (5034, U) },
        ["DukeFishron"] = new[] { (4936, R), (4808, P), (2589, T), (2588, M), (4990, U), (5032, U) },
        ["CultistBoss"] = new[] { (4937, R), (4809, P), (3357, T), (3372, M), (1607, U), (5032, U) },
        ["MoonLord"] = new[] { (4938, R), (4810, P), (3595, T), (3373, M), (3044, U), (5036, U) },
        ["DD2DarkMageT3"] = new[] { (4946, R), (4796, P), (3867, T), (3864, M), (3857, G), (3855, G) },
        ["PirateShip"] = new[] { (4940, R), (4792, P), (3359, T) },
        ["DD2OgreT3"] = new[] { (4947, R), (4816, P), (3868, T), (3865, M), (3856, G) },
        ["MourningWood"] = new[] { (4941, R), (4793, P), (1855, T), (1837, G) },
        ["Pumpking"] = new[] { (4942, R), (4812, P), (1856, T), (1798, G) },
        ["Everscream"] = new[] { (4944, R), (4813, P), (1962, T) },
        ["SantaNK1"] = new[] { (4945, R), (4794, P), (1961, T) },
        ["IceQueen"] = new[] { (4943, R), (4814, P), (1960, T), (1959, G) },
        ["MartianSaucer"] = new[] { (4939, R), (4815, P), (3358, T) },
        ["TorchGod"] = new[] { (574, U), (5039, U) },
        ["BloodMoon"] = new[] { (563, U), (5031, U) },
        ["GoblinArmy"] = new[] { (3371, U), (5034, U) },
        ["OldOnesArmy"] = new[] { (3869, U), (5034, U) },
        ["FrostLegion"] = new[] { (574, U), (5034, U) },
        ["Eclipse"] = new[] { (1609, U), (5031, U) },
        ["PirateInvasion"] = new[] { (3236, U), (5034, U) },
        ["PumpkinMoon"] = new[] { (1963, U), (5034, U) },
        ["FrostMoon"] = new[] { (1965, U), (5034, U) },
        ["MartianMadness"] = new[] { (3235, U), (5034, U) },
        ["LunarEvent"] = new[] { (3370, U), (5035, U) },
    };

    public static readonly Dictionary<int, int[]> BAG_EXCLUSIVES = new()
    {
        [3318] = new[] { 3090 }, [3319] = new[] { 3097 }, [3320] = new[] { 3224 }, [3321] = new[] { 3223 }, [3322] = new[] { 3333 }, [3323] = new[] { 3245 }, [5111] = new[] { 5100 }, [3324] = new[] { 3335 },
        [4957] = new[] { 4987 }, [3325] = new[] { 3355 }, [3326] = new[] { 3354 }, [3327] = new[] { 3356 }, [3328] = new[] { 3336 }, [3329] = new[] { 3337 }, [3330] = new[] { 3367 }, [4782] = new[] { 4989 },
        [3332] = new[] { 1131, 3577 },
    };

    public static readonly HashSet<int> EXPERT_ITEMS = new() { 3090, 3097, 3224, 3223, 3333, 3245, 5100, 3335, 4987, 3355, 3354, 3356, 3336, 3337, 3367, 4989, 1131, 3577 };

    public static readonly HashSet<int> VANILLA_MUSIC_BOXES = new()
    {
        562, 1600, 564, 1601, 1596, 1602, 1603, 1604, 4077, 1597, 4079, 566, 1964, 1610, 568, 569,
        570, 1598, 2742, 571, 573, 3237, 1605, 1608, 567, 572, 574, 1599, 1607, 5112, 4979, 1606, 4985, 4990, 563, 1609, 3371,
        3236, 3235, 1963, 1965, 3370, 3044, 3796, 3869, 4078, 4080, 4081, 4082, 4237, 4356, 4357, 4358, 4421, 4606, 4991, 4992,
        5006, 5044, 565,
    };

    public static readonly HashSet<int> OTHERWORLD_MUSIC_BOXES = new(Enumerable.Range(5014, 27));

    public static readonly HashSet<int> DAY_DESPAWNERS = new() { 4, 125, 126, 134 };
    public static readonly HashSet<int> SKIP_HEADS = new() { 564, 576 };

    public static readonly string[] DOWNED_FLAGS = { "bloodmoon", "frostmoon", "pumpkinmoon", "solareclipse", "darkmage", "ogre", "flyingdutchman", "martiansaucer" };

    public static readonly Dictionary<int, string> KILL_FLAGS = new()
    {
        [564] = "darkmage", [565] = "darkmage", [576] = "ogre", [577] = "ogre", [491] = "flyingdutchman", [395] = "martiansaucer",
    };

    public static readonly int[] FRAGMENTS = { 3456, 3457, 3458, 3459 };
    public static readonly int[] SCALES = { 86, 1329 };

    public static readonly Color RarityPurple = new(210, 160, 255);
    public static readonly Color RarityGreen = new(150, 255, 150);
    public static readonly Color RarityRed = new(255, 150, 150);
    public static readonly Color RarityYellow = new(255, 255, 150);
    public static readonly Color Orange = new(255, 165, 0);
    public static readonly Color MoonEnd = new(50, 255, 130);
    public static readonly Color LightYellow = new(255, 255, 224);
}
