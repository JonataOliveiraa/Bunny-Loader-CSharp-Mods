using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TeleportKeeperMod.Common;

public static class Util
{
    public const string ModId = "teleportkeeper";

    static readonly HashSet<string> once = new();

    public static void LogOnce(string key, string text)
    {
        if (once.Add(key))
            ModContent.GetInstance<TeleportKeeper>().Logger.Info(text);
    }

    public static void Safe(string label, Action fn)
    {
        try
        {
            fn();
        }
        catch (Exception e)
        {
            LogOnce(label + ":" + e.Message, label + " failed: " + e);
        }
    }

    // {0}, {1}... are replaced by hand: names typed by players may hold anything.
    public static string L(string key, params object[] args)
    {
        string text = Language.GetTextValue("Mods." + ModId + "." + key) ?? key;
        for (int i = 0; i < args.Length; i++)
            text = text.Replace("{" + i + "}", Convert.ToString(args[i]));
        return text;
    }

    public static string T(string key, params object[] args) => L("Messages." + key, args);

    // A dedicated server has nobody to show this to. The phone's host (mode 3) does.
    public static void Say(string key, byte r, byte g, byte b, params object[] args)
    {
        if (Main.netMode == 2)
            return;
        Main.NewText(T(key, args), r, g, b, false);
    }

    public static int Mode => Main.netMode;
    public static bool IsClient => Main.netMode == 1;
    public static bool IsServer => (Main.netMode & NetmodeID.Server) != 0;

    public static Player Local => Main.player[Main.myPlayer];

    static Vector2 ListenerPosition
    {
        get
        {
            Player p = Local;
            return p.IsNull ? Vector2.Zero : p.Center;
        }
    }

    public static void MenuTick()
    {
        if (Main.netMode != 2)
            SoundEngine.PlaySound(SoundID.MenuTick, ListenerPosition, 1, 0f);
    }

    public static void TeleportSound()
    {
        if (Main.netMode != 2)
            SoundEngine.PlaySound(SoundID.Item6, ListenerPosition, 0f, 1f);
    }

    public static int Round(double value) => (int)Math.Floor(value + 0.5);
}
