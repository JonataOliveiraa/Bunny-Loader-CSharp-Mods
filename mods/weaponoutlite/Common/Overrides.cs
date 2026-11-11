using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Terraria.ID;
using Terraria.ModLoader;

namespace WeaponOutLiteMod.Common;

public struct StyleOverride
{
    public PoseGroup poseGroup;
    public DrawItemPoseID drawItemPose;
    public int? turn;
}

// The per-item overrides, kept in ItemOverrides.json in the mod's data folder.
public static class Overrides
{
    public sealed class Entry
    {
        public string Item = "";
        public string? ForcePoseGroup;
        public string? ForceDrawItemPose;
        public double? SpriteTurn;
    }

    static Entry[] Defaults() => new[]
    {
        new Entry { Item = "EmpressBlade", ForcePoseGroup = "Unassigned", ForceDrawItemPose = "FloatingOffHandAimed" },
        new Entry { Item = "JoustingLance", ForcePoseGroup = "Spear", ForceDrawItemPose = "JoustingLance" },
        new Entry { Item = "ShadowJoustingLance", ForcePoseGroup = "Spear", ForceDrawItemPose = "JoustingLance" },
        new Entry { Item = "HallowJoustingLance", ForcePoseGroup = "Spear", ForceDrawItemPose = "JoustingLance" },
    };

    static List<Entry>? list;
    static Dictionary<int, StyleOverride> cache = new();
    static Mod? mod;

    public static int Revision { get; private set; }

    // The JavaScript mod's data folder, mod_data/<uid>, sits next to bunny_packs/<uid>. C# has no
    // property for it, so it is found from the package folder: saved overrides keep working.
    static string FileOf()
    {
        string? root = null;
        for (DirectoryInfo? dir = new DirectoryInfo(mod!.Folder); dir != null; dir = dir.Parent)
        {
            if (dir.Name == "bunny_packs")
            {
                root = dir.Parent?.FullName;
                break;
            }
        }
        return Path.Combine(root ?? mod.Folder, "mod_data", mod.Uid, "ItemOverrides.json");
    }

    static Dictionary<string, int>? vanillaIds;
    static Dictionary<int, string>? vanillaNames;

    // ItemID's constants, by name and by number. The first name of a number wins.
    static void LoadVanillaNames()
    {
        vanillaIds = new Dictionary<string, int>();
        vanillaNames = new Dictionary<int, string>();
        foreach (FieldInfo field in typeof(ItemID).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (!field.IsLiteral || (field.FieldType != typeof(short) && field.FieldType != typeof(int)))
                continue;
            int value = Convert.ToInt32(field.GetRawConstantValue(), CultureInfo.InvariantCulture);
            vanillaIds[field.Name] = value;
            if (value > 0 && value < ItemID.Count)
                vanillaNames.TryAdd(value, field.Name);
        }
    }

    static string? VanillaName(int type)
    {
        if (vanillaNames == null)
            LoadVanillaNames();
        return vanillaNames!.TryGetValue(type, out string? name) ? name : null;
    }

    public static string ItemKey(int type) => ModContent.GetModItem(type)?.FullName ?? VanillaName(type) ?? type.ToString(CultureInfo.InvariantCulture);

    static bool AllDigits(string key)
    {
        foreach (char c in key)
        {
            if (c < '0' || c > '9')
                return false;
        }
        return key.Length > 0;
    }

    static int TypeOf(string key)
    {
        if (AllDigits(key))
            return int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : 0;
        if (key.Contains('/'))
            return ModContent.TryFind(key, out ModItem modItem) ? modItem.Type : 0;
        if (vanillaIds == null)
            LoadVanillaNames();
        return vanillaIds!.TryGetValue(key, out int value) ? value : 0;
    }

    static int? TurnOf(Entry e)
    {
        if (e.SpriteTurn is not { } turn || turn != Math.Floor(turn))
            return null;
        return (int)((PoseMath.RoundHalfUp(turn / 90) % 4 + 4) % 4);
    }

    static bool HasOverride(Entry e) =>
        PoseIDs.GroupNumber(e.ForcePoseGroup) != PoseGroup.Unassigned || PoseIDs.PoseNumber(e.ForceDrawItemPose) != DrawItemPoseID.Unassigned || TurnOf(e) != null;

    static void Rebuild()
    {
        cache = new Dictionary<int, StyleOverride>();
        var clean = new List<Entry>();
        foreach (Entry entry in list!)
        {
            if (entry.Item.Length == 0)
                continue;
            int type = TypeOf(entry.Item);
            if (type == 0 || !HasOverride(entry))
                continue;
            int old = clean.FindIndex(e => TypeOf(e.Item) == type);
            if (old >= 0)
                clean.RemoveAt(old);
            clean.Add(entry);
            cache[type] = new StyleOverride
            {
                poseGroup = PoseIDs.GroupNumber(entry.ForcePoseGroup),
                drawItemPose = PoseIDs.PoseNumber(entry.ForceDrawItemPose),
                turn = TurnOf(entry),
            };
        }
        list = clean;
        Revision++;
    }

