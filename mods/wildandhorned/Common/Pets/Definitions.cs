// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
// Animation timelines, companions and recipes from the original included source.
using System;
using Terraria.ID;

namespace WildAndHornedMod.Common.Pets;

public sealed record PetAction(string Name, (int Frame, int Ticks)[] Frames, float Weight, int[] Lines, int lag = 20, float? alertWeight = null, int[]? alertLines = null);

public sealed record PetCompanion(string Pet, int Line, string? Form);

public sealed record PetIngredient(string? Group, int Item, int Amount)
{
    public PetIngredient(int item, int amount) : this(null, item, amount) { }
    public PetIngredient(string group, int amount) : this(group, 0, amount) { }
}

public sealed record PetRecipe(int Tile, PetIngredient[] Ingredients);

public sealed record PetDefinition(string Name, string Item, int Frames, int Columns, bool Light, float Speed, (int R, int G, int B) Color, (int Width, int Height) Size,
    PetAction[] Actions, int[] Regular, PetCompanion[] Companions, PetRecipe[] Recipes);

public static class PETS
{
    public static readonly PetDefinition Kasen = new("Kasen", "IbarakiBox", 13, 2, false, 14,
        (232, 88, 120), (28, 26),
        Array.Empty<PetAction>(),
        new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
        new PetCompanion[]
        {
            new("Reimu", 11, null),
            new("Suika", 12, null),
            new("Marisa", 13, null),
            new("Yuugi", 22, null),
            new("Komachi", 23, null),
            new("Seiga", 24, null),
            new("Mamizou", 25, null),
            new("Kanako", 26, null),
            new("Shion", 27, null),
        },
        new PetRecipe[]
        {
            new(TileID.WorkBenches, new PetIngredient[] { new("Wood", 12), new(ItemID.Silk, 3), new(ItemID.Chain, 1) }),
        });

