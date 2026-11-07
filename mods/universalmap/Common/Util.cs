using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.Localization;

namespace UniversalMapMod.Common;

public static class Util
{
    static readonly HashSet<string> Once = new();

    // Real time in milliseconds: the phone's frame rate says nothing about seconds.
    public static double NowMs => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;

    public static string T(string key, params string[] args)
    {
        string text = Language.GetTextValue("Mods.universalmap." + key) ?? key;
        for (int i = 0; i < args.Length; i++)
            text = text.Replace("{" + i + "}", args[i]);
        return text;
    }

    public static void Chat(string key, byte r, byte g, byte b, params string[] args) =>
        Main.NewText(T("Messages." + key, args), r, g, b);

    public static void Green(string key, params string[] args) => Chat(key, 120, 230, 120, args);

    public static void Orange(string key, params string[] args) => Chat(key, 255, 165, 0, args);

    public static void Log(string text) => Sync.mod?.Logger.Info("[UniversalMap] " + text);

    public static void LogOnce(string key, string text)
    {
        if (Once.Add(key))
            Log(text);
    }
}
