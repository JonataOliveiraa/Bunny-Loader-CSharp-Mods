using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ModLoader;

namespace PingModPort.Common.UI;

// Measures the hearts as the game draws them, so the indicator can sit next to the real bar.
public class LifeBarTracker : ModResourceOverlay
{
    const long MaxAgeMs = 500;

    static bool lifeBar;
    static bool building;
    static float x0, y0, x1, y1;
    static bool measured;
    static float mx, my, mh;
    static long measuredAt;

    // The draw context does not say which bar it belongs to. This hook, around the bar, does.
    public override bool PreDrawResourceDisplay(PlayerStatsSnapshot snapshot, IPlayerResourcesDisplaySet displaySet, bool drawingLife, ref Color textColor, out bool drawText)
    {
        lifeBar = drawingLife;
        drawText = true;
        return true;
    }

    public override void PostDrawResourceDisplay(PlayerStatsSnapshot snapshot, IPlayerResourcesDisplaySet displaySet, bool drawingLife, Color textColor, bool drawText) =>
        lifeBar = false;

    public override void PostDrawResource(ResourceOverlayDrawContext context)
    {
        if (!lifeBar)
            return;
        float scale = context.scale.X;
        Texture2D texture = context.texture.Value;
        float w = context.source?.Width ?? texture.Width;
        float h = context.source?.Height ?? texture.Height;
        float left = context.position.X - context.origin.X * scale;
        float top = context.position.Y - context.origin.Y * scale;
        float right = left + w * scale, bottom = top + h * scale;
        if (!building)
        {
            building = true;
            x0 = left;
            y0 = top;
            x1 = right;
            y1 = bottom;
        }
        else
        {
            x0 = Math.Min(x0, left);
            y0 = Math.Min(y0, top);
            x1 = Math.Max(x1, right);
            y1 = Math.Max(y1, bottom);
        }
    }

    public static void CommitLifeBar()
    {
        if (!building)
            return;
        measured = true;
        mx = x0;
        my = y0;
        mh = y1 - y0;
        measuredAt = Ping.NowMs;
        building = false;
    }

    public static bool MeasuredLifeBar(out float x, out float y, out float h)
    {
        x = mx;
        y = my;
        h = mh;
        return measured && Ping.NowMs - measuredAt <= MaxAgeMs;
    }
}
