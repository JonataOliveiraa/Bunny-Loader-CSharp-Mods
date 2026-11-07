using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LootBeamsMod.Common;

public readonly record struct Rgb(int R, int G, int B);

public static class Util
{
    static readonly Regex Hex = new("^#?([0-9a-f]{6})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Rounds .5 up, as JavaScript does, so colors keep the same values.
    public static int Round(double value) => (int)Math.Floor(value + 0.5);

    public static Rgb? ParseHex(string? text)
    {
        Match match = Hex.Match((text ?? "").Trim());
        if (!match.Success)
            return null;
        int n = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new Rgb((n >> 16) & 255, (n >> 8) & 255, n & 255);
    }

    public static string ToHex(Rgb rgb) => $"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}";
}
