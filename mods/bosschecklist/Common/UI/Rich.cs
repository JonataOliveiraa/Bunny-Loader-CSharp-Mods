using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Graphics;

namespace BossChecklistMod.Common.UI;

public sealed class RichToken
{
    public string text = "";
    public Color color;
    public int item;
    public bool space;
    public float x, w;
}

public sealed class RichLayout
{
    public readonly List<List<RichToken>> lines = new() { new List<RichToken>() };
    public float lineH, height, icon;

    public float MeasuredWidth()
    {
        float width = 0;
        foreach (List<RichToken> line in lines)
        {
            foreach (RichToken t in line)
                width = Math.Max(width, t.x + t.w);
        }
        return width;
    }
}

// Text with the game's [c/hex:...] and [i:id] tags, wrapped by hand.
public static class Rich
{
    static readonly Regex Tag = new(@"\[([a-zA-Z]+)(?:/([^:\]]*))?:([^\]]*)\]");

    static Color Hex(string hex, Color fallback)
    {
        if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int v))
            return fallback;
        return D.Rgb((v >> 16) & 255, (v >> 8) & 255, v & 255);
    }

    static bool IsWide(char c) => (c >= '　' && c <= '鿿') || (c >= '가' && c <= '힯') || (c >= '＀' && c <= '￯');

    static bool AnyWide(string part)
    {
        foreach (char c in part)
        {
            if (IsWide(c))
                return true;
        }
        return false;
    }

    // Tokens of a text segment: words and runs of spaces, with wide scripts split by character. A null marks a line break.
    static void AddText(List<RichToken?> tokens, string segment, Color color)
    {
        string[] lines = segment.Split('\n');
        for (int li = 0; li < lines.Length; li++)
        {
            if (li > 0)
                tokens.Add(null);
            string line = lines[li];
            int i = 0;
            while (i < line.Length)
            {
                int start = i;
                bool space = char.IsWhiteSpace(line[i]);
                while (i < line.Length && char.IsWhiteSpace(line[i]) == space)
                    i++;
                string part = line[start..i];
                if (!space && AnyWide(part))
                {
                    TextElementEnumerator characters = StringInfo.GetTextElementEnumerator(part);
                    while (characters.MoveNext())
                        tokens.Add(new RichToken { text = characters.GetTextElement(), color = color });
                }
                else
                {
                    tokens.Add(new RichToken { text = part, color = color, space = space });
                }
            }
        }
    }

    static List<RichToken?> Tokens(string src, Color color)
    {
        var tokens = new List<RichToken?>();
        int last = 0;
        foreach (Match m in Tag.Matches(src))
        {
            if (m.Index > last)
                AddText(tokens, src[last..m.Index], color);
            string tag = m.Groups[1].Value.ToLowerInvariant();
            if (tag == "i" || tag == "item")
            {
                if (int.TryParse(m.Groups[3].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) && id > 0)
                    tokens.Add(new RichToken { item = id });
            }
            else if (tag == "c" || tag == "color")
            {
                AddText(tokens, m.Groups[3].Value, Hex(m.Groups[2].Value, color));
            }
            else
            {
                AddText(tokens, m.Groups[3].Value, color);
            }
            last = m.Index + m.Length;
        }
        if (last < src.Length)
            AddText(tokens, src[last..], color);
        return tokens;
    }

    public static RichLayout Layout(string text, float width, float scale, Color? color = null)
    {
        var layout = new RichLayout { lineH = 24 * scale, icon = 22 * scale };
        float x = 0;
        foreach (RichToken? t in Tokens(text, color ?? C.White))
        {
            if (t == null)
            {
                layout.lines.Add(new List<RichToken>());
                x = 0;
                continue;
            }
            float w = t.item != 0 ? layout.icon + 2 * scale : D.Measure(t.text, scale).X;
            if (x > 0 && x + w > width && !t.space)
            {
                layout.lines.Add(new List<RichToken>());
                x = 0;
            }
            if (x == 0 && t.space)
                continue;
            t.x = x;
            t.w = w;
            layout.lines[^1].Add(t);
            x += w;
        }
        layout.height = layout.lines.Count * layout.lineH;
        return layout;
    }

    // Draws lines first..first+count. Item icons inside the vertical bounds show their tooltip under the finger.
    public static void Draw(RichLayout layout, float x, float y, float scale, bool hoverItems = false, int first = 0, int count = int.MaxValue,
        float boundTop = float.MinValue, float boundBottom = float.MaxValue)
    {
        int end = count == int.MaxValue ? layout.lines.Count : Math.Min(layout.lines.Count, first + count);
        for (int i = first; i < end; i++)
        {
            float ly = y + (i - first) * layout.lineH;
            foreach (RichToken t in layout.lines[i])
            {
                if (t.item != 0)
                {
                    float rx = x + t.x, ry = ly + 1 * scale, size = layout.icon;
                    D.ItemIcon(t.item, rx + size / 2, ry + size / 2, size, scale * 1.2f, C.White);
                    if (hoverItems && ry + size > boundTop && ry < boundBottom && Gesture.Inside(rx, ry, size, size))
                        Hover.Item(t.item);
                }
                else if (!t.space)
                {
                    D.Text(t.text, x + t.x, ly + 2 * scale, t.color, scale);
                }
            }
        }
    }
}
