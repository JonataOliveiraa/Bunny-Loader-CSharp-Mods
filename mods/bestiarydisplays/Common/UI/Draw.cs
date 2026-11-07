using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace BestiaryDisplaysMod.Common.UI;

public static class D
{
    public static Color Rgb(int r, int g, int b, int a = 255) => new(r, g, b, a);

    static Color Mul(int r, int g, int b, double f) =>
        new(Util.Round(r * f), Util.Round(g * f), Util.Round(b * f), Util.Round(255 * f));

    public static readonly Color White = Rgb(255, 255, 255), Black = Rgb(0, 0, 0), Yellow = Rgb(255, 255, 0),
        PanelBorder = Rgb(18, 18, 31), Button = Mul(63, 82, 151, 0.85), ButtonHover = Rgb(73, 94, 171), ButtonBorder = Rgb(0, 0, 0),
        Field = Rgb(25, 28, 45), FieldActive = Rgb(40, 45, 80);

    static Texture2D Pixel => TextureAssets.MagicPixel.Value;
    static SpriteFont Font => FontAssets.MouseText.Value;
    static SpriteBatch Batch => Main.spriteBatch;

    public static void Fill(float x, float y, float w, float h, Color color)
    {
        if (w <= 0 || h <= 0)
            return;
        Batch.Draw(Pixel, new Rectangle(Util.Round(x), Util.Round(y), Util.Round(w), Util.Round(h)), color);
    }

    public static void Border(float x, float y, float w, float h, Color color, float t = 2)
    {
        Fill(x, y, w, t, color);
        Fill(x, y + h - t, w, t, color);
        Fill(x, y, t, h, color);
        Fill(x + w - t, y, t, h, color);
    }

    public static void Panel(float x, float y, float w, float h, Color back, Color border, float t = 2)
    {
        Fill(x, y, w, h, back);
        Border(x, y, w, h, border, t);
    }

    static readonly Dictionary<string, Vector2> measureCache = new();

    public static Vector2 Measure(string text, float scale = 1)
    {
        if (!measureCache.TryGetValue(text, out Vector2 size))
        {
            size = Font.MeasureString(text);
            if (measureCache.Count > 4000)
                measureCache.Clear();
            measureCache[text] = size;
        }
        return size * scale;
    }

    public static void Text(string text, float x, float y, Color color, float scale = 1)
    {
        if (text.Length == 0)
            return;
        Terraria.Utils.DrawBorderStringFourWay(Batch, Font, text, x, y, color, Black, Vector2.Zero, scale);
    }

    public static string Truncate(string text, float maxWidth, float scale = 1)
    {
        if (maxWidth <= 0)
            return "";
        if (Measure(text, scale).X <= maxWidth)
            return text;
        int lo = 0, hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (Measure(text[..mid] + "...", scale).X <= maxWidth)
                lo = mid;
            else
                hi = mid - 1;
        }
        return lo <= 0 ? "..." : text[..lo] + "...";
    }

    static readonly Regex Wide = new("[　-鿿가-힯＀-￯]");

    public static List<string> Wrap(string text, float maxWidth, float scale = 1)
    {
        var lines = new List<string>();
        foreach (string paragraph in text.Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' '))
            {
                bool wide = Wide.IsMatch(word);
                int pieces = wide ? word.Length : 1;
                for (int i = 0; i < pieces; i++)
                {
                    string piece = wide ? word[i].ToString() : word;
                    string next = line.Length > 0 ? line + (wide ? "" : " ") + piece : piece;
                    if (line.Length > 0 && Measure(next, scale).X > maxWidth)
                    {
                        lines.Add(line);
                        line = piece;
                    }
                    else
                    {
                        line = next;
                    }
                }
            }
            lines.Add(line);
        }
        return lines;
    }
}
