using System.Collections.Concurrent;
using System.Collections.Generic;
using Terraria;

namespace WorldGenPreviewerMod.Common.Gen;

public static class PassNames
{
    // Which of the game's own generation texts names each pass.
    static readonly Dictionary<string, int> Message = new()
    {
        ["Terrain"] = 0,
        ["Jungle"] = 11,
        ["Dunes"] = 1,
        ["Mount Caves"] = 2,
        ["Dirt Wall Backgrounds"] = 3,
        ["Rocks In Dirt"] = 4,
        ["Dirt In Rocks"] = 5,
        ["Clay"] = 6,
        ["Small Holes"] = 7,
        ["Dirt Layer Caves"] = 8,
        ["Rock Layer Caves"] = 9,
        ["Surface Caves"] = 10,
        ["Generate Ice Biome"] = 56,
        ["Mud Caves To Grass"] = 77,
        ["Full Desert"] = 78,
        ["Mushroom Patches"] = 13,
        ["Marble"] = 80,
        ["Granite"] = 81,
        ["Floating Islands"] = 12,
        ["Dirt To Mud"] = 14,
        ["Silt"] = 15,
        ["Shinies"] = 16,
        ["Webs"] = 17,
        ["Underworld"] = 18,
        ["Corruption"] = 72,
        ["Lakes"] = 19,
        ["Mountain Caves"] = 21,
        ["Beaches"] = 22,
        ["Gems"] = 23,
        ["Gravitating Sand"] = 24,
        ["Create Ocean Caves"] = 90,
        ["Clean Up Dirt"] = 25,
        ["Altars"] = 26,
        ["Jungle Temple"] = 70,
        ["Hives"] = 71,
        ["Settle Liquids"] = 27,
        ["Smooth World"] = 60,
        ["Waterfalls"] = 69,
        ["Wall Variety"] = 79,
        ["Life Crystals"] = 28,
        ["Statues"] = 29,
        ["Buried Chests"] = 30,
        ["Surface Chests"] = 31,
        ["Jungle Chests Placement"] = 32,
        ["Water Chests"] = 33,
        ["Spider Caves"] = 64,
        ["Gem Caves"] = 64,
        ["Moss"] = 61,
        ["Cave Walls"] = 63,
        ["Jungle Trees"] = 83,
        ["Pots"] = 35,
        ["Hellforge"] = 36,
        ["Spreading Grass"] = 37,
        ["Place Fallen Log"] = 85,
        ["Traps"] = 34,
        ["Piles"] = 89,
        ["Sunflowers"] = 39,
        ["Planting Trees"] = 40,
        ["Herbs"] = 41,
        ["Weeds"] = 42,
        ["Vines"] = 43,
        ["Flowers"] = 44,
        ["Mushrooms"] = 45,
        ["Micro Biomes"] = 76,
        ["Settle Liquids Again"] = 27,
        ["Cactus, Palm Trees, & Coral"] = 38,
        ["Tile Cleanup"] = 84,
        ["Water Plants"] = 88,
        ["Remove Broken Traps"] = 82,
        ["Final Cleanup"] = 86,
    };

    static readonly ConcurrentDictionary<int, string> Cache = new();

    static string GameText(int index) =>
        Cache.GetOrAdd(index, i => (Lang.gen[i].Value ?? "").Trim());

    public static void ClearLabels() => Cache.Clear();

    public static string Label(int i)
    {
        string?[] labels = State.labels;
        if (i >= 0 && i < labels.Length && !string.IsNullOrEmpty(labels[i]))
            return labels[i]!;
        string[] names = State.names;
        if (i < 0 || i >= names.Length)
            return "";
        string name = names[i];
        if (!Message.TryGetValue(name, out int index))
            return name;
        string text = GameText(index);
        return text.Length > 0 ? text : name;
    }
}
