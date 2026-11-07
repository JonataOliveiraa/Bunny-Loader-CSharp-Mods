using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BestiaryDisplaysMod.Common;

public static class Util
{
    public const string ModId = "bestiarydisplays";

    static readonly HashSet<string> once = new();
    static readonly Dictionary<string, SoundStyle> sounds = new();

    public static string Message(Exception e)
    {
        string text = e.Message;
        return text.Length > 300 ? text[..300] : text;
    }

    public static void Log(string text) => ModContent.GetInstance<BestiaryDisplays>().Logger.Info(text);

    public static void LogOnce(string key, string text)
    {
        if (once.Add(key))
            Log(text);
    }

    public static void Safe(string label, Action fn)
    {
        try
        {
            fn();
        }
        catch (Exception e)
        {
            LogOnce(label + ":" + Message(e), label + " failed: " + Message(e));
        }
    }

    public static T Safe<T>(string label, Func<T> fn, T fallback)
    {
        try
        {
            return fn();
        }
        catch (Exception e)
        {
            LogOnce(label + ":" + Message(e), label + " failed: " + Message(e));
            return fallback;
        }
    }

    public static string T(string key) => Language.GetTextValue("Mods." + ModId + "." + key) ?? key;

    public static void PlaySound(string name)
    {
        if (Main.netMode == 2)
            return;
        if (!sounds.TryGetValue(name, out SoundStyle style))
            sounds[name] = style = new SoundStyle(ModId + "/Assets/Sounds/" + name);
        SoundEngine.PlaySound(style);
    }

    public static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));

    public static int Round(double value) => (int)Math.Floor(value + 0.5);
}
