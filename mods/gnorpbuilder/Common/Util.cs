using Terraria.Localization;

namespace GnorpBuilderMod.Common;

public static class Util
{
    public const string ModName = "gnorpbuilder";
    public const string KeyPrefix = "Mods." + ModName + ".";

    // A text of this mod, with {0}, {1}... filled in.
    public static string T(string key, params string[] args)
    {
        string text = Language.GetTextValue(KeyPrefix + key) ?? key;
        for (int i = 0; i < args.Length; i++)
            text = text.Replace("{" + i + "}", args[i]);
        return text;
    }
}
