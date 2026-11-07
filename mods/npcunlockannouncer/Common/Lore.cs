using System;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace NPCUnlockAnnouncerMod.Common;

public static class Lore
{
    static string SearchName(int type)
    {
        try
        {
            return NPCID.Search.GetName(type) ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    // A mod NPC's name in NPCID.Search already has the mod in front.
    public static string NpcKey(int type)
    {
        string name = SearchName(type);
        if (name.Length == 0)
            name = type.ToString();
        return name.Contains('/') ? name : "Terraria/" + name;
    }

    static string NpcName(int type)
    {
        try
        {
            return Lang.GetNPCNameValue(type) ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    public static (string Title, string Subtitle) GetLore(int type)
    {
        string name = SearchName(type).Split('/').Last();
        string key = "NPCTitles." + name;

        string title = name.Length > 0 ? Util.Text(key + ".Title") : "";
        if (title.Length == 0)
            title = NpcName(type).ToUpperInvariant();
        if (title.Length == 0)
            title = name.ToUpperInvariant();

        string pool = name.Length > 0 ? Util.Text(key + ".Subtitle") : "";
        if (pool.Length == 0)
            pool = Util.Text("NPCTitles.Default.Subtitle");
        string[] options = pool.Split('|').Where(s => s.Trim().Length > 0).ToArray();
        string subtitle = options.Length > 0 ? options[Random.Shared.Next(options.Length)] : "New NPC Unlocked!";
        return (title, subtitle);
    }
}
