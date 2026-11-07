using System.Collections.Generic;
using System.Diagnostics;
using Terraria.Localization;
using Terraria.ModLoader;

namespace WorldGenPreviewerMod.Common;

public static class Util
{
    static readonly Dictionary<string, string> Texts = new();
    static readonly HashSet<string> Once = new();

    public static Mod? mod;

    // Real time in milliseconds, from any thread.
    public static double NowMs => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;

    // Math.round from the original: halves go up.
    public static int Round(double value) => (int)System.Math.Floor(value + 0.5);

    // Only the screen, on the game thread, reads texts. They are kept because it draws each frame.
    public static string T(string key)
    {
        if (!Texts.TryGetValue(key, out string? text))
        {
            text = Language.GetTextValue("Mods.worldgenpreviewer." + key) ?? key;
            Texts[key] = text;
        }
        return text;
    }

    public static string T(string key, string[] args)
    {
        string text = T(key);
        for (int i = 0; i < args.Length; i++)
            text = text.Replace("{" + i + "}", args[i]);
        return text;
    }

    public static void ClearTexts() => Texts.Clear();

    public static void Log(string text) => mod?.Logger.Info("[WorldGenPreviewer] " + text);

    public static void LogOnce(string key, string text)
    {
        lock (Once)
        {
            if (!Once.Add(key))
                return;
        }
        Log(text);
    }
}
