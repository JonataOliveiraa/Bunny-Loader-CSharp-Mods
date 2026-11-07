using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace BossChecklistMod.Common.UI;

// An element that reacts to a long press. Returns false to let its parent try.
public interface ILongPress
{
    bool LongPress();
}

public static class Gesture
{
    const float Move = 12;
    const long LongMs = 500;

    public static bool touching, pressed, moved, longFired;
    public static float x, y, startX, startY;
    static long startTime;
    static UIElement? target;

    public static void Press(UIMouseEvent evt)
    {
        Vector2 m = evt.MousePosition;
        pressed = true;
        touching = true;
        startX = x = m.X;
        startY = y = m.Y;
        startTime = Environment.TickCount64;
        target = evt.Target;
        moved = false;
        longFired = false;
    }

    public static void Release(UIMouseEvent evt)
    {
        Track(evt.MousePosition.X, evt.MousePosition.Y);
        pressed = false;
        touching = false;
    }

    static float Distance(float ax, float ay) => MathF.Sqrt((ax - startX) * (ax - startX) + (ay - startY) * (ay - startY));

    static void Track(float nx, float ny)
    {
        x = nx;
        y = ny;
        if (pressed && Distance(nx, ny) > Move)
            moved = true;
    }

    public static UIElement? Update()
    {
        bool down = Main.mouseLeft;
        touching = down;
        if (down)
            Track(Main.mouseX, Main.mouseY);
        if (!down || !pressed)
            return null;
        if (longFired || moved || Environment.TickCount64 - startTime < LongMs)
            return null;
        longFired = true;
        return target;
    }

    public static bool FireLong(UIElement start)
    {
        for (UIElement? e = start; e != null; e = e.Parent)
        {
            if (e is ILongPress element && element.LongPress())
                return true;
        }
        return false;
    }

    public static bool IsTap(UIMouseEvent evt)
    {
        if (longFired)
            return false;
        return Distance(evt.MousePosition.X, evt.MousePosition.Y) <= Move;
    }

    public static bool On(UIElement el) => touching && el.ContainsPoint(new Vector2(x, y));

    public static bool Inside(float rx, float ry, float rw, float rh) =>
        touching && x >= rx && x < rx + rw && y >= ry && y < ry + rh;
}
