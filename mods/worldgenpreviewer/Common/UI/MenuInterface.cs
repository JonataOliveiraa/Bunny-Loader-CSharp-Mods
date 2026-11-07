using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace WorldGenPreviewerMod.Common.UI;

public static class Pointer
{
    public static bool down;
    public static float x, y;
    public static float pressX, pressY;
    public static double pressAt;
    public static bool moved;
    public static bool seen;
    public static bool pinching;
    public static Vector2 touchA, touchB;
}

// The screen lives in the menu, where the game gives a UserInterface no touches. This reads them.
public class MenuInterface
{
    const double DOUBLE_TAP_MS = 350;
    const float DRAG_PX = 10;

    static bool multitouch = true;

    readonly UserInterface ui = new();
    UIElement? target;
    bool wasDown;
    bool guard = true;
    int idle;
    UIElement? lastTap;
    double lastTapAt;

    public UIState? CurrentState => ui.CurrentState;

    public void SetState(UIState state)
    {
        ui.SetState(state);
        Reset();
    }

    public void Reset()
    {
        target = null;
        wasDown = false;
        guard = true;
        idle = 0;
        Pointer.down = Pointer.moved = false;
        Pointer.pinching = false;
    }

    static void Touches()
    {
        Pointer.pinching = false;
        if (!multitouch)
            return;
        try
        {
            if (GameBindings.TouchCount() < 2)
                return;
            int sw = GameBindings.ScreenWidth(), sh = GameBindings.ScreenHeight();
            float kx = (float)Main.screenWidth / sw, ky = (float)Main.screenHeight / sh;
            Vector2 a = GameBindings.TouchPosition(0), b = GameBindings.TouchPosition(1);
            Pointer.touchA = new Vector2(a.X * kx, (sh - a.Y) * ky);
            Pointer.touchB = new Vector2(b.X * kx, (sh - b.Y) * ky);
            Pointer.pinching = true;
        }
        catch (Exception e)
        {
            multitouch = false;
            Util.Log("pinch zoom unavailable: " + e.Message);
        }
    }

    void Input(UIState state)
    {
        bool left = Main.mouseLeft;
        Vector2 point = new(Main.mouseX, Main.mouseY);
        Pointer.x = point.X;
        Pointer.y = point.Y;
        // A finger that was already down when the screen appeared is not a press on it.
        if (guard)
        {
            idle = left ? 0 : idle + 1;
            if (idle >= 2)
                guard = false;
            wasDown = left;
            return;
        }
        Touches();
        if (left && !wasDown)
        {
            Pointer.down = true;
            Pointer.seen = true;
            Pointer.pressX = point.X;
            Pointer.pressY = point.Y;
            Pointer.pressAt = Util.NowMs;
            Pointer.moved = false;
            XNAUIInputLayer.Instance.CaptureUICrusorDrag(-1);
            target = state.GetElementAt(point);
            if (target != null)
            {
                target.LeftMouseDown(new UIMouseEvent(target, point));
                if (lastTap == target && Pointer.pressAt - lastTapAt < DOUBLE_TAP_MS)
                {
                    target.LeftDoubleClick(new UIMouseEvent(target, point));
                    lastTap = null;
                }
            }
        }
        else if (left)
        {
            if (Pointer.pinching || Math.Abs(point.X - Pointer.pressX) > DRAG_PX || Math.Abs(point.Y - Pointer.pressY) > DRAG_PX)
                Pointer.moved = true;
        }
        else if (wasDown)
        {
            Pointer.down = false;
            UIElement? released = target;
            target = null;
            if (released != null)
            {
                if (!Pointer.moved && released.ContainsPoint(point))
                {
                    released.LeftClick(new UIMouseEvent(released, point));
                    lastTap = released;
                    lastTapAt = Util.NowMs;
                }
                released.LeftMouseUp(new UIMouseEvent(released, point));
            }
        }
        wasDown = left;
    }

    public void Frame(SpriteBatch spriteBatch)
    {
        UIState? state = ui.CurrentState;
        if (state == null)
            return;
        GUIInputRegionManager.Instance.RegisterInputRegion(new Rectangle(0, 0, Main.screenWidth, Main.screenHeight));
        Input(state);
        state.Update(default);
        ui.Draw(spriteBatch, default);
    }
}
