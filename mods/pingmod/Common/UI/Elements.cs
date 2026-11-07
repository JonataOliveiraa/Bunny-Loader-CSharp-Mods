using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace PingModPort.Common.UI;

public class DraggablePanel : UIPanel
{
    const float DRAG_THRESHOLD = 8;

    public static readonly Color PANEL_COLOR = new(63, 82, 151, 222);

    public Action? onTap;
    public bool locked;
    public bool dragging;
    Vector2? down;
    Vector2 offset;

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        if (evt.Target != this)
            return;
        CalculatedStyle d = GetDimensions();
        down = evt.MousePosition;
        offset = new Vector2(evt.MousePosition.X - d.X, evt.MousePosition.Y - d.Y);
        dragging = false;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        if (down == null)
            return;
        bool tapped = !dragging;
        down = null;
        dragging = false;
        if (tapped)
            onTap?.Invoke();
    }

    public void MoveTo(float x, float y)
    {
        CalculatedStyle parent = Parent!.GetDimensions();
        CalculatedStyle d = GetDimensions();
        HAlign = 0;
        VAlign = 0;
        Left.Set(Math.Min(Math.Max(x - parent.X, 0), parent.Width - d.Width), 0);
        Top.Set(Math.Min(Math.Max(y - parent.Y, 0), parent.Height - d.Height), 0);
        Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
        if (down is not Vector2 start)
            return;
        if (!Main.mouseLeft)
        {
            down = null;
            dragging = false;
            return;
        }
        float dx = Main.mouseX - start.X;
        float dy = Main.mouseY - start.Y;
        if (locked)
            return;
        if (!dragging && dx * dx + dy * dy > DRAG_THRESHOLD * DRAG_THRESHOLD)
            dragging = true;
        if (dragging)
            MoveTo(Main.mouseX - offset.X, Main.mouseY - offset.Y);
    }
}

public class TextButton : UIPanel
{
    static readonly Color Hovered = new(255, 230, 120);

    public TextButton(string text, float width, float height, Action onClick)
    {
        SetPadding(0);
        Width.Set(width, 0);
        Height.Set(height, 0);
        var label = new UIText(text, 1f);
        label.HAlign = 0.5f;
        label.VAlign = 0.5f;
        label.IgnoresMouseInteraction = true;
        Append(label);
        OnLeftClick += (evt, element) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick, -1, -1, 1, 1f, 0f);
            onClick();
        };
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        BorderColor = IsMouseHovering ? Hovered : Color.Black;
    }
}

public static class Layout
{
    public static T Place<T>(T element, float left, float top, float? width = null, float? height = null) where T : UIElement
    {
        element.Left.Set(left, 0);
        element.Top.Set(top, 0);
        if (width != null)
            element.Width.Set(width.Value, 0);
        if (height != null)
            element.Height.Set(height.Value, 0);
        return element;
    }
}
