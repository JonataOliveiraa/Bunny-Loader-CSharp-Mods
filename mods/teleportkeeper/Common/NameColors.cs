using Microsoft.Xna.Framework.Graphics;

namespace TeleportKeeperMod.Common;

public sealed class NameColorInfo
{
    public readonly string key;
    public readonly byte r, g, b;

    public NameColorInfo(string key, byte r, byte g, byte b)
    {
        this.key = key;
        this.r = r;
        this.g = g;
        this.b = b;
    }
}

public static class NameColors
{
    public static readonly NameColorInfo[] NAME_COLORS =
    {
        new("White", 255, 255, 255),
        new("Yellow", 255, 230, 80),
        new("Orange", 255, 150, 40),
        new("Red", 255, 80, 80),
        new("Pink", 255, 120, 200),
        new("Purple", 190, 110, 255),
        new("Blue", 90, 150, 255),
        new("Cyan", 80, 230, 240),
        new("Green", 100, 240, 100),
        new("Gray", 170, 170, 180),
    };

    public static NameColorInfo NameColorOf(string? key)
    {
        foreach (NameColorInfo c in NAME_COLORS)
        {
            if (c.key == key)
                return c;
        }
        return NAME_COLORS[0];
    }

    public static string NameColorName(string? key) => Util.L("Colors." + NameColorOf(key).key);

    public static Color NameColor(string? key)
    {
        NameColorInfo c = NameColorOf(key);
        return new Color((int)c.r, c.g, c.b, 255);
    }

    public static string NameColorHex(string? key)
    {
        NameColorInfo c = NameColorOf(key);
        return c.r.ToString("x2") + c.g.ToString("x2") + c.b.ToString("x2");
    }
}
