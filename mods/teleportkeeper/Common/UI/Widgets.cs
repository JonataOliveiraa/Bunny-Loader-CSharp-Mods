using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace TeleportKeeperMod.Common.UI;

public static class Colors
{
    public static readonly Color Panel = new(33, 43, 79, 240), Inner = new(44, 57, 105, 230), Button = new(63, 82, 151, 255),
        ButtonHot = new(86, 110, 196, 255), Danger = new(150, 50, 60, 255), DangerHot = new(196, 70, 80, 255), Gold = new(255, 215, 0, 255),
        Black = new(0, 0, 0, 255), White = new(255, 255, 255, 255), Gray = new(150, 150, 165, 255);
}

public static class Press
{
    public static float x, y;
    public static UIElement? target;
}

public static class Widgets
{
    const float Move = 12;

    public static T Place<T>(T el, float? left, float? top, float? width = null, float? height = null) where T : UIElement
    {
        if (left != null)
            el.Left.Set(left.Value, 0);
        if (top != null)
            el.Top.Set(top.Value, 0);
        if (width != null)
            el.Width.Set(width.Value, 0);
        if (height != null)
            el.Height.Set(height.Value, 0);
        return el;
    }

    public static void DrawTexture(Texture2D texture, float x, float y, float w, float h, Color color)
    {
        if (texture.IsNull)
            return;
        Main.spriteBatch.Draw(texture, new Rectangle(Util.Round(x), Util.Round(y), Util.Round(w), Util.Round(h)), color);
    }

    public static void DrawFitted(Texture2D texture, float x, float y, float width, float height, float size)
    {
        if (texture.IsNull)
            return;
        float s = Math.Min(Math.Min(size / texture.Width, size / texture.Height), 2f);
        float w = texture.Width * s, h = texture.Height * s;
        DrawTexture(texture, x + (width - w) / 2, y + (height - h) / 2, w, h, Colors.White);
    }

    public static void DrawFitted(Texture2D texture, CalculatedStyle d, float size) => DrawFitted(texture, d.X, d.Y, d.Width, d.Height, size);

    public static UIText Label(string text, float scale = 1f, Color? color = null)
    {
        var t = new UIText(text, scale);
        if (color != null)
            t.TextColor = color.Value;
        t.IgnoresMouseInteraction = true;
        return t;
    }

    public static bool Tapped(UIMouseEvent evt)
    {
        Vector2 m = evt.MousePosition;
        float dx = m.X - Press.x, dy = m.Y - Press.y;
        return Math.Sqrt(dx * dx + dy * dy) <= Move;
    }
}

public class Button : UIPanel
{
    readonly bool danger;
    readonly Action<Button> onTap;
    readonly UIText label;

    public bool confirm;

    public Button(string text, Action<Button> onTap, bool danger = false, float scale = 0.9f)
    {
        SetPadding(4);
        this.danger = danger;
        this.onTap = onTap;
        label = Widgets.Label(text, scale);
        label.HAlign = 0.5f;
        label.VAlign = 0.5f;
        Append(label);
    }

    public void SetText(string text) => label.SetText(text);

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (Widgets.Tapped(evt))
        {
            Util.MenuTick();
            onTap(this);
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        bool hot = IsMouseHovering;
        BackgroundColor = danger ? (hot ? Colors.DangerHot : Colors.Danger) : hot ? Colors.ButtonHot : Colors.Button;
        BorderColor = Colors.Black;
        base.DrawSelf(spriteBatch);
    }
}

public class TextBox : UIPanel
{
    readonly TextField input = new();
    readonly Action<string, string>? onChange;
    readonly int max;
    readonly UIText label;

    public string placeholder;
    public Color? textColor;

    public TextBox(string placeholder, Action<string, string>? onChange = null, int max = 40)
    {
        SetPadding(8);
        this.placeholder = placeholder;
        this.onChange = onChange;
        this.max = max;
        label = Widgets.Label("", 1f);
        label.VAlign = 0.5f;
        Append(label);
    }

    public string Text
    {
        get => input.text;
        set
        {
            input.text = value ?? "";
            input.old = input.text;
        }
    }

    public void Blur() => input.Blur();

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!Widgets.Tapped(evt) || input.focused)
            return;
        if (input.Focus())
            Util.MenuTick();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        TextField f = input;
        BackgroundColor = f.focused ? Colors.ButtonHot : Colors.Inner;
        BorderColor = f.focused ? Colors.Gold : Colors.Black;
        base.DrawSelf(spriteBatch);
        if (f.focused)
        {
            CalculatedStyle d = GetDimensions();
            string? result = f.Update(d.X, d.Y, d.Width, d.Height, max);
            if (result != null)
                onChange?.Invoke(f.text, result);
        }
        bool caret = f.focused && Main.GameUpdateCount / 20 % 2 == 0;
        bool shown = f.text.Length > 0 || f.focused;
        string showing = shown ? (caret ? f.text + "|" : f.text) : placeholder;
        if (label.Text != showing)
            label.SetText(showing);
        label.TextColor = shown ? textColor ?? Colors.White : Colors.Gray;
    }
}