    public static readonly PetDefinition Yuugi = new("Yuugi", "StarBearSakeDish", 7, 2, false, 13f,
        (240, 180, 60), (28, 18),
        new PetAction[]
        {
            new("Toast", new (int Frame, int Ticks)[] { (3, 30), (4, 20), (3, 16), (4, 30), (3, 24) }, 1, new[] { 13 }, lag: 30),
            new("Laugh", new (int Frame, int Ticks)[] { (5, 8), (6, 8), (5, 8), (6, 8), (5, 8), (6, 8), (5, 8), (6, 8), (5, 16) }, 1, new[] { 14 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Suika", 9, null),
            new("Kasen", 10, "Hermit"),
            new("Satori", 11, null),
            new("Kasen", 12, "Oni"),
        },
        new PetRecipe[]
        {
            new(TileID.Furnaces, new PetIngredient[] { new(ItemID.ClayBlock, 10), new(ItemID.FallenStar, 1), new(ItemID.Chain, 1) }),
        });

    public static readonly PetDefinition Komachi = new("Komachi", "SanzuFerryCoin", 8, 3, false, 14,
        (255, 110, 110), (24, 24),
        new PetAction[]
        {
            new("Doze", new (int Frame, int Ticks)[] { (1, 40), (2, 40), (1, 40), (2, 40), (1, 40), (2, 40), (3, 40) }, 2, new[] { 12 }, lag: 0),
            new("Yawn", new (int Frame, int Ticks)[] { (3, 60) }, 1, new[] { 13 }, lag: 10),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Kasen", 9, null),
            new("Yuyuko", 10, null),
            new("Youmu", 11, null),
        },
        new PetRecipe[]
        {
            new(TileID.WorkBenches, new PetIngredient[] { new(ItemID.GoldCoin, 1), new(ItemID.WaterBucket, 1) }),
        });

    public static readonly PetDefinition Seiga = new("Seiga", "WallPassingHairpin", 8, 3, false, 14,
        (110, 160, 240), (28, 24),
        new PetAction[]
        {
            new("Hole", new (int Frame, int Ticks)[] { (3, 24), (4, 60), (3, 16) }, 1, new[] { 10 }, lag: 20),
            new("Laugh", new (int Frame, int Ticks)[] { (1, 12), (2, 12), (1, 12), (2, 12), (1, 12), (2, 12) }, 1, new[] { 11 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7 },
        new PetCompanion[]
        {
            new("Kasen", 8, null),
            new("Reimu", 9, null),
        },
        new PetRecipe[]
        {
            new(TileID.Anvils, new PetIngredient[] { new("IronBar", 3), new(ItemID.FallenStar, 1) }),
        });

    public static readonly PetDefinition Byakuren = new("Byakuren", "SorcerersSutraScroll", 8, 3, false, 14,
        (190, 150, 230), (24, 18),
        new PetAction[]
        {
            new("Meditate", new (int Frame, int Ticks)[] { (1, 20), (2, 20), (1, 20), (2, 20), (1, 20), (2, 20) }, 1, new[] { 11 }, lag: 0),
            new("Twirl", new (int Frame, int Ticks)[] { (3, 6), (4, 6), (5, 6), (3, 6), (4, 6), (5, 6), (3, 6), (4, 6), (5, 6), (3, 6), (4, 6), (5, 6), (3, 6), (4, 6), (5, 6), (3, 10) }, 1, new[] { 12 }, lag: 10),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7 },
        new PetCompanion[]
        {
            new("Murasa", 8, null),
            new("Kogasa", 9, null),
            new("Kasen", 10, null),
            new("Nazrin", 13, null),
            new("Mamizou", 14, null),
            new("Joon", 15, null),
        },
        new PetRecipe[]
        {
            new(TileID.WorkBenches, new PetIngredient[] { new(ItemID.Silk, 5), new(ItemID.FallenStar, 2), new(ItemID.Amethyst, 1) }),
        });

    public static readonly PetDefinition Aunn = new("Aunn", "GuardianShrineBell", 8, 3, true, 16f,
        (110, 210, 170), (24, 26),
        new PetAction[]
        {
            new("Wag", new (int Frame, int Ticks)[] { (1, 60) }, 2, new[] { 11 }, lag: 0, alertWeight: 1),
            new("Bark", new (int Frame, int Ticks)[] { (2, 20), (3, 8), (2, 20), (3, 12) }, 1, new[] { 12 }, lag: 0, alertWeight: 6, alertLines: new[] { 13 }),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Reimu", 9, null),
            new("Okina", 10, null),
            new("InuSakuya", 14, null),
            new("Momiji", 15, null),
        },
        new PetRecipe[]
        {
            new(TileID.Anvils, new PetIngredient[] { new("IronBar", 2), new(ItemID.Silk, 2), new(ItemID.StoneBlock, 20) }),
        });

    public static readonly PetDefinition Okina = new("Okina", "SecretBackDoor", 8, 3, true, 14,
        (245, 175, 60), (20, 26),
        new PetAction[]
        {
            new("Door", new (int Frame, int Ticks)[] { (1, 20), (2, 60), (1, 16) }, 1, new[] { 11 }, lag: 20),
            new("Laugh", new (int Frame, int Ticks)[] { (3, 50) }, 1, new[] { 12 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Yukari", 9, null),
            new("Aunn", 10, null),
        },
        new PetRecipe[]
        {
            new(TileID.WorkBenches, new PetIngredient[] { new(ItemID.WoodenDoor, 1), new(ItemID.FallenStar, 7) }),
        });

    public static readonly PetDefinition Kosuzu = new("Kosuzu", "SuzunaanDemonBook", 11, 3, true, 14,
        (238, 140, 80), (24, 28),
        new PetAction[]
        {
            new("Read", new (int Frame, int Ticks)[] { (1, 30), (2, 16), (3, 6), (4, 6), (5, 6), (1, 30), (2, 16), (3, 6), (4, 6), (5, 6), (1, 30) }, 2, new[] { 13 }, lag: 20),
            new("Wave", new (int Frame, int Ticks)[] { (6, 7), (7, 5), (8, 7), (7, 5), (6, 7), (7, 5), (8, 7), (7, 5), (6, 7), (7, 5), (8, 7), (7, 5), (7, 10) }, 1, new[] { 14 }, lag: 0),
            new("Giggle", new (int Frame, int Ticks)[] { (9, 8), (10, 8), (9, 8), (10, 8), (9, 8), (10, 8), (9, 8), (10, 8), (9, 20) }, 1, new[] { 15 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Reimu", 9, null),
            new("Marisa", 10, null),
            new("Mamizou", 11, null),
            new("Aya", 12, null),
        },
        new PetRecipe[]
        {
            new(TileID.Bookcases, new PetIngredient[] { new(ItemID.Book, 1), new(ItemID.FallenStar, 2) }),
        });

    public static readonly PetDefinition Mamizou = new("Mamizou", "TanukiLeaf", 14, 3, false, 14,
        (206, 112, 130), (26, 26),
        new PetAction[]
        {
            new("Glasses", new (int Frame, int Ticks)[] { (1, 8), (2, 16), (3, 6), (2, 4), (3, 6), (2, 16), (1, 8) }, 1, new[] { 14 }, lag: 0),
            new("Pipe", new (int Frame, int Ticks)[] { (4, 20), (5, 20), (4, 12), (6, 16), (7, 24), (4, 16), (5, 16) }, 1, new[] { 15 }, lag: 10),
            new("Leaf", new (int Frame, int Ticks)[] { (8, 24), (9, 20), (10, 8), (11, 12), (12, 24), (13, 8), (12, 8), (13, 8), (12, 24), (11, 12), (10, 8) }, 1, new[] { 16 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Kosuzu", 9, null),
            new("Ran", 10, null),
            new("Kasen", 11, null),
            new("Kokoro", 12, null),
            new("Nazrin", 13, null),
        },
        new PetRecipe[]
        {
            new(TileID.WorkBenches, new PetIngredient[] { new(ItemID.Acorn, 5), new(ItemID.FallenStar, 1) }),
        });

    public static readonly PetDefinition Nazrin = new("Nazrin", "NazrinPendulum", 10, 3, true, 14,
        (150, 162, 204), (18, 30),
        new PetAction[]
        {
            new("Dowse", new (int Frame, int Ticks)[] { (1, 24), (2, 8), (1, 10), (2, 8), (1, 8), (2, 10), (3, 10), (4, 10), (3, 10), (4, 10), (3, 16) }, 1, new[] { 15 }, lag: 20, alertWeight: 5, alertLines: new[] { 14 }),
            new("Pendulum", new (int Frame, int Ticks)[] { (5, 10), (6, 6), (7, 10), (6, 6), (5, 10), (6, 6), (7, 10), (6, 6), (5, 10), (6, 6), (7, 10), (6, 6), (6, 10) }, 1, new[] { 16 }, lag: 10),
            new("Smug", new (int Frame, int Ticks)[] { (8, 20), (9, 6), (8, 10), (9, 6), (8, 24) }, 1, new[] { 17 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Byakuren", 9, null),
            new("Murasa", 10, null),
            new("Kogasa", 11, null),
            new("Mamizou", 12, null),
            new("Marisa", 13, null),
        },
        new PetRecipe[]
        {
            new(TileID.Anvils, new PetIngredient[] { new(ItemID.Sapphire, 2), new(ItemID.Chain, 1) }),
        });

    public static readonly PetDefinition Suwako = new("Suwako", "MoriyaIronRing", 8, 3, true, 14,
        (150, 130, 230), (24, 24),
        new PetAction[]
        {
            new("Hop", new (int Frame, int Ticks)[] { (1, 8), (2, 6), (3, 8), (2, 6), (1, 8), (2, 6), (3, 8), (2, 6), (1, 10) }, 2, new[] { 13 }, lag: 0),
            new("Ring", new (int Frame, int Ticks)[] { (4, 24), (5, 16), (6, 6), (5, 6), (6, 6), (5, 6), (6, 6), (5, 12), (4, 16) }, 1, new[] { 14 }, lag: 20),
            new("Laugh", new (int Frame, int Ticks)[] { (7, 50) }, 1, new[] { 15 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Kanako", 9, null),
            new("Sanae", 10, null),
            new("Cirno", 11, null),
            new("Reimu", 12, null),
        },
        new PetRecipe[]
        {
            new(TileID.Anvils, new PetIngredient[] { new("IronBar", 3), new(ItemID.Frog, 1) }),
        });

    public static readonly PetDefinition Kanako = new("Kanako", "MiniatureOnbashira", 8, 3, false, 13f,
        (214, 72, 70), (20, 32),
        new PetAction[]
        {
            new("Present", new (int Frame, int Ticks)[] { (1, 8), (2, 30), (3, 8), (2, 8), (3, 8), (2, 20), (1, 8) }, 1, new[] { 14 }, lag: 20),
            new("Onbashira", new (int Frame, int Ticks)[] { (4, 30), (5, 10), (6, 40), (5, 10), (4, 20) }, 1, new[] { 15 }, lag: 30),
            new("Laugh", new (int Frame, int Ticks)[] { (7, 50) }, 1, new[] { 16 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Suwako", 9, null),
            new("Sanae", 10, null),
            new("Utsuho", 11, null),
            new("Nitori", 12, null),
            new("Reimu", 13, null),
        },
        new PetRecipe[]
        {
            new(TileID.Sawmill, new PetIngredient[] { new("Wood", 20), new(ItemID.Rope, 10) }),
        });

    public static readonly PetDefinition InuSakuya = new("InuSakuya", "MaidsBellCollar", 8, 3, true, 15f,
        (120, 150, 230), (24, 24),
        new PetAction[]
        {
            new("Beg", new (int Frame, int Ticks)[] { (1, 8), (2, 8), (1, 8), (2, 8), (1, 8), (2, 8), (1, 8), (2, 8), (1, 16) }, 2, new[] { 14 }, lag: 0),
            new("Tea", new (int Frame, int Ticks)[] { (3, 24), (4, 12), (5, 12), (4, 12), (5, 12), (3, 20) }, 1, new[] { 15 }, lag: 20),
            new("Shake", new (int Frame, int Ticks)[] { (6, 4), (7, 4), (6, 4), (7, 4), (6, 4), (7, 4), (6, 4), (7, 4), (6, 4), (7, 4), (0, 10) }, 1, new[] { 16 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Remilia", 9, null),
            new("Sakuya", 10, null),
            new("Meirin", 11, null),
            new("Flandre", 12, null),
            new("Aunn", 13, null),
        },
        new PetRecipe[]
        {
            new(TileID.WorkBenches, new PetIngredient[] { new(ItemID.SilverWatch, 1), new(ItemID.Silk, 1) }),
            new(TileID.WorkBenches, new PetIngredient[] { new(ItemID.TungstenWatch, 1), new(ItemID.Silk, 1) }),
        });

    public static readonly PetDefinition Shion = new("Shion", "OverdueNotice", 8, 3, false, 12f,
        (110, 140, 230), (24, 24),
        new PetAction[]
        {
            new("Beg", new (int Frame, int Ticks)[] { (1, 10), (2, 30), (3, 8), (2, 8), (3, 8), (2, 24), (1, 10) }, 2, new[] { 14 }, lag: 20),
            new("Misfortune", new (int Frame, int Ticks)[] { (4, 20), (5, 8), (6, 8), (5, 8), (6, 8), (5, 8), (6, 30), (5, 10), (4, 20) }, 1, new[] { 15 }, lag: 30),
            new("Sigh", new (int Frame, int Ticks)[] { (7, 50) }, 1, new[] { 16 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Joon", 9, null),
            new("Tenshi", 10, null),
            new("Reimu", 11, null),
            new("Kasen", 12, null),
            new("Yuyuko", 13, null),
        },
        new PetRecipe[]
        {
            new(TileID.WorkBenches, new PetIngredient[] { new(ItemID.Silk, 2), new(ItemID.DirtBlock, 1) }),
        });

    public static readonly PetDefinition Joon = new("Joon", "DesignerHandbag", 9, 3, true, 15f,
        (232, 176, 48), (24, 24),
        new PetAction[]
        {
            new("Fan", new (int Frame, int Ticks)[] { (1, 10), (2, 12), (3, 12), (2, 12), (3, 12), (2, 12), (3, 12), (2, 20), (1, 10) }, 1, new[] { 14 }, lag: 10),
            new("Shades", new (int Frame, int Ticks)[] { (4, 8), (5, 20), (6, 40), (5, 16), (4, 8) }, 1, new[] { 15 }, lag: 28),
            new("Coin", new (int Frame, int Ticks)[] { (7, 6), (8, 14), (7, 6), (0, 12), (7, 6), (8, 14), (7, 6), (0, 12) }, 2, new[] { 16 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Shion", 9, null),
            new("Byakuren", 10, null),
            new("Mamizou", 11, null),
            new("Reimu", 12, null),
            new("Marisa", 13, null),
        },
        new PetRecipe[]
        {
            new(TileID.Loom, new PetIngredient[] { new(ItemID.Leather, 1), new(ItemID.Silk, 3), new(ItemID.Topaz, 2) }),
        });

    public static readonly PetDefinition Momiji = new("Momiji", "DaiShogiPiece", 8, 3, false, 15f,
        (236, 112, 60), (24, 24),
        new PetAction[]
        {
            new("Scout", new (int Frame, int Ticks)[] { (1, 8), (2, 30), (3, 40), (2, 20), (1, 8) }, 2, new[] { 14 }, lag: 30),
            new("Shogi", new (int Frame, int Ticks)[] { (4, 30), (5, 12), (4, 12), (5, 12), (4, 20), (6, 36) }, 1, new[] { 15 }, lag: 20),
            new("Alert", new (int Frame, int Ticks)[] { (7, 50) }, 1, new[] { 16 }, lag: 0),
        },
        new[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        new PetCompanion[]
        {
            new("Nitori", 9, null),
            new("Aya", 10, null),
            new("Kanako", 11, null),
            new("Sanae", 12, null),
            new("Aunn", 13, null),
        },
        new PetRecipe[]
        {
            new(TileID.Sawmill, new PetIngredient[] { new("Wood", 10), new(ItemID.FallenStar, 1) }),
        });

    // In the order of the original table: the reply codes depend on it.
    public static readonly PetDefinition[] All = { Kasen, Yuugi, Komachi, Seiga, Byakuren, Aunn, Okina, Kosuzu, Mamizou, Nazrin, Suwako, Kanako, InuSakuya, Shion, Joon, Momiji };
}
