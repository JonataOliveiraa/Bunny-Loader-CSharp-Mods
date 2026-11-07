using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace BossChecklistMod.Common;

public static class Util
{
    public const string ModId = "bosschecklist";

    static readonly HashSet<string> once = new();

    public static string Message(Exception e)
    {
        string text = e.Message;
        return text.Length > 300 ? text[..300] : text;
    }

    public static void Log(string text) => ModContent.GetInstance<BossChecklist>().Logger.Info(text);

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

    public static void Say(string? text, Color color)
    {
        if (Main.netMode == 2 || string.IsNullOrEmpty(text))
            return;
        Main.NewText(text, color.R, color.G, color.B, false);
    }

    public static int Mode => Main.netMode;
    public static bool IsSP => Main.netMode == 0;
    public static bool IsClient => Main.netMode == 1;
    // 3 is the phone's hosted game: the server and a local player in one process.
    public static bool IsServer => Main.netMode == 2 || Main.netMode == 3;

    public static Player Local => Main.player[Main.myPlayer];

    public static void PlaySound(int id)
    {
        if (Main.netMode == 2)
            return;
        Player p = Local;
        SoundEngine.PlaySound(id, p.IsNull ? Vector2.Zero : p.Center, 1, 0f);
    }
}