public class IconButton : UIPanel
{
    readonly string key;
    readonly Func<string, bool> isSelected;
    readonly Action<string> onTap;

    public IconButton(string key, float size, Func<string, bool> isSelected, Action<string> onTap)
    {
        SetPadding(0);
        this.key = key;
        this.isSelected = isSelected;
        this.onTap = onTap;
        Widgets.Place(this, null, null, size, size);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (Widgets.Tapped(evt))
        {
            Util.MenuTick();
            onTap(key);
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        bool selected = isSelected(key);
        BackgroundColor = selected ? Colors.ButtonHot : Colors.Inner;
        BorderColor = selected ? Colors.Gold : Colors.Black;
        base.DrawSelf(spriteBatch);
        CalculatedStyle d = GetDimensions();
        Widgets.DrawFitted(Icons.IconTexture(key), d, d.Width - 10);
    }
}

public class ColorButton : UIPanel
{
    readonly string key;
    readonly Func<string, bool> isSelected;
    readonly Action<string> onTap;

    public ColorButton(string key, Color color, float size, Func<string, bool> isSelected, Action<string> onTap)
    {
        SetPadding(5);
        this.key = key;
        this.isSelected = isSelected;
        this.onTap = onTap;
        Widgets.Place(this, null, null, size, size);
        var swatch = new UIPanel();
        swatch.Width.Set(0, 1);
        swatch.Height.Set(0, 1);
        swatch.BackgroundColor = color;
        swatch.BorderColor = Colors.Black;
        swatch.IgnoresMouseInteraction = true;
        Append(swatch);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (Widgets.Tapped(evt))
        {
            Util.MenuTick();
            onTap(key);
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        bool selected = isSelected(key);
        BackgroundColor = selected ? Colors.ButtonHot : Colors.Inner;
        BorderColor = selected ? Colors.Gold : Colors.Black;
        base.DrawSelf(spriteBatch);
    }
}

public class Chip : UIPanel
{
    readonly Func<Texture2D>? content;
    readonly Func<bool> isSelected;
    readonly Action onTap;

    Chip(float width, float height, Func<bool> isSelected, Action onTap)
    {
        SetPadding(0);
        this.isSelected = isSelected;
        this.onTap = onTap;
        Widgets.Place(this, null, null, width, height);
    }

    public Chip(string text, float width, float height, Func<bool> isSelected, Action onTap) : this(width, height, isSelected, onTap)
    {
        UIText label = Widgets.Label(text, 0.8f);
        label.HAlign = 0.5f;
        label.VAlign = 0.5f;
        Append(label);
    }

    public Chip(Func<Texture2D> content, float width, float height, Func<bool> isSelected, Action onTap) : this(width, height, isSelected, onTap)
    {
        this.content = content;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (Widgets.Tapped(evt))
        {
            Util.MenuTick();
            onTap();
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        bool selected = isSelected();
        BackgroundColor = selected ? Colors.ButtonHot : Colors.Inner;
        BorderColor = selected ? Colors.Gold : Colors.Black;
        base.DrawSelf(spriteBatch);
        if (content != null)
        {
            CalculatedStyle d = GetDimensions();
            Widgets.DrawFitted(content(), d, Math.Min(d.Width, d.Height) - 10);
        }
    }
}

public class CheckBox : UIElement
{
    readonly Func<bool> get;
    readonly Action<bool> set;

    public CheckBox(string text, Func<bool> get, Action<bool> set)
    {
        this.get = get;
        this.set = set;
        UIText label = Widgets.Label(text, 0.9f);
        label.Left.Set(30, 0);
        label.VAlign = 0.5f;
        Append(label);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!Widgets.Tapped(evt))
            return;
        set(!get());
        Util.MenuTick();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetDimensions();
        Texture2D tex = (get() ? TextureAssets.InventoryTickOn : TextureAssets.InventoryTickOff).Value;
        Widgets.DrawTexture(tex, d.X + 2, d.Y + (d.Height - 22) / 2, 22, 22, Colors.White);
    }
}

public class DragList : UIList
{
    bool block;

    public override bool ContainsPoint(Vector2 point) => !block && base.ContainsPoint(point);

    public override void Update(GameTime gameTime)
    {
        bool startedHere = base.ContainsPoint(new Vector2(Press.x, Press.y));
        block = Main.mouseLeft && !startedHere;
        try
        {
            base.Update(gameTime);
        }
        finally
        {
            block = false;
        }
    }
}

public class Backdrop : UIElement
{
    readonly Action onTap;

    public Backdrop(Action onTap)
    {
        Width.Set(0, 1);
        Height.Set(0, 1);
        this.onTap = onTap;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (evt.Target == this && Widgets.Tapped(evt))
            onTap();
    }
}
