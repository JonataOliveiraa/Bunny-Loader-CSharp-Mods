using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
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

    // The file is plain JSON and System.Text.Json is not on the phone. This reads the JSON grammar
    // into strings, numbers, lists and dictionaries, and writes the entries as JSON.stringify(list, null, 2) did.
    static class Json
    {
        public static List<Entry>? ReadEntries(string text)
        {
            int i = 0;
            object? root = Value(text, ref i);
            SkipSpace(text, ref i);
            if (i != text.Length)
                throw new FormatException("text after the end of the JSON, at " + i);
            if (root is not List<object?> items)
                return null;
            var entries = new List<Entry>();
            foreach (object? item in items)
            {
                if (item is not Dictionary<string, object?> fields)
                    continue;
                fields.TryGetValue("Item", out object? key);
                fields.TryGetValue("ForcePoseGroup", out object? group);
                fields.TryGetValue("ForceDrawItemPose", out object? pose);
                fields.TryGetValue("SpriteTurn", out object? turn);
                entries.Add(new Entry
                {
                    Item = key is double number ? number.ToString(CultureInfo.InvariantCulture) : key as string ?? "",
                    ForcePoseGroup = group as string,
                    ForceDrawItemPose = pose as string,
                    SpriteTurn = turn as double?,
                });
            }
            return entries;
        }

        static void SkipSpace(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n'))
                i++;
        }

        static object? Value(string s, ref int i)
        {
            SkipSpace(s, ref i);
            if (i >= s.Length)
                throw new FormatException("unexpected end of the JSON");
            char c = s[i];
            if (c == '{')
                return Object(s, ref i);
            if (c == '[')
                return Array(s, ref i);
            if (c == '"')
                return String(s, ref i);
            if (Word(s, ref i, "true"))
                return true;
            if (Word(s, ref i, "false"))
                return false;
            if (Word(s, ref i, "null"))
                return null;
            return Number(s, ref i);
        }

        static bool Word(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0)
                return false;
            i += word.Length;
            return true;
        }

        static double Number(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && (char.IsAsciiDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E'))
                i++;
            return double.Parse(s.AsSpan(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        static string String(string s, ref int i)
        {
            var text = new StringBuilder();
            i++;
            while (true)
            {
                char c = s[i++];
                if (c == '"')
                    return text.ToString();
                if (c != '\\')
                {
                    text.Append(c);
                    continue;
                }
                char escape = s[i++];
                switch (escape)
                {
                    case 'b': text.Append('\b'); break;
                    case 'f': text.Append('\f'); break;
                    case 'n': text.Append('\n'); break;
                    case 'r': text.Append('\r'); break;
                    case 't': text.Append('\t'); break;
                    case 'u':
                        text.Append((char)int.Parse(s.AsSpan(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: text.Append(escape); break;
                }
            }
        }

        static List<object?> Array(string s, ref int i)
        {
            var items = new List<object?>();
            i++;
            SkipSpace(s, ref i);
            if (s[i] == ']')
            {
                i++;
                return items;
            }
            while (true)
            {
                items.Add(Value(s, ref i));
                SkipSpace(s, ref i);
                char c = s[i++];
                if (c == ']')
                    return items;
                if (c != ',')
                    throw new FormatException("expected , or ] at " + (i - 1));
            }
        }

        static Dictionary<string, object?> Object(string s, ref int i)
        {
            var fields = new Dictionary<string, object?>();
            i++;
            SkipSpace(s, ref i);
            if (s[i] == '}')
            {
                i++;
                return fields;
            }
            while (true)
            {
                SkipSpace(s, ref i);
                if (s[i] != '"')
                    throw new FormatException("expected a name at " + i);
                string name = String(s, ref i);
                SkipSpace(s, ref i);
                if (s[i++] != ':')
                    throw new FormatException("expected : at " + (i - 1));
                fields[name] = Value(s, ref i);
                SkipSpace(s, ref i);
                char c = s[i++];
                if (c == '}')
                    return fields;
                if (c != ',')
                    throw new FormatException("expected , or } at " + (i - 1));
            }
        }

        static void Quoted(StringBuilder text, string value)
        {
            text.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': text.Append("\\\""); break;
                    case '\\': text.Append("\\\\"); break;
                    case '\n': text.Append("\\n"); break;
                    case '\r': text.Append("\\r"); break;
                    case '\t': text.Append("\\t"); break;
                    default:
                        if (c < ' ')
                            text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            text.Append(c);
                        break;
                }
            }
            text.Append('"');
        }

        static void Field(StringBuilder text, string name, string? value, ref bool first)
        {
            if (value == null)
                return;
            if (!first)
                text.Append(",\n");
            first = false;
            text.Append("    ");
            Quoted(text, name);
            text.Append(": ");
            Quoted(text, value);
        }

        public static string WriteEntries(List<Entry> entries)
        {
            if (entries.Count == 0)
                return "[]";
            var text = new StringBuilder("[\n");
            for (int k = 0; k < entries.Count; k++)
            {
                Entry entry = entries[k];
                bool first = true;
                text.Append("  {\n");
                Field(text, "Item", entry.Item, ref first);
                Field(text, "ForcePoseGroup", entry.ForcePoseGroup, ref first);
                Field(text, "ForceDrawItemPose", entry.ForceDrawItemPose, ref first);
                if (entry.SpriteTurn is { } turn)
                {
                    if (!first)
                        text.Append(",\n");
                    first = false;
                    text.Append("    \"SpriteTurn\": ").Append(turn.ToString(CultureInfo.InvariantCulture));
                }
                text.Append("\n  }");
                text.Append(k < entries.Count - 1 ? ",\n" : "\n");
            }
            text.Append(']');
            return text.ToString();
        }
    }
}
