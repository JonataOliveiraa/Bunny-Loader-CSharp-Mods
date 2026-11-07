using System;
using System.Collections.Generic;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NPCUnlockAnnouncerMod.Common;

public static class Util
{
    public const string ModId = "npcunlockannouncer";

    static readonly HashSet<string> once = new();

    public static void LogOnce(string key, string text)
    {
        if (once.Add(key))
            ModContent.GetInstance<NPCUnlockAnnouncer>().Logger.Warn(text);
    }

    public static void Safe(string label, Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            LogOnce(label + ":" + e.Message, label + " failed: " + e.Message);
        }
    }

    public static string Text(string key)
    {
        string full = "Mods." + ModId + "." + key;
        // A missing key comes back as the key itself.
        string? value = Language.GetTextValue(full);
        return string.IsNullOrEmpty(value) || value == full ? "" : value;
    }
}
