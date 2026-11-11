using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace LootBeamsMod.Common;

// What a player chose with /beams.
public sealed class ListData
{
    public List<int> Whitelist = new();
    public List<int> Blacklist = new();
    public List<int> Mini = new();
    public SortedDictionary<int, Rgb> Colors = new();
    public Dictionary<int, string> Names = new();
}

// The same lists, ready to be read while drawing.
public sealed class ListLookup
{
    public HashSet<int> White = new();
    public HashSet<int> Black = new();
    public HashSet<int> Mini = new();
    public Dictionary<int, Rgb> Colors = new();
}

// The default lists, plus the player's. The file has one "kind<TAB>type[<TAB>value]" per line.
public static class Lists
{
    static Rgb C(double r, double g, double b) => new(Util.Round(r * 255), Util.Round(g * 255), Util.Round(b * 255));

    public static readonly Dictionary<int, Rgb> DefaultColors = new()
    {
        [71] = C(0.95, 0.55, 0.4), [72] = C(0.7, 0.75, 0.75), [73] = C(0.875, 0.8, 0.35), [74] = C(0.7, 0.85, 0.85),
        [3822] = C(0, 1, 0.55),
        [58] = C(0.85, 0.08, 0.08), [1734] = C(0.85, 0.08, 0.08), [1867] = C(0.85, 0.08, 0.08),
        [184] = C(0.25, 0.4, 0.9), [1735] = C(0.25, 0.4, 0.9), [1868] = C(0.25, 0.4, 0.9), [4143] = C(0.25, 0.4, 0.9),
        [3453] = C(1, 0, 1), [3454] = C(0.8, 0.4, 0.1), [3455] = C(0.5, 0, 0.5),
    };

    public static readonly HashSet<int> DefaultWhitelist = new() { 71, 72, 73, 74, 3822, 1774, 1869, 327, 329, 1533, 1534, 1536, 1537, 4714, 892, 885, 891, 893, 886, 887, 890, 889, 888, 1326 };
    public static readonly HashSet<int> DefaultBlacklist = new() { 58, 1734, 1867, 184, 1735, 1868, 4143, 3453, 3454, 3455, 520, 521, 575, 547, 548, 549 };
    public static readonly HashSet<int> DefaultMini = new() { 71, 72, 73, 74, 3822, 58, 1734, 1867, 184, 1735, 1868, 4143, 3453, 3454, 3455 };

    public static int Version { get; private set; }

    static ListData data = new();
    static bool loaded;

    static int cacheVersion = -1;
    static readonly ListLookup Cache = new();

    static string FilePath => Path.Combine(Main.SavePath ?? "", "lootbeams.lists.txt");

    static void Warn(string text) => ModContent.GetInstance<LootBeams>().Logger.Warn(text);

    // Loads on first use: Main.SavePath is not ready while the mod is loading.
    static void Load()
    {
        loaded = true;
        try
        {
            if (File.Exists(FilePath))
            {
                var read = new ListData();
                foreach (string line in File.ReadAllLines(FilePath))
                    ReadLine(read, line);
                data = read;
            }
        }
        catch (Exception e)
        {
            Warn("The lists could not be read: " + e.Message);
            data = new ListData();
        }
        Version++;
    }

    static void ReadLine(ListData into, string line)
    {
        string[] parts = line.Split('\t', 3);
        if (parts.Length < 2 || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int type))
            return;
        switch (parts[0])
        {
            case "whitelist":
                into.Whitelist.Add(type);
                break;
            case "blacklist":
                into.Blacklist.Add(type);
                break;
            case "mini":
                into.Mini.Add(type);
                break;
            case "color":
                if (parts.Length == 3 && Util.ParseHex(parts[2]) is Rgb rgb)
                    into.Colors[type] = rgb;
                break;
            case "name":
                if (parts.Length == 3)
                    into.Names[type] = parts[2];
                break;
        }
    }

    public static void Save()
    {
        Version++;
        try
        {
            var lines = new List<string>();
            foreach (int type in data.Whitelist)
                lines.Add($"whitelist\t{type}");
            foreach (int type in data.Blacklist)
                lines.Add($"blacklist\t{type}");
            foreach (int type in data.Mini)
                lines.Add($"mini\t{type}");
            foreach (var (type, rgb) in data.Colors)
                lines.Add($"color\t{type}\t{Util.ToHex(rgb)}");
            foreach (var (type, name) in data.Names)
                lines.Add($"name\t{type}\t{name}");
            File.WriteAllLines(FilePath, lines);
        }
        catch (Exception e)
        {
            Warn("It was not possible to save the lists: " + e.Message);
        }
    }

    public static ListData Data()
    {
        if (!loaded)
            Load();
        return data;
    }

    public static bool Forget(int type)
    {
        ListData d = Data();
        bool changed = d.Whitelist.RemoveAll(t => t == type) > 0;
        changed |= d.Blacklist.RemoveAll(t => t == type) > 0;
        changed |= d.Mini.RemoveAll(t => t == type) > 0;
        changed |= d.Colors.Remove(type);
        d.Names.Remove(type);
        if (changed)
            Save();
        return changed;
    }

    // Puts the item in one of the two lists and takes it out of the other.
    public static void Put(bool always, int type, string name)
    {
        ListData d = Data();
        (always ? d.Blacklist : d.Whitelist).RemoveAll(t => t == type);
        List<int> list = always ? d.Whitelist : d.Blacklist;
        if (!list.Contains(type))
            list.Add(type);
        d.Names[type] = name;
        Save();
    }

    public static void SetColor(int type, Rgb rgb, string name)
    {
        ListData d = Data();
        d.Colors[type] = rgb;
        d.Names[type] = name;
        Save();
    }

    public static void ClearAll()
    {
        Data();
        data = new ListData();
        Save();
    }

    public static ListLookup Lookup()
    {
        ListData d = Data();
        if (cacheVersion != Version)
        {
            cacheVersion = Version;
            Cache.White = new HashSet<int>(d.Whitelist);
            Cache.Black = new HashSet<int>(d.Blacklist);
            Cache.Mini = new HashSet<int>(d.Mini);
            Cache.Colors = new Dictionary<int, Rgb>(d.Colors);
        }
        return Cache;
    }

    public static string NameOf(Item item)
    {
        string? name = item.Name;
        if (string.IsNullOrEmpty(name))
            name = item.AffixName();
        return string.IsNullOrEmpty(name) ? "#" + item.type : name;
    }
}
