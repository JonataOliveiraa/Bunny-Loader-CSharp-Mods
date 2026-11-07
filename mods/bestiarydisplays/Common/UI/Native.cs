using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace BestiaryDisplaysMod.Common.UI;

public readonly struct Box
{
    public readonly float x, y, w, h;

    public Box(float x, float y, float w, float h)
    {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }
}

public enum PanelStyle
{
    Window, Cell, CellSelected, Bar, Image,
}

public readonly struct ClickState
{
    public readonly bool over, hot, clicked;

    public ClickState(bool over, bool hot, bool clicked)
    {
        this.over = over;
        this.hot = hot;
        this.clicked = clicked;
    }
}

public static class Pointer
{
    const float Drag = 10;

    public static float x, y, startX, startY;
    public static bool down, was, pressed, released, dragged, armed;

    static Vector2 MousePosition()
    {
        GUIInputRegionManager manager = GUIInputRegionManager.Instance;
        if (!manager.IsNull)
        {
            Vector2 p = manager.MousePosition;
            if (float.IsFinite(p.X) && float.IsFinite(p.Y))
                return p;
        }
        float k = Math.Max(0.1f, Main.UIScale);
        return new Vector2(Main.mouseX / k, Main.mouseY / k);
    }

    public static void Update()
    {
        Vector2 p = MousePosition();
        x = p.X;
        y = p.Y;
        bool now = Main.mouseLeft;
        pressed = now && !was;
        released = !now && was;
        down = now;
        was = now;
        if (pressed)
        {
            armed = true;
            startX = x;
            startY = y;
            dragged = false;
        }
        else if (down && Distance(x - startX, y - startY) > Drag)
        {
            dragged = true;
        }
        if (!armed)
            released = false;
    }

    static double Distance(float dx, float dy) => Math.Sqrt(dx * dx + dy * dy);

    public static void Disarm()
    {
        armed = false;
        was = Main.mouseLeft;
    }

    public static bool In(Box r) => InAt(r, x, y);

    static bool InAt(Box r, float px, float py) => px >= r.x && px < r.x + r.w && py >= r.y && py < r.y + r.h;

    public static bool StartedIn(Box r) => InAt(r, startX, startY);
}

public static class Native
{
    const int ScreenControl = 0;

    public static Box Screen()
    {
        Rectangle r = ControlAnchor.GetControlRegion((ControlAnchor.ControlId)ScreenControl);
        if (r.Width > 0 && r.Height > 0)
            return new Box(r.X, r.Y, r.Width, r.Height);
        float k = Math.Max(0.1f, Main.UIScale);
        return new Box(0, 0, Main.screenWidth / k, Main.screenHeight / k);
    }

    public static bool Region(Box r)
    {
        GUIInputRegionManager manager = GUIInputRegionManager.Instance;
        bool over = manager.IsNull
            ? Pointer.In(r)
            : manager.RegisterInputRegion(new Rectangle(Util.Round(r.x), Util.Round(r.y), Util.Round(r.w), Util.Round(r.h)));
        if (over)
            Main.LocalPlayer.mouseInterface = true;
        return over;
    }

    public static ClickState Clicked(Box r)
    {
        bool over = Region(r);
        bool hot = over && Pointer.down && Pointer.StartedIn(r);
        bool clicked = Pointer.released && !Pointer.dragged && Pointer.StartedIn(r) && Pointer.In(r);
        return new ClickState(over, hot, clicked);
    }

    static Panel_Layout Template(PanelStyle style)
    {
        Bestiary_Layout layout = Bestiary_Layout.Instance;
        if (layout.IsNull)
            return default;
        return style switch
        {
            PanelStyle.Window => layout.Backing,
            PanelStyle.Cell => layout.ItemBacking,
            PanelStyle.CellSelected => layout.ItemSelectedBacking,
            PanelStyle.Bar => layout.EntryNameBacking,
            _ => layout.EntryImage,
        };
    }

    // Draws the game's own panel. When there is no such layout, the caller's flat fallback draws.
    public static bool Panel(PanelStyle style, float x, float y, float w, float h, bool over)
    {
        try
        {
            Panel_Layout layout = Template(style);
            if (layout.IsNull)
                return false;
            GUIPanel.DrawAtPosition(layout, new Vector2(Util.Round(x), Util.Round(y)), new Vector2(Util.Round(w), Util.Round(h)), over);
            return true;
        }
        catch (Exception e)
        {
            Util.LogOnce("panel:" + style, "GUIPanel.DrawAtPosition failed: " + Util.Message(e));
            return false;
        }
    }

    public static void Flush() => Main.spriteBatch.Flush();
}
