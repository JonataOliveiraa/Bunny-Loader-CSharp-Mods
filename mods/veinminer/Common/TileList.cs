using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace VeinMinerMod.Common;

// The vein-minable blocks: the defaults, plus the player's list. The list is a text
// file with one "type<TAB>name" per line, because the phone has no System.Text.Json.
public static class TileList
{
    static readonly HashSet<int> Defaults = new()
    {
        TileID.Sapphire, TileID.Ruby, TileID.Emerald, TileID.Topaz, TileID.Amethyst,
        TileID.Diamond, TileID.Silt, TileID.Slush, TileID.DesertFossil, TileID.Obsidian,
    };

    static readonly List<(int Type, string Name)> PlayerTiles = new();
    static bool loaded;

    static string FilePath => Path.Combine(Main.SavePath ?? "", "veinminer.playerTiles.txt");

    public static bool IsDefault(int type) => Defaults.Contains(type);

    public static bool IsOre(int type) => TileID.Sets.Ore[type];

    public static IEnumerable<string> PlayerTileNames
    {
        get
        {
            Load();
            return PlayerTiles.Select(tile => tile.Name);
        }
    }

    // Loads on first use: Main.SavePath is not ready while the mod is loading.
    static void Load()
    {
        if (loaded)
            return;
        loaded = true;
        try
        {
            if (!File.Exists(FilePath))
                return;
            foreach (string line in File.ReadAllLines(FilePath))
            {
                string[] parts = line.Split('\t', 2);
                if (parts.Length == 2 && int.TryParse(parts[0], out int type) && type >= 0)
                    PlayerTiles.Add((type, parts[1]));
            }
        }
        catch (Exception e)
        {
            ModContent.GetInstance<VeinMiner>().Logger.Warn("Could not read the list: " + e.Message);
            PlayerTiles.Clear();
        }
    }

    static void Save()
    {
        try
        {
            File.WriteAllLines(FilePath, PlayerTiles.Select(tile => $"{tile.Type}\t{tile.Name}"));
        }
        catch (Exception e)
        {
            ModContent.GetInstance<VeinMiner>().Logger.Warn("Could not save the list: " + e.Message);
        }
    }

    public static bool HasPlayerTile(int type)
    {
        Load();
        return PlayerTiles.Any(tile => tile.Type == type);
    }

    public static bool Add(int type, string name)
    {
        if (HasPlayerTile(type))
            return false;
        PlayerTiles.Add((type, name));
        Save();
        return true;
    }

    public static bool Remove(int type)
    {
        Load();
        if (PlayerTiles.RemoveAll(tile => tile.Type == type) == 0)
            return false;
        Save();
        return true;
    }

    public static int Clear()
    {
        Load();
        int count = PlayerTiles.Count;
        PlayerTiles.Clear();
        Save();
        return count;
    }
}
