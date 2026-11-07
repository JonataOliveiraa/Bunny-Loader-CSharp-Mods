using System.Text;
using Terraria.ModLoader;

namespace FlatSeedMod.Common;

public static class Util
{
    public const string ModId = "flatseed";

    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        var result = new StringBuilder(text.Length);
        foreach (char c in text.ToLowerInvariant())
        {
            if (char.IsWhiteSpace(c) || c == '﻿' || "-_'\".,!?:;".IndexOf(c) >= 0)
                continue;
            result.Append(c);
        }
        return result.ToString();
    }

    public static string T(string key) => ModContent.GetInstance<FlatSeed>().GetLocalization(key).Value ?? key;
}