    public static void LoadOverrides(Mod owner)
    {
        mod = owner;
        list = new List<Entry>(Defaults());
        try
        {
            string file = FileOf();
            if (File.Exists(file))
            {
                List<Entry>? saved = Json.ReadEntries(File.ReadAllText(file));
                if (saved != null)
                    list = saved;
            }
        }
        catch (Exception e)
        {
            State.Log("ItemOverrides.json invalid, using the defaults: " + e);
        }
        Rebuild();
    }

    static void Save()
    {
        if (mod == null)
            return;
        try
        {
            string file = FileOf();
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, Json.WriteEntries(list!));
        }
        catch (Exception e)
        {
            State.Log("could not write ItemOverrides.json: " + e);
        }
    }

    public static bool FindStyleOverride(int type, out StyleOverride found) => cache.TryGetValue(type, out found);

    public static Entry? OverrideEntry(int type)
    {
        if (list == null)
            return null;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (TypeOf(list[i].Item) == type)
                return list[i];
        }
        return null;
    }

    public static void UpsertOverride(int type, PoseGroup poseGroup, DrawItemPoseID drawItemPose)
    {
        list ??= new List<Entry>();
        Entry? entry = OverrideEntry(type);
        string groupName = PoseIDs.PoseGroupName(poseGroup);
        string poseName = PoseIDs.PoseName(drawItemPose);
        if (entry != null)
        {
            entry.ForcePoseGroup = groupName;
            entry.ForceDrawItemPose = poseName;
        }
        else
        {
            list.Add(new Entry { Item = ItemKey(type), ForcePoseGroup = groupName, ForceDrawItemPose = poseName });
        }
        Rebuild();
        Save();
    }

    static readonly Dictionary<int, int> modTurns = new();

    public static void SetModSpriteTurn(int type, int quarters) => modTurns[type] = (quarters % 4 + 4) % 4;

    public static int? ItemSpriteTurn(int type)
    {
        if (cache.TryGetValue(type, out StyleOverride found) && found.turn != null)
            return found.turn;
        return modTurns.TryGetValue(type, out int turn) ? turn : null;
    }

    public static void SetSpriteTurn(int type, int quarters, bool save = true)
    {
        list ??= new List<Entry>();
        Entry? entry = OverrideEntry(type);
        if (entry == null)
        {
            entry = new Entry { Item = ItemKey(type), ForcePoseGroup = "Unassigned", ForceDrawItemPose = "Unassigned" };
            list.Add(entry);
        }
        entry.SpriteTurn = (quarters % 4 + 4) % 4 * 90;
        Rebuild();
        if (save)
            Save();
    }

    public static bool ClearOverride(int type)
    {
        if (list == null)
            return false;
        int before = list.Count;
        list.RemoveAll(e => TypeOf(e.Item) == type);
        Rebuild();
        Save();
        return list.Count != before;
    }

    public static void ResetOverrides()
    {
        list = new List<Entry>(Defaults());
        Rebuild();
        Save();
    }

    // Reads the entries, and writes them as JSON.stringify(list, null, 2) did.
    static class Json
    {
        public static List<Entry>? ReadEntries(string text)
        {
            using JsonDocument json = JsonDocument.Parse(text);
            if (json.RootElement.ValueKind != JsonValueKind.Array)
                return null;
            var entries = new List<Entry>();
            foreach (JsonElement item in json.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;
                entries.Add(new Entry
                {
                    Item = Field(item, "Item") switch
                    {
                        { ValueKind: JsonValueKind.Number } number => number.GetDouble().ToString(CultureInfo.InvariantCulture),
                        { ValueKind: JsonValueKind.String } key => key.GetString()!,
                        _ => "",
                    },
                    ForcePoseGroup = Text(item, "ForcePoseGroup"),
                    ForceDrawItemPose = Text(item, "ForceDrawItemPose"),
                    SpriteTurn = Field(item, "SpriteTurn") is { ValueKind: JsonValueKind.Number } turn ? turn.GetDouble() : null,
                });
            }
            return entries;
        }

        static JsonElement? Field(JsonElement item, string name) => item.TryGetProperty(name, out JsonElement value) ? value : null;

        static string? Text(JsonElement item, string name) => Field(item, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

        public static string WriteEntries(List<Entry> entries)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                writer.WriteStartArray();
                foreach (Entry entry in entries)
                {
                    writer.WriteStartObject();
                    writer.WriteString("Item", entry.Item);
                    if (entry.ForcePoseGroup != null)
                        writer.WriteString("ForcePoseGroup", entry.ForcePoseGroup);
                    if (entry.ForceDrawItemPose != null)
                        writer.WriteString("ForceDrawItemPose", entry.ForceDrawItemPose);
                    if (entry.SpriteTurn is { } turn)
                        writer.WriteNumber("SpriteTurn", turn);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
