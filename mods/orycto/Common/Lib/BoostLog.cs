using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace OryctoMod.Common.Lib;

public static class BoostLog
{
    static readonly HashSet<string> Warned = new();

    public static void Info(string text) => ModContent.GetInstance<BunnyBoost>().Logger.Info(text);

    public static void WarnOnce(string key, Exception error)
    {
        if (Warned.Add(key))
            ModContent.GetInstance<BunnyBoost>().Logger.Warn(key + ": " + error.Message);
    }
}
