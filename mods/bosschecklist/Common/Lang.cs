using System;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.Localization;

namespace BossChecklistMod.Common;

// The original's Lang.js. Named ModText because Terraria.Lang is in scope everywhere.
public static class ModText
{
    const string Prefix = "Mods." + Util.ModId + ".";

    static readonly Regex Reference = new(@"\{\$([A-Za-z0-9_.]+)\}");
    static readonly Regex PluralTag = new(@"\{\^(\d+):([^}]*)\}");
    static readonly Regex Placeholder = new(@"\{(\d+)(?::[^}]*)?\}");
    static readonly Regex ChatTag = new(@"\[([a-zA-Z]+)(/[^:\]]*)?:([^\]]*)\]");

    static string Raw(string key) => Language.GetTextValue(key) ?? key;

    static bool Usable(string? value, string key) => !string.IsNullOrEmpty(value) && value != key && value != "null";

    public static string Vanilla(string key, object? arg = null)
    {
        string value = arg == null ? Raw(key) : Language.GetTextValue(key, new[] { arg });
        return Usable(value, key) ? value : key;
    }

    public static string NPCName(int type)
    {
        string? value = Lang.GetNPCNameValue(type);
        return Usable(value, "") ? value! : "";
    }

    public static string Resolve(string key)
    {
        if (key.StartsWith("Mods.", StringComparison.Ordinal))
            return Raw(key);
        string full = Prefix + key;
        string own = Raw(full);
        return own != full ? own : Vanilla(key);
    }

    static string Plural(object? n, string forms)
    {
        string[] parts = forms.Split(';');
        double number = n == null ? 0 : Convert.ToDouble(n);
        long v = (long)Math.Abs(Math.Truncate(number));
        if (parts.Length >= 3)
        {
            long a = v % 10, b = v % 100;
            if (a == 1 && b != 11)
                return parts[0];
            if (a >= 2 && a <= 4 && (b < 12 || b > 14))
                return parts[1];
            return parts[2];
        }
        return parts[v == 1 ? 0 : Math.Min(1, parts.Length - 1)];
    }

    public static string Format(string text, params object?[] args)
    {
        if (text.IndexOf('{') < 0)
            return text;
        text = Reference.Replace(text, m => Resolve(m.Groups[1].Value));
        text = PluralTag.Replace(text, m =>
        {
            int i = int.Parse(m.Groups[1].Value);
            return Plural(i < args.Length ? args[i] : null, m.Groups[2].Value);
        });
        return Placeholder.Replace(text, m =>
        {
            int i = int.Parse(m.Groups[1].Value);
            return i < args.Length ? Convert.ToString(args[i]) ?? "null" : m.Value;
        });
    }

    public static string L(string key, params object?[] args) => Format(Raw(Prefix + key), args);

    public static bool HasKey(string key)
    {
        string full = Prefix + key;
        return Raw(full) != full;
    }

    public static string Text(string key, params object?[] args) => Format(Resolve(key), args);

    public static string StripTags(string text) =>
        ChatTag.Replace(text, m => m.Groups[1].Value == "i" ? "" : m.Groups[3].Value);
}
