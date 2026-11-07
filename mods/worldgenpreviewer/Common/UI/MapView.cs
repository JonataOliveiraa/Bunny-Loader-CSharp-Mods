using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using WorldGenPreviewerMod.Common.Configs;
using WorldGenPreviewerMod.Common.Gen;
using WorldGenPreviewerMod.Common.Map;

namespace WorldGenPreviewerMod.Common.UI;

public static class View
{
    public static bool zoomed;
    public static float scale;
    public static float cx = 0.5f, cy = 0.5f;

    public static void Reset()
    {
        zoomed = false;
        scale = 0;
        cx = cy = 0.5f;
    }
}

public class MapView : UIElement
{
    const float MAX_PIXELS_PER_TILE = 12;
    const double HOLD_MS = 350;
    const float ZOOMED = 1.02f;

    Vector2? last;
    bool hasPinch;
    float pinchDist, pinchX, pinchY;

    float Fit()
    {
        CalculatedStyle d = GetInnerDimensions();
        return Math.Min(d.Width / Preview.worldW, d.Height / Preview.worldH);
    }

    float Scale()
    {
        float fit = Fit();
        return View.zoomed ? Math.Max(fit, Math.Min(MAX_PIXELS_PER_TILE, View.scale)) : fit;
    }

    void Origin(out float x, out float y, out float scale)
    {
        CalculatedStyle d = GetInnerDimensions();
        scale = Scale();
        x = d.X + d.Width / 2 - View.cx * Preview.worldW * scale;
        y = d.Y + d.Height / 2 - View.cy * Preview.worldH * scale;
    }

    void Clamp()
    {
        CalculatedStyle d = GetInnerDimensions();
        float scale = Scale();
        float hx = d.Width / (2 * Preview.worldW * scale), hy = d.Height / (2 * Preview.worldH * scale);
        View.cx = hx >= 0.5f ? 0.5f : Math.Max(hx, Math.Min(1 - hx, View.cx));
        View.cy = hy >= 0.5f ? 0.5f : Math.Max(hy, Math.Min(1 - hy, View.cy));
    }

    void ZoomAt(float factor, float px, float py)
    {
        Origin(out float ox, out float oy, out float before);
        float wx = (px - ox) / before, wy = (py - oy) / before;
        float fit = Fit();
        float scale = Math.Max(fit, Math.Min(MAX_PIXELS_PER_TILE, before * factor));
        if (scale > fit * ZOOMED)
        {
            View.zoomed = true;
            View.scale = scale;
        }
        else if (factor < 1)
        {
            View.zoomed = false;
            View.cx = View.cy = 0.5f;
            return;
        }
        CalculatedStyle d = GetInnerDimensions();
        float now = Scale();
        View.cx = (wx * now - (px - d.X - d.Width / 2)) / (Preview.worldW * now);
        View.cy = (wy * now - (py - d.Y - d.Height / 2)) / (Preview.worldH * now);
        Clamp();
    }

    void Pan(float dx, float dy)
    {
        float scale = Scale();
        View.cx -= dx / (Preview.worldW * scale);
        View.cy -= dy / (Preview.worldH * scale);
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        last = evt.MousePosition;
        hasPinch = false;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        last = null;
        hasPinch = false;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (!Preview.Ready)
            return;
        if (last is Vector2 previous && Pointer.down)
        {
            if (Pointer.pinching)
            {
                Vector2 a = Pointer.touchA, b = Pointer.touchB;
                float dx = a.X - b.X, dy = a.Y - b.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy), mx = (a.X + b.X) / 2, my = (a.Y + b.Y) / 2;
                if (hasPinch && pinchDist > 0)
                {
                    Pan(mx - pinchX, my - pinchY);
                    ZoomAt(dist / pinchDist, mx, my);
                }
                hasPinch = true;
                pinchDist = dist;
                pinchX = mx;
                pinchY = my;
            }
            else
            {
                hasPinch = false;
                if (Pointer.moved && View.zoomed)
                    Pan(Pointer.x - previous.X, Pointer.y - previous.Y);
            }
            last = new Vector2(Pointer.x, Pointer.y);
        }
        Clamp();
        Preview.Look(View.zoomed ? WorldView() : null);
    }

    WorldArea WorldView()
    {
        CalculatedStyle d = GetInnerDimensions();
        Origin(out float ox, out float oy, out float scale);
        return new WorldArea { x = (d.X - ox) / scale, y = (d.Y - oy) / scale, w = d.Width / scale, h = d.Height / scale };
    }

    static void DrawStructures(SpriteBatch spriteBatch, List<Rectangle> list, Color color, in CalculatedStyle d, float ox, float oy, float scale)
    {
        foreach (Rectangle r in list)
        {
            float x = ox + r.X * scale, y = oy + r.Y * scale;
            float w = Math.Max(1, r.Width * scale), h = Math.Max(1, r.Height * scale);
            if (x > d.X + d.Width || y > d.Y + d.Height || x + w < d.X || y + h < d.Y)
                continue;
            Widgets.Fill(spriteBatch, x, y, w, h, color);
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetInnerDimensions();
        Widgets.Fill(spriteBatch, d.X, d.Y, d.Width, d.Height, Color.Black);
        if (!Preview.Ready)
        {
            string text = Widgets.Tr(WorldGenPreviewerConfig.Instance.LivePreview ? "Waiting" : "PreviewOff");
            float s = Math.Max(1, d.Height / 600);
            Widgets.Text(spriteBatch, text, d.X + (d.Width - Widgets.Width(text, s)) / 2, d.Y + d.Height / 2 - 12 * s, Colors.Status, s);
            return;
        }
        Origin(out float ox, out float oy, out float scale);
        Preview.full.Draw(spriteBatch, ox, oy, scale, d.X, d.Y, d.Width, d.Height);

        if (State.showStructures)
        {
            DrawStructures(spriteBatch, Preview.Structures(false), Colors.Structure, d, ox, oy, scale);
            DrawStructures(spriteBatch, Preview.Structures(true), Colors.Protected, d, ox, oy, scale);
        }

        if (State.active)
        {
            float x = ox + Preview.full.Scan * scale;
            if (x >= d.X && x <= d.X + d.Width)
                Widgets.Fill(spriteBatch, x, d.Y, Math.Max(Math.Max(1, Util.Round(d.Height / 360)), scale), d.Height, Colors.Scan);
        }

        if (last != null && Pointer.down && !Pointer.moved && Util.NowMs - Pointer.pressAt > HOLD_MS)
        {
            int type = Preview.TypeAt((int)Math.Floor((Pointer.x - ox) / scale), (int)Math.Floor((Pointer.y - oy) / scale));
            string name = Preview.TileName(type);
            if (name.Length > 0)
                Hint.text = Widgets.Tr("Tile", name);
        }
    }
}
