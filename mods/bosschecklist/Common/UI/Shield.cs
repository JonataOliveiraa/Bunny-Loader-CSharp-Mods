using System.Collections.Generic;
using Terraria.UI;

namespace BossChecklistMod.Common.UI;

// Keeps the game's on-screen controls from reacting to a touch that lands on a panel.
public static class Shield
{
    static bool installed;

    public static readonly List<CalculatedStyle> rects = new();

    public static void Install()
    {
        if (installed)
            return;
        installed = true;
        On_GUIVirtualInputController.HandleTouchCursorDown += (orig, self, slot, position, cursor) =>
        {
            if (!Covers(position.X, position.Y))
                orig(self, slot, position, cursor);
        };
    }

    static bool Covers(float x, float y)
    {
        foreach (CalculatedStyle r in rects)
        {
            if (x >= r.X && x < r.X + r.Width && y >= r.Y && y < r.Y + r.Height)
                return true;
        }
        return false;
    }
}
