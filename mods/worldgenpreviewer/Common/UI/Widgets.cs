using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace WorldGenPreviewerMod.Common.UI;

public static class Colors
{
    public static readonly Color Background = new(13, 17, 38, 255);
    public static readonly Color Panel = new(44, 57, 106, 235);
    public static readonly Color PanelLight = new(63, 82, 151, 255);
    public static readonly Color Hot = new(98, 124, 214, 255);
    public static readonly Color Edge = new(20, 24, 50, 255);
    public static readonly Color Gold = new(255, 225, 110, 255);
    public static readonly Color Status = new(255, 240, 160, 255);
    public static readonly Color Dim = new(110, 110, 125, 160);
    public static readonly Color Done = new(255, 110, 110, 255);
    public static readonly Color Skipped = new(255, 240, 80, 255);
    public static readonly Color Marked = new(140, 140, 150, 255);
    public static readonly Color Running = new(255, 255, 255, 40);
    public static readonly Color Scan = new(255, 182, 193, 255);
    public static readonly Color Structure = new(0, 120, 0, 120);
    public static readonly Color Protected = new(170, 0, 0, 120);
}

public static class Widgets
{
    static readonly Dictionary<string, float> Widths = new();
    static readonly Dictionary<string, Texture2D> Icons = new();

    public static SpriteFont Font => FontAssets.MouseText.Value;

    public static void Fill(SpriteBatch spriteBatch, float x, float y, float w, float h, Color color)
    {
        if (w <= 0 || h <= 0)
            return;
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(Util.Round(x), Util.Round(y), Util.Round(w), Util.Round(h)), color);
    }

    public static void Frame(SpriteBatch spriteBatch, float x, float y, float w, float h, Color color, float t = 2)
    {
        Fill(spriteBatch, x, y, w, t, color);
        Fill(spriteBatch, x, y + h - t, w, t, color);
        Fill(spriteBatch, x, y, t, h, color);
        Fill(spriteBatch, x + w - t, y, t, h, color);
    }

    public static void Image(SpriteBatch spriteBatch, Texture2D texture, float x, float y, float w, float h, Color color)
    {
        if (texture.IsNull || w <= 0 || h <= 0)
            return;
        spriteBatch.Draw(texture, new Rectangle(Util.Round(x), Util.Round(y), Util.Round(w), Util.Round(h)), color);
    }

    public static float Width(string text, float scale = 1)
    {
        if (!Widths.TryGetValue(text, out float w))
        {
            w = Font.MeasureString(text).X;
            if (Widths.Count > 2000)
                Widths.Clear();
            Widths[text] = w;
        }
        return w * scale;
    }

    public static void Text(SpriteBatch spriteBatch, string text, float x, float y, Color color, float scale = 1)
    {
        if (text.Length == 0)
            return;
        Utils.DrawBorderStringFourWay(spriteBatch, Font, text, x, y, color, Color.Black, Vector2.Zero, scale);
    }

    public static string Fit(string text, float maxWidth, float scale = 1)
    {
        if (Width(text, scale) <= maxWidth)
            return text;
        int lo = 0, hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (Width(text.Substring(0, mid) + "...", scale) <= maxWidth)
                lo = mid;
            else
                hi = mid - 1;
        }
        return text.Substring(0, lo) + "...";
    }

    public static string Tr(string key) => Util.T("UI." + key);

    public static string Tr(string key, params string[] args) => Util.T("UI." + key, args);

    public static Texture2D Icon(string name)
    {
        if (!Icons.TryGetValue(name, out Texture2D texture))
        {
            texture = ModContent.Request<Texture2D>("worldgenpreviewer/Assets/Textures/" + name).Value;
            Icons[name] = texture;
        }
        return texture;
    }

    public static T Place<T>(T element, float left, float top, float width, float height) where T : UIElement
    {
        element.Left.Set(left, 0);
        element.Top.Set(top, 0);
        element.Width.Set(width, 0);
        element.Height.Set(height, 0);
        return element;
    }
}

public static class Hint
{
    public static string? text;
    public static string? shown;
}

public class IconButton : UIElement
{
    readonly string icon;
    readonly Func<string> tip;
    readonly Action onTap;
    readonly Func<bool> enabled;
    readonly Func<bool> on;
    bool pressed;

    public IconButton(string icon, Func<string> tip, Action onTap, Func<bool> enabled, Func<bool> on)
    {
        this.icon = icon;
        this.tip = tip;
        this.onTap = onTap;
        this.enabled = enabled;
        this.on = on;
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        pressed = true;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        pressed = false;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!enabled())
            return;
        SoundEngine.PlaySound(12, -1, -1, 1, 1f, 0f);
        onTap();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetDimensions();
        bool isEnabled = enabled(), isOn = on();
        float t = Math.Max(1, Util.Round(d.Width / 24));
        Widgets.Fill(spriteBatch, d.X, d.Y, d.Width, d.Height, pressed && isEnabled ? Colors.Hot : isOn ? Colors.PanelLight : Colors.Panel);
        Widgets.Frame(spriteBatch, d.X, d.Y, d.Width, d.Height, isOn ? Colors.Gold : Colors.Edge, t);
        float s = Util.Round(d.Width * 0.72f);
        Widgets.Image(spriteBatch, Widgets.Icon(icon), d.X + (d.Width - s) / 2, d.Y + (d.Height - s) / 2, s, s, isEnabled ? Color.White : Colors.Dim);
        if (pressed)
            Hint.text = tip();
    }
}

public class DynamicText : UIElement
{
    readonly Func<string> text;
    readonly Color color;
    readonly float scale;
    readonly float align;

    public DynamicText(Func<string> text, Color color, float scale, float align = 0)
    {
        this.text = text;
        this.color = color;
        this.scale = scale;
        this.align = align;
        IgnoresMouseInteraction = true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetDimensions();
        string fitted = Widgets.Fit(text(), d.Width, scale);
        float w = Widgets.Width(fitted, scale);
        float x = d.X + (d.Width - w) * align;
        Widgets.Text(spriteBatch, fitted, x, d.Y + (d.Height - 22 * scale) / 2, color, scale);
    }
}

public class Box : UIElement
{
    readonly Color color;
    readonly Color? border;

    public Box(Color color, Color? border = null)
    {
        this.color = color;
        this.border = border;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetDimensions();
        Widgets.Fill(spriteBatch, d.X, d.Y, d.Width, d.Height, color);
        if (border is Color edge)
            Widgets.Frame(spriteBatch, d.X, d.Y, d.Width, d.Height, edge, 2);
    }
}
